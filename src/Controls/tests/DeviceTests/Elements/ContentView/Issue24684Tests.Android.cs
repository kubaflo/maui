#if ANDROID
#pragma warning disable CS0618 // Frame is required to reproduce Issue24684.
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue24684")]
	public class Issue24684 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TapOnFramedContentRaisesAncestorContentViewGesture()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Frame, FrameRenderer>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			int framedTapCount = -1;

			var framedLabel = new Label { Text = "Click me" };
			var frame = new Frame { Content = framedLabel };
			var framedControl = new Issue24684Control { Content = frame };
			var framedTap = new TapGestureRecognizer();
			framedTap.Tapped += (_, _) => framedTapCount = framedTapCount < 0 ? 1 : framedTapCount + 1;
			framedControl.GestureRecognizers.Add(framedTap);

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					framedControl
				}
			};
			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AView root = page.ToPlatform();
				AView framedControlView = framedControl.ToPlatform();
				AView frameView = frame.ToPlatform();
				AView framedLabelView = framedLabel.ToPlatform();

				Assert.Equal("Click me", Assert.IsAssignableFrom<ATextView>(framedLabelView).Text);
				Assert.True(root.IsAttachedToWindow);
				Assert.True(framedControlView.IsAttachedToWindow);
				Assert.True(frameView.IsAttachedToWindow);
				Assert.True(framedLabelView.IsAttachedToWindow);
				Assert.True(root.Width > 0 && root.Height > 0);
				Assert.True(framedControlView.Width > 0 && framedControlView.Height > 0);
				Assert.True(frameView.Width > 0 && frameView.Height > 0);
				Assert.True(framedLabelView.Width > 0 && framedLabelView.Height > 0);
				Assert.Equal(-1, framedTapCount);

				(float framedX, float framedY) = GetCenterInRoot(root, framedLabelView);
				AssertPointInside(root, framedX, framedY);
				AssertPointInside(framedLabelView, root, framedX, framedY);

				(bool downDispatched, bool upDispatched) = DispatchTap(root, framedX, framedY);
				Assert.True(downDispatched);
				Assert.True(upDispatched);
				await AssertEventually(
					() => framedTapCount == 1,
					message: $"Framed custom ContentView tap callback did not fire after native Android tap: observed {framedTapCount}; expected 1.");
			});
		}

		static (float X, float Y) GetCenterInRoot(AView root, AView target)
		{
			int[] rootLocation = new int[2];
			int[] targetLocation = new int[2];
			root.GetLocationOnScreen(rootLocation);
			target.GetLocationOnScreen(targetLocation);

			return (
				targetLocation[0] - rootLocation[0] + (target.Width / 2f),
				targetLocation[1] - rootLocation[1] + (target.Height / 2f));
		}

		static void AssertPointInside(AView view, float x, float y)
		{
			Assert.InRange(x, 0f, (float)view.Width);
			Assert.InRange(y, 0f, (float)view.Height);
		}

		static void AssertPointInside(AView target, AView root, float rootX, float rootY)
		{
			int[] rootLocation = new int[2];
			int[] targetLocation = new int[2];
			root.GetLocationOnScreen(rootLocation);
			target.GetLocationOnScreen(targetLocation);
			float targetX = rootLocation[0] + rootX - targetLocation[0];
			float targetY = rootLocation[1] + rootY - targetLocation[1];

			AssertPointInside(target, targetX, targetY);
		}

		static (bool DownDispatched, bool UpDispatched) DispatchTap(AView root, float x, float y)
		{
			long downTime = SystemClock.UptimeMillis();
			AMotionEvent down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, x, y, 0);
			bool downDispatched = root.DispatchTouchEvent(down);
			down.Recycle();

			AMotionEvent up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, x, y, 0);
			bool upDispatched = root.DispatchTouchEvent(up);
			up.Recycle();

			return (downDispatched, upDispatched);
		}

		sealed class Issue24684Control : ContentView
		{
		}
	}
}
#endif


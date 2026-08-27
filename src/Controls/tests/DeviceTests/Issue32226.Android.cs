#if ANDROID
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32226")]
	public class Issue32226 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task UnhandledNativeTouchDoesNotSuppressTapGestureRecognizer()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			int tapCount = 0;
			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (_, _) => tapCount++;

			var headingLabel = new Label
			{
				Text = "Tap gesture with unhandled native touch",
				FontSize = 20
			};
			var affectedLabel = new Label
			{
				Text = "Click me (Label TapGestureRecognizer)",
				FontSize = 18
			};
			affectedLabel.GestureRecognizers.Add(tapGestureRecognizer);

			var touchStatusLabel = new Label
			{
				Text = "Touch received: no"
			};
			var checkButton = new Button
			{
				Text = "Check tap result"
			};
			var resultLabel = new Label
			{
				Text = "Tap result",
				FontSize = 18
			};
			var stackLayout = new VerticalStackLayout
			{
				Margin = 24,
				Spacing = 20,
				Children =
				{
					headingLabel,
					affectedLabel,
					touchStatusLabel,
					checkButton,
					resultLabel
				}
			};
			var page = new ContentPage
			{
				Content = stackLayout
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Equal("Click me (Label TapGestureRecognizer)", affectedLabel.Text);
				Assert.Equal(18, affectedLabel.FontSize);
				Assert.Same(affectedLabel, stackLayout.Children[1]);

				var platformLabel = affectedLabel.Handler.PlatformView as AView;
				Assert.NotNull(platformLabel);
				await platformLabel.WaitForLayoutOrNonZeroSize();
				Assert.True(platformLabel.IsAttachedToWindow);
				Assert.True(platformLabel.Width > 0);
				Assert.True(platformLabel.Height > 0);

				var windowRoot = MauiContext.Context.GetActivity().Window.DecorView;
				Assert.NotNull(windowRoot);

				DispatchTap(windowRoot, platformLabel);
				Assert.Equal(1, tapCount);

				int nativeTouchAction = -1;
				int nativeTouchEventCount = -1;
				platformLabel.Touch += (_, e) =>
				{
					if (nativeTouchEventCount < 0)
					{
						nativeTouchAction = (int)e.Event.ActionMasked;
						nativeTouchEventCount = 1;
					}
					else
					{
						nativeTouchEventCount++;
					}
					e.Handled = false;
				};

				int tapCountBeforeSecondTouch = tapCount;
				DispatchTap(windowRoot, platformLabel);

				Assert.True(nativeTouchEventCount >= 1);
				Assert.Equal((int)AMotionEventActions.Down, nativeTouchAction);
				Assert.True(
					tapCount == tapCountBeforeSecondTouch + 1,
					$"Issue 32226: TapGestureRecognizer Tapped callback count was {tapCount}; expected {tapCountBeforeSecondTouch + 1}.");
			});
		}

		static void DispatchTap(AView windowRoot, AView target)
		{
			var rootLocation = new int[2];
			var targetLocation = new int[2];
			windowRoot.GetLocationOnScreen(rootLocation);
			target.GetLocationOnScreen(targetLocation);

			float x = targetLocation[0] - rootLocation[0] + (target.Width / 2f);
			float y = targetLocation[1] - rootLocation[1] + (target.Height / 2f);
			long downTime = SystemClock.UptimeMillis();

			var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, x, y, 0);
			windowRoot.DispatchTouchEvent(down);
			down.Recycle();

			var up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, x, y, 0);
			windowRoot.DispatchTouchEvent(up);
			up.Recycle();
		}
	}
}
#endif


#if ANDROID
using System.Threading.Tasks;
using Android.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;
using MauiButton = Microsoft.Maui.Controls.Button;

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
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<MauiButton, ButtonHandler>();
				});
			});

			AMotionEventActions? nativeAction = null;
			int tappedCount = -1;
			AView platformTargetLabel = null;

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (_, _) =>
				tappedCount = tappedCount < 0 ? 1 : tappedCount + 1;

			var targetLabel = new Label
			{
				Text = "Click me (Label TapGestureRecognizer)"
			};
			targetLabel.GestureRecognizers.Add(tapGestureRecognizer);
			targetLabel.HandlerChanged += (_, _) =>
			{
				if (targetLabel.Handler?.PlatformView is AView nativeLabel)
				{
					platformTargetLabel = nativeLabel;
					nativeLabel.Touch += (_, e) =>
					{
						if (e.Event is AMotionEvent motionEvent &&
							motionEvent.ActionMasked == AMotionEventActions.Down)
						{
							nativeAction = motionEvent.ActionMasked;
						}

						e.Handled = false;
					};
				}
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label { Text = "TapGestureRecognizer with Android Touch handled false" },
						targetLabel,
						new Label { Text = "Native Touch received: 0" },
						new Label { Text = "TapGestureRecognizer Tapped count: 0" },
						new Label { Text = "Tap result pending" },
						new MauiButton { Text = "Reset state" },
						new MauiButton { Text = "Check tap result" }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(platformTargetLabel);
				await platformTargetLabel.WaitForLayoutOrNonZeroSize();

				Assert.True(platformTargetLabel.IsAttachedToWindow);
				Assert.True(platformTargetLabel.Width > 0);
				Assert.True(platformTargetLabel.Height > 0);
				Assert.True(platformTargetLabel is TextView);
				Assert.Equal(targetLabel.Text, ((TextView)platformTargetLabel).Text);

				var decorView = MauiContext.Context.GetActivity().Window.DecorView;
				Assert.NotNull(decorView);

				var targetLocation = new int[2];
				var decorLocation = new int[2];
				platformTargetLabel.GetLocationOnScreen(targetLocation);
				decorView.GetLocationOnScreen(decorLocation);

				float tapX = targetLocation[0] - decorLocation[0] + (platformTargetLabel.Width / 2f);
				float tapY = targetLocation[1] - decorLocation[1] + (platformTargetLabel.Height / 2f);
				Assert.InRange(tapX, 0.1f, decorView.Width - 0.1f);
				Assert.InRange(tapY, 0.1f, decorView.Height - 0.1f);

				long downTime = global::Android.OS.SystemClock.UptimeMillis();
				var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, tapX, tapY, 0);
				decorView.DispatchTouchEvent(down);
				down.Recycle();

				var up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, tapX, tapY, 0);
				decorView.DispatchTouchEvent(up);
				up.Recycle();

				bool nativeTouchObserved = await AssertHelpers.Wait(
					() => nativeAction == AMotionEventActions.Down);
				Assert.True(nativeTouchObserved, "The native Touch listener did not receive the DOWN event.");

				bool tappedObserved = await AssertHelpers.Wait(() => tappedCount != -1);
				string failureMessage =
					$"Issue32226: TapGestureRecognizer Tapped callback count was {tappedCount}; expected 1 after native Touch received the same tap with Handled=false.";
				Assert.True(tappedObserved, failureMessage);
				Assert.True(tappedCount == 1, failureMessage);
			});
		}
	}
}
#endif


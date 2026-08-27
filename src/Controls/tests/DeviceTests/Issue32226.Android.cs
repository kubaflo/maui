using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AView = Android.Views.View;

#if ANDROID
namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32226")]
	public class Issue32226 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task NativeTouchHandlerDoesNotPreventTapGestureRecognizer()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var tappedCount = 0;
			var nativeTouchCount = 0;
			var observedNativeAction = (MotionEventActions)(-1);

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (sender, args) => tappedCount++;

			var targetLabel = new Label
			{
				Text = "Click me (Label TapGestureRecognizer)"
			};
			targetLabel.GestureRecognizers.Add(tapGestureRecognizer);

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label { Text = "Tap the label, then check whether its TapGestureRecognizer fired." },
						targetLabel,
						new Label { Text = "Native touch received: 0" },
						new Label { Text = "TapGestureRecognizer fired: 0" },
						new Button { Text = "Check tap result" },
						new Label { Text = "Tap result pending" }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var nativeView = targetLabel.Handler?.PlatformView as AView;
				Assert.NotNull(nativeView);
				await nativeView.WaitForLayoutOrNonZeroSize();

				Assert.True(nativeView.IsAttachedToWindow);
				Assert.Equal(ViewStates.Visible, nativeView.Visibility);
				Assert.True(nativeView.Enabled);
				Assert.True(nativeView.MeasuredWidth > 0);
				Assert.True(nativeView.MeasuredHeight > 0);

				EventHandler<AView.TouchEventArgs> nativeTouchHandler = (sender, args) =>
				{
					if (args.Event?.ActionMasked == MotionEventActions.Down)
					{
						observedNativeAction = args.Event.ActionMasked;
						nativeTouchCount++;
					}

					args.Handled = false;
				};

				nativeView.Touch += nativeTouchHandler;
				try
				{
					var rootView = nativeView.RootView;
					Assert.NotNull(rootView);

					var targetLocation = new int[2];
					var rootLocation = new int[2];
					nativeView.GetLocationOnScreen(targetLocation);
					rootView.GetLocationOnScreen(rootLocation);

					var centerX = targetLocation[0] - rootLocation[0] + (nativeView.MeasuredWidth / 2f);
					var centerY = targetLocation[1] - rootLocation[1] + (nativeView.MeasuredHeight / 2f);
					var downTime = SystemClock.UptimeMillis();

					var downEvent = MotionEvent.Obtain(
						downTime,
						downTime,
						MotionEventActions.Down,
						centerX,
						centerY,
						0);
					var downAccepted = rootView.DispatchTouchEvent(downEvent);
					downEvent.Recycle();

					var upEvent = MotionEvent.Obtain(
						downTime,
						downTime + 100,
						MotionEventActions.Up,
						centerX,
						centerY,
						0);
					var upAccepted = rootView.DispatchTouchEvent(upEvent);
					upEvent.Recycle();

					Assert.Equal(MotionEventActions.Down, observedNativeAction);
					Assert.True(nativeTouchCount > 0, "The target Label should receive the native down event.");
					Assert.Same(nativeView, targetLabel.Handler?.PlatformView);
					Assert.True(nativeView.IsAttachedToWindow);

					await AssertHelpers.AssertEventually(
						() => tappedCount > 0,
						message: $"TapGestureRecognizer should fire after native Touch sets Handled=false; nativeTouchCount={nativeTouchCount}, tappedCount={tappedCount}, expected=1, downAccepted={downAccepted}, upAccepted={upAccepted}");

					Assert.True(
						tappedCount == 1,
						$"TapGestureRecognizer should fire after native Touch sets Handled=false; nativeTouchCount={nativeTouchCount}, tappedCount={tappedCount}, expected=1, downAccepted={downAccepted}, upAccepted={upAccepted}");
				}
				finally
				{
					nativeView.Touch -= nativeTouchHandler;
				}
			});
		}
	}
}
#endif


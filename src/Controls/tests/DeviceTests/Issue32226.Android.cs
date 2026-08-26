#if ANDROID
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using ASystemClock = Android.OS.SystemClock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32226")]
	public class Issue32226 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task UnhandledNativeTouchDoesNotSuppressTapGesture()
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

			int nativeDownPhase = -1;
			int tappedPhase = -1;
			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (_, _) =>
				tappedPhase = tappedPhase == -1 ? 1 : tappedPhase + 1;

			var instructionLabel = new Label
			{
				Text = "Tap the label, then check the result."
			};
			var tapTarget = new Label
			{
				Text = "Click me (Label TapGestureRecognizer)"
			};
			tapTarget.GestureRecognizers.Add(tapGestureRecognizer);

			var nativeTouchStatus = new Label
			{
				Text = "Native touch count: 0"
			};
			var gestureStatus = new Label
			{
				Text = "Gesture tapped count: 0"
			};
			var checkResultButton = new Button
			{
				Text = "Check result"
			};
			var resultStatus = new Label
			{
				Text = "Result pending"
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					instructionLabel,
					tapTarget,
					nativeTouchStatus,
					gestureStatus,
					checkResultButton,
					resultStatus
				}
			};
			var page = new ContentPage
			{
				Content = layout
			};

			await CreateHandlerAndAddToWindow(page, () =>
			{
				Assert.Same(tapTarget, layout.Children[1]);
				Assert.Equal("Click me (Label TapGestureRecognizer)", tapTarget.Text);
				Assert.Contains(tapGestureRecognizer, tapTarget.GestureRecognizers);
				Assert.Null(tapTarget.Style);

				var labelHandler = Assert.IsType<LabelHandler>(tapTarget.Handler);
				var platformView = Assert.IsAssignableFrom<AppCompatTextView>(labelHandler.PlatformView);
				Assert.True(platformView.Width > 0);
				Assert.True(platformView.Height > 0);

				platformView.Touch += (_, e) =>
				{
					if (e.Event?.Action == AMotionEventActions.Down)
						nativeDownPhase = nativeDownPhase == -1 ? 1 : -2;

					e.Handled = false;
				};

				var rootView = platformView.RootView;
				Assert.NotSame(platformView, rootView);

				var targetLocation = new int[2];
				var rootLocation = new int[2];
				platformView.GetLocationOnScreen(targetLocation);
				rootView.GetLocationOnScreen(rootLocation);

				float centerX = targetLocation[0] - rootLocation[0] + platformView.Width / 2f;
				float centerY = targetLocation[1] - rootLocation[1] + platformView.Height / 2f;
				Assert.InRange(centerX, 0f, (float)rootView.Width);
				Assert.InRange(centerY, 0f, (float)rootView.Height);
				long downTime = ASystemClock.UptimeMillis();

				var downEvent = AMotionEvent.Obtain(
					downTime,
					downTime,
					AMotionEventActions.Down,
					centerX,
					centerY,
					0);
				rootView.DispatchTouchEvent(downEvent);
				downEvent.Recycle();

				var upEvent = AMotionEvent.Obtain(
					downTime,
					downTime + 16,
					AMotionEventActions.Up,
					centerX,
					centerY,
					0);
				rootView.DispatchTouchEvent(upEvent);
				upEvent.Recycle();

				Assert.Equal(1, nativeDownPhase);
				Assert.True(
					tappedPhase == 1,
					$"TapGestureRecognizer Tapped phase should be 1 after an unhandled native touch; actual: {tappedPhase}.");
			});
		}
	}
}
#endif


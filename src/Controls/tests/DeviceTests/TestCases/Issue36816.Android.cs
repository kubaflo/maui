#if ANDROID
using System;
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.HeadlessRunner;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36816")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36816 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task OpaqueContentViewBlocksTapFromCoveredButton()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
				});
			});

			int clickCount = 0;
			var coveredButton = new Button
			{
				Text = "Covered button"
			};
			coveredButton.Clicked += (_, _) => clickCount++;

			var overlay = new ContentView
			{
				BackgroundColor = Colors.Green
			};

			var grid = new Grid
			{
				HeightRequest = 180,
				Children =
				{
					coveredButton,
					overlay
				}
			};

			var resetButton = new Button
			{
				Text = "Reset test"
			};
			resetButton.Clicked += (_, _) => clickCount = 0;

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "Tap the green view covering the button.", FontSize = 18 },
						grid,
						new Label { Text = "Covered button clicks: 0", FontSize = 18 },
						resetButton
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Equal("Covered button", coveredButton.Text);
				Assert.Same(coveredButton, grid[0]);
				Assert.Same(overlay, grid[1]);
				Assert.False(overlay.InputTransparent);

				Assert.NotNull(grid.Handler);
				Assert.NotNull(coveredButton.Handler);
				Assert.NotNull(overlay.Handler);

				var platformGrid = (AViewGroup)grid.Handler.PlatformView;
				var platformButton = (AView)coveredButton.Handler.PlatformView;
				var platformOverlay = (AView)overlay.Handler.PlatformView;

				await platformGrid.WaitForLayoutOrNonZeroSize();
				await platformButton.WaitForLayoutOrNonZeroSize();
				await platformOverlay.WaitForLayoutOrNonZeroSize();

				Assert.True(platformGrid.IsAttachedToWindow);
				Assert.True(platformButton.IsAttachedToWindow);
				Assert.True(platformOverlay.IsAttachedToWindow);
				Assert.Equal(2, platformGrid.ChildCount);
				Assert.Same(platformButton, platformGrid.GetChildAt(0));
				Assert.Same(platformOverlay, platformGrid.GetChildAt(1));

				var buttonLocation = new int[2];
				var overlayLocation = new int[2];
				platformButton.GetLocationOnScreen(buttonLocation);
				platformOverlay.GetLocationOnScreen(overlayLocation);

				int tolerance = (int)Math.Ceiling(platformGrid.Context.ToPixels(1));
				Assert.InRange(Math.Abs(buttonLocation[0] - overlayLocation[0]), 0, tolerance);
				Assert.InRange(Math.Abs(buttonLocation[1] - overlayLocation[1]), 0, tolerance);
				Assert.InRange(Math.Abs(platformButton.Width - platformOverlay.Width), 0, tolerance);
				Assert.InRange(Math.Abs(platformButton.Height - platformOverlay.Height), 0, tolerance);
				Assert.InRange(Math.Abs(platformGrid.Height - platformGrid.Context.ToPixels(180)), 0, tolerance);
				Assert.True(platformOverlay.Width > 0);
				Assert.True(platformOverlay.Height > 0);
				Assert.Equal(0, clickCount);

				float tapX = overlayLocation[0] + platformOverlay.Width / 2f;
				float tapY = overlayLocation[1] + platformOverlay.Height / 2f;
				long downTime = SystemClock.UptimeMillis();

				var inputDispatchReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				Assert.True(platformOverlay.Post(() => inputDispatchReady.SetResult(true)));
				await inputDispatchReady.Task.ConfigureAwait(false);

				using var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, tapX, tapY, 0);
				using var up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, tapX, tapY, 0);
				MauiTestInstrumentation.Current.SendPointerSync(down);
				MauiTestInstrumentation.Current.SendPointerSync(up);

				int observedClickCount = -1;
				var observationCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				bool posted = platformOverlay.Post(() =>
				{
					observedClickCount = clickCount;
					observationCompleted.SetResult(true);
				});

				Assert.True(posted);
				await observationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.NotEqual(-1, observedClickCount);
				Assert.True(observedClickCount == 0,
					$"Covered button received {observedClickCount} click(s) through the opaque ContentView; expected 0");
			});
		}
	}
}
#endif


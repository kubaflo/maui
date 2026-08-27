using System;
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36816")]
	public class Issue36816 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ContentViewBlocksTouchesFromUnderlyingButton()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
				});
			});

			static ContentPage CreatePage(Button underlyingButton, View topView)
			{
				var root = new Grid
				{
					Padding = 24,
					RowSpacing = 16,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Auto),
						new RowDefinition(220),
						new RowDefinition(GridLength.Auto),
					}
				};

				var overlayGrid = new Grid();
				overlayGrid.Add(underlyingButton);
				overlayGrid.Add(topView);
				root.Add(overlayGrid, row: 2);

				return new ContentPage { Content = root };
			}

			static (float X, float Y) GetCenterInRoot(AView surface, AViewGroup root)
			{
				var surfaceLocation = new int[2];
				var rootLocation = new int[2];
				surface.GetLocationOnScreen(surfaceLocation);
				root.GetLocationOnScreen(rootLocation);

				return (
					surfaceLocation[0] - rootLocation[0] + surface.Width / 2f,
					surfaceLocation[1] - rootLocation[1] + surface.Height / 2f);
			}

			static void AssertPointInside(AView view, AViewGroup root, float x, float y)
			{
				var viewLocation = new int[2];
				var rootLocation = new int[2];
				view.GetLocationOnScreen(viewLocation);
				root.GetLocationOnScreen(rootLocation);

				var left = viewLocation[0] - rootLocation[0];
				var top = viewLocation[1] - rootLocation[1];
				Assert.True(x >= left && x < left + view.Width);
				Assert.True(y >= top && y < top + view.Height);
			}

			static async Task<(bool? DownHandled, bool? UpHandled)> DispatchTap(AViewGroup root, float x, float y)
			{
				bool? downHandled = null;
				bool? upHandled = null;
				var postTriggerObserved = false;
				var posted = new TaskCompletionSource<bool>();
				var downTime = SystemClock.UptimeMillis();

				var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, x, y, 0);
				downHandled = root.DispatchTouchEvent(down);
				down.Recycle();

				var up = AMotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), AMotionEventActions.Up, x, y, 0);
				upHandled = root.DispatchTouchEvent(up);
				up.Recycle();

				using var postCallback = new Java.Lang.Runnable(() =>
				{
					postTriggerObserved = true;
					posted.TrySetResult(true);
				});
				Assert.True(root.Post(postCallback));

				await posted.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.True(postTriggerObserved);
				Assert.True(downHandled.HasValue);
				Assert.True(upHandled.HasValue);
				return (downHandled, upHandled);
			}

			var cleanUnderlyingClicks = 0;
			var cleanTopClicks = 0;
			var cleanUnderlyingButton = new Button { Text = "UNDERLYING BUTTON" };
			var cleanTopButton = new Button { Text = "TOP BUTTON" };
			cleanUnderlyingButton.Clicked += (_, _) => cleanUnderlyingClicks++;
			cleanTopButton.Clicked += (_, _) => cleanTopClicks++;
			var cleanPage = CreatePage(cleanUnderlyingButton, cleanTopButton);

			await CreateHandlerAndAddToWindow(cleanPage, async () =>
			{
				Assert.NotNull(cleanPage.Content.Handler);
				Assert.NotNull(cleanUnderlyingButton.Handler);
				Assert.NotNull(cleanTopButton.Handler);
				var root = Assert.IsAssignableFrom<AViewGroup>(cleanPage.Content.Handler.PlatformView);
				var underlying = Assert.IsAssignableFrom<AView>(cleanUnderlyingButton.Handler.PlatformView);
				var top = Assert.IsAssignableFrom<AView>(cleanTopButton.Handler.PlatformView);
				Assert.True(root.IsAttachedToWindow);
				Assert.True(underlying.IsAttachedToWindow);
				Assert.True(top.IsAttachedToWindow);
				Assert.True(root.Width > 0 && root.Height > 0);
				Assert.True(underlying.Width > 0 && underlying.Height > 0);
				Assert.True(top.Width > 0 && top.Height > 0);

				var point = GetCenterInRoot(top, root);
				AssertPointInside(root, root, point.X, point.Y);
				AssertPointInside(top, root, point.X, point.Y);
				AssertPointInside(underlying, root, point.X, point.Y);
				await DispatchTap(root, point.X, point.Y);

				Assert.Equal(1, cleanTopClicks);
				Assert.Equal(0, cleanUnderlyingClicks);
			});

			var targetClicks = 0;
			var targetButton = new Button { Text = "UNDERLYING BUTTON" };
			var overlay = new ContentView { BackgroundColor = Colors.Green };
			Assert.False(overlay.InputTransparent);
			targetButton.Clicked += (_, _) => targetClicks++;
			var targetPage = CreatePage(targetButton, overlay);

			await CreateHandlerAndAddToWindow(targetPage, async () =>
			{
				Assert.NotNull(targetPage.Content.Handler);
				Assert.NotNull(targetButton.Handler);
				Assert.NotNull(overlay.Handler);
				var root = Assert.IsAssignableFrom<AViewGroup>(targetPage.Content.Handler.PlatformView);
				var underlying = Assert.IsAssignableFrom<AView>(targetButton.Handler.PlatformView);
				var top = Assert.IsAssignableFrom<AView>(overlay.Handler.PlatformView);
				Assert.True(root.IsAttachedToWindow);
				Assert.True(underlying.IsAttachedToWindow);
				Assert.True(top.IsAttachedToWindow);
				Assert.True(root.Width > 0 && root.Height > 0);
				Assert.True(underlying.Width > 0 && underlying.Height > 0);
				Assert.True(top.Width > 0 && top.Height > 0);

				var point = GetCenterInRoot(top, root);
				AssertPointInside(root, root, point.X, point.Y);
				AssertPointInside(top, root, point.X, point.Y);
				AssertPointInside(underlying, root, point.X, point.Y);
				var handled = await DispatchTap(root, point.X, point.Y);

				Assert.True(targetClicks == 0,
					$"ContentView touch-through: underlying button clicks={targetClicks}, expected=0; downHandled={handled.DownHandled}, upHandled={handled.UpHandled}");
			});
		}
	}
}


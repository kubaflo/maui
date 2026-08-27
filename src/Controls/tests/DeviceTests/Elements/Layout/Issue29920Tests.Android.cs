using System.Threading.Tasks;
using Android.OS;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29920")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue29920 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TopStackLayoutBlocksTapFromReachingOverlappedBoxView()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
				});
			});

			int lowerTapCount = 0;
			var lowerTapGesture = new TapGestureRecognizer();
			lowerTapGesture.Tapped += (_, _) => lowerTapCount++;

			var lowerBoxView = new BoxView
			{
				Color = Colors.Blue,
				HeightRequest = 96,
				HorizontalOptions = LayoutOptions.Start,
				Margin = new Thickness(24),
				VerticalOptions = LayoutOptions.Start,
				WidthRequest = 160,
				GestureRecognizers = { lowerTapGesture },
			};
			var lowerStackLayout = new StackLayout
			{
				Background = Colors.Blue,
				Opacity = 0.1,
				Children = { lowerBoxView },
			};
			var upperStackLayout = new StackLayout
			{
				Background = Colors.Red,
				Opacity = 0.1,
			};
			var rootGrid = new Grid
			{
				Children =
				{
					lowerStackLayout,
					upperStackLayout,
				},
			};
			var page = new ContentPage { Content = rootGrid };
			int dispatchPhase = -1;
			int observedTapCount = -1;

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.IsType<PageHandler>(page.Handler);
				Assert.IsType<LayoutHandler>(rootGrid.Handler);
				Assert.IsType<LayoutHandler>(lowerStackLayout.Handler);
				Assert.IsType<LayoutHandler>(upperStackLayout.Handler);
				Assert.IsType<BoxViewHandler>(lowerBoxView.Handler);
				Assert.DoesNotContain(upperStackLayout.GestureRecognizers, gesture => gesture is TapGestureRecognizer);

				AView nativeRoot = rootGrid.ToPlatform(MauiContext);
				AView nativeLowerBoxView = lowerBoxView.ToPlatform(MauiContext);
				AView nativeUpperStackLayout = upperStackLayout.ToPlatform(MauiContext);

				await Task.WhenAll(
					nativeRoot.WaitForLayoutOrNonZeroSize(),
					nativeLowerBoxView.WaitForLayoutOrNonZeroSize(),
					nativeUpperStackLayout.WaitForLayoutOrNonZeroSize());

				var rootLocation = new int[2];
				var lowerBoxViewLocation = new int[2];
				var upperStackLayoutLocation = new int[2];
				nativeRoot.GetLocationOnScreen(rootLocation);
				nativeLowerBoxView.GetLocationOnScreen(lowerBoxViewLocation);
				nativeUpperStackLayout.GetLocationOnScreen(upperStackLayoutLocation);

				float tapScreenX = lowerBoxViewLocation[0] + (nativeLowerBoxView.Width / 2f);
				float tapScreenY = lowerBoxViewLocation[1] + (nativeLowerBoxView.Height / 2f);
				bool tapIsInsideUpperStackLayout =
					tapScreenX >= upperStackLayoutLocation[0] &&
					tapScreenX < upperStackLayoutLocation[0] + nativeUpperStackLayout.Width &&
					tapScreenY >= upperStackLayoutLocation[1] &&
					tapScreenY < upperStackLayoutLocation[1] + nativeUpperStackLayout.Height;

				Assert.True(tapIsInsideUpperStackLayout, "The lower BoxView center must be covered by the upper StackLayout.");

				float tapX = tapScreenX - rootLocation[0];
				float tapY = tapScreenY - rootLocation[1];
				long downTime = SystemClock.UptimeMillis();

				var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, tapX, tapY, 0);
				nativeRoot.DispatchTouchEvent(down);
				down.Recycle();
				dispatchPhase = 0;

				var up = AMotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), AMotionEventActions.Up, tapX, tapY, 0);
				nativeRoot.DispatchTouchEvent(up);
				up.Recycle();
				dispatchPhase = 1;
				observedTapCount = lowerTapCount;

				Assert.Equal(1, dispatchPhase);
				Assert.NotEqual(-1, observedTapCount);
				Assert.True(
					observedTapCount == 0,
					"The lower BoxView TapGestureRecognizer received a tap through the overlapping top StackLayout.");
			});
		}
	}
}


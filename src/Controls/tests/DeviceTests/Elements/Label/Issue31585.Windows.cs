using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WFlowDirection = Microsoft.UI.Xaml.FlowDirection;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WThickness = Microsoft.UI.Xaml.Thickness;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31585")]
	public class Issue31585 : ControlsHandlerTestBase
	{
		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});
		}

		[Fact]
		public async Task RtlLabelMapsPaddingToVisualRight()
		{
			SetupBuilder();

			const double tolerance = 0.01;
			var arrangedPadding = new Thickness(0, 0, 100, 0);

			var ltrLabel = new Label
			{
				Text = "My Label",
				Padding = arrangedPadding,
				BackgroundColor = Colors.Yellow
			};
			var ltrStack = new VerticalStackLayout
			{
				WidthRequest = 500,
				HeightRequest = 125,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				FlowDirection = FlowDirection.LeftToRight,
				BackgroundColor = Colors.Red,
				Children = { ltrLabel }
			};
			var ltrPage = new ContentPage
			{
				Content = new Grid
				{
					Children = { ltrStack }
				}
			};

			bool ltrCallbackExecuted = false;
			WThickness? ltrNativePadding = null;
			WTextBlock ltrNativeLabel = null;
			LabelHandler ltrLabelHandler = null;
			double ltrWidth = 0;
			double ltrHeight = 0;
			WFlowDirection ltrNativeFlowDirection = default;

			await AttachAndRun(ltrPage, _ =>
			{
				ltrCallbackExecuted = true;
				ltrLabelHandler = Assert.IsType<LabelHandler>(ltrLabel.Handler);
				ltrNativeLabel = Assert.IsType<WTextBlock>(ltrLabelHandler.PlatformView);
				ltrNativePadding = ltrNativeLabel.Padding;
				ltrWidth = ltrNativeLabel.ActualWidth;
				ltrHeight = ltrNativeLabel.ActualHeight;
				ltrNativeFlowDirection = ltrNativeLabel.FlowDirection;
			});

			Assert.True(ltrCallbackExecuted);
			Assert.True(ltrNativePadding.HasValue);
			Assert.NotNull(ltrNativeLabel);
			Assert.NotNull(ltrLabelHandler);
			Assert.Same(ltrLabel, ltrLabelHandler.VirtualView);
			Assert.Same(ltrNativeLabel, ltrLabelHandler.PlatformView);
			Assert.Equal("My Label", ltrNativeLabel.Text);
			Assert.True(ltrWidth > 0);
			Assert.True(ltrHeight > 0);
			Assert.Equal(WFlowDirection.LeftToRight, ltrNativeFlowDirection);
			Assert.Equal(Colors.Red, ltrStack.BackgroundColor);
			Assert.Equal(Colors.Yellow, ltrLabel.BackgroundColor);
			Assert.Equal(arrangedPadding, ltrLabel.Padding);
			Assert.True(Math.Abs(ltrNativePadding.Value.Left - arrangedPadding.Left) <= tolerance);
			Assert.True(Math.Abs(ltrNativePadding.Value.Right - arrangedPadding.Right) <= tolerance);

			var rtlLabel = new Label
			{
				Text = "My Label",
				Padding = arrangedPadding,
				BackgroundColor = Colors.Yellow
			};
			var rtlStack = new VerticalStackLayout
			{
				WidthRequest = 500,
				HeightRequest = 125,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				FlowDirection = FlowDirection.RightToLeft,
				BackgroundColor = Colors.Red,
				Children = { rtlLabel }
			};
			var rtlPage = new ContentPage
			{
				Content = new Grid
				{
					Children = { rtlStack }
				}
			};

			bool rtlCallbackExecuted = false;
			WThickness? rtlNativePadding = null;
			WTextBlock rtlNativeLabel = null;
			LabelHandler rtlLabelHandler = null;
			double rtlWidth = 0;
			double rtlHeight = 0;
			WFlowDirection rtlNativeFlowDirection = default;

			await AttachAndRun(rtlPage, _ =>
			{
				rtlCallbackExecuted = true;
				rtlLabelHandler = Assert.IsType<LabelHandler>(rtlLabel.Handler);
				rtlNativeLabel = Assert.IsType<WTextBlock>(rtlLabelHandler.PlatformView);
				rtlNativePadding = rtlNativeLabel.Padding;
				rtlWidth = rtlNativeLabel.ActualWidth;
				rtlHeight = rtlNativeLabel.ActualHeight;
				rtlNativeFlowDirection = rtlNativeLabel.FlowDirection;
			});

			Assert.True(rtlCallbackExecuted);
			Assert.True(rtlNativePadding.HasValue);
			Assert.NotNull(rtlNativeLabel);
			Assert.NotNull(rtlLabelHandler);
			Assert.Same(rtlLabel, rtlLabelHandler.VirtualView);
			Assert.Same(rtlNativeLabel, rtlLabelHandler.PlatformView);
			Assert.Equal("My Label", rtlNativeLabel.Text);
			Assert.True(rtlWidth > 0);
			Assert.True(rtlHeight > 0);
			Assert.Equal(WFlowDirection.RightToLeft, rtlNativeFlowDirection);
			Assert.Equal(Colors.Red, rtlStack.BackgroundColor);
			Assert.Equal(Colors.Yellow, rtlLabel.BackgroundColor);
			Assert.Equal(arrangedPadding, rtlLabel.Padding);

			double expectedLeft = arrangedPadding.Right;
			double expectedRight = arrangedPadding.Left;
			bool paddingIsCorrect =
				Math.Abs(rtlNativePadding.Value.Left - expectedLeft) <= tolerance &&
				Math.Abs(rtlNativePadding.Value.Right - expectedRight) <= tolerance;

			Assert.True(
				paddingIsCorrect,
				$"RTL Label native padding was incorrect. Expected Left={expectedLeft}, Right={expectedRight}; " +
				$"observed Left={rtlNativePadding.Value.Left}, Right={rtlNativePadding.Value.Right}.");
		}
	}
}


#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31585")]
	public class Issue31585 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RightToLeftPaddingIsMirrored()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var ltrLabel = new Label
			{
				BackgroundColor = Colors.Yellow,
				FlowDirection = FlowDirection.LeftToRight,
				Padding = new Thickness(20, 10, 80, 10),
				Text = "My Label",
				VerticalOptions = LayoutOptions.Start
			};
			var ltrContainer = new Grid
			{
				BackgroundColor = Colors.Red,
				HeightRequest = 200,
				WidthRequest = 400,
				Children = { ltrLabel }
			};
			var ltrStack = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children = { ltrContainer }
			};
			var ltrRoot = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				Children = { ltrStack }
			};
			Grid.SetRow(ltrStack, 1);
			var ltrPage = new ContentPage { Content = ltrRoot };
			var ltrLoaded = false;
			var ltrNativeLeft = double.NaN;
			var ltrNativeRight = double.NaN;

			await AttachAndRun(ltrPage, (PageHandler _) =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(ltrLabel.Handler);
				var nativeLabel = Assert.IsType<WTextBlock>(labelHandler.PlatformView);

				Assert.Same(ltrLabel, labelHandler.VirtualView);
				ltrLoaded = nativeLabel.IsLoaded;
				ltrNativeLeft = nativeLabel.Padding.Left;
				ltrNativeRight = nativeLabel.Padding.Right;
			});

			Assert.True(ltrLoaded, "The LTR Label's native TextBlock was not loaded.");
			Assert.False(double.IsNaN(ltrNativeLeft));
			Assert.False(double.IsNaN(ltrNativeRight));
			Assert.True(
				Math.Abs(ltrNativeLeft - ltrLabel.Padding.Left) <= 0.01 &&
				Math.Abs(ltrNativeRight - ltrLabel.Padding.Right) <= 0.01,
				$"LTR Label native padding did not match the arranged MAUI padding. Native={ltrNativeLeft},{ltrNativeRight}; MAUI={ltrLabel.Padding.Left},{ltrLabel.Padding.Right}");

			var rtlLabel = new Label
			{
				BackgroundColor = Colors.Yellow,
				FlowDirection = FlowDirection.RightToLeft,
				Padding = new Thickness(20, 10, 80, 10),
				Text = "My Label",
				VerticalOptions = LayoutOptions.Start
			};
			var rtlContainer = new Grid
			{
				BackgroundColor = Colors.Red,
				HeightRequest = 200,
				WidthRequest = 400,
				Children = { rtlLabel }
			};
			var rtlStack = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children = { rtlContainer }
			};
			var rtlRoot = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				Children = { rtlStack }
			};
			Grid.SetRow(rtlStack, 1);
			var rtlPage = new ContentPage { Content = rtlRoot };
			var rtlLoaded = false;
			var rtlNativeLeft = double.NaN;
			var rtlNativeRight = double.NaN;

			await AttachAndRun(rtlPage, (PageHandler _) =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(rtlLabel.Handler);
				var nativeLabel = Assert.IsType<WTextBlock>(labelHandler.PlatformView);

				Assert.Same(rtlLabel, labelHandler.VirtualView);
				rtlLoaded = nativeLabel.IsLoaded;
				rtlNativeLeft = nativeLabel.Padding.Left;
				rtlNativeRight = nativeLabel.Padding.Right;
			});

			Assert.True(rtlLoaded, "The RTL Label's native TextBlock was not loaded.");
			Assert.False(double.IsNaN(rtlNativeLeft));
			Assert.False(double.IsNaN(rtlNativeRight));
			Assert.Same(rtlRoot, rtlPage.Content);
			Assert.Same(rtlStack, rtlContainer.Parent);
			Assert.Same(rtlContainer, rtlLabel.Parent);
			Assert.Equal(1, Grid.GetRow(rtlStack));
			Assert.Equal("My Label", rtlLabel.Text);
			Assert.Equal(Colors.Red, rtlContainer.BackgroundColor);
			Assert.Equal(Colors.Yellow, rtlLabel.BackgroundColor);
			Assert.Equal(FlowDirection.RightToLeft, rtlLabel.FlowDirection);
			Assert.Equal(new Thickness(20, 10, 80, 10), rtlLabel.Padding);
			Assert.InRange(rtlContainer.Width, 399.99, 400.01);
			Assert.InRange(rtlContainer.Height, 199.99, 200.01);
			Assert.True(
				Math.Abs(rtlNativeLeft - rtlLabel.Padding.Right) <= 0.01 &&
				Math.Abs(rtlNativeRight - rtlLabel.Padding.Left) <= 0.01,
				$"RTL Label native padding was not mirrored. Observed left={rtlNativeLeft}, right={rtlNativeRight}; expected left={rtlLabel.Padding.Right}, right={rtlLabel.Padding.Left}.");
		}
	}
}
#endif


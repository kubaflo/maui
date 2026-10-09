#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
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
		const double Tolerance = 0.5;

		[Fact]
		public async Task RightToLeftLabelMapsStartPaddingToPhysicalRight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
				});
			});

			var (rtlPage, rtlLabel) = CreateHierarchy(FlowDirection.RightToLeft);
			var rtlAttached = false;
			var rtlLeft = double.NaN;
			var rtlRight = double.NaN;
			string rtlText = null;

			await CreateHandlerAndAddToWindow(rtlPage, () =>
			{
				rtlAttached = true;
				Assert.NotNull(rtlLabel.Handler);
				var textBlock = Assert.IsType<WTextBlock>(rtlLabel.Handler.PlatformView);

				rtlText = textBlock.Text;
				rtlLeft = textBlock.Padding.Left;
				rtlRight = textBlock.Padding.Right;
			});

			Assert.True(rtlAttached, "The RTL Label attachment callback did not run.");
			Assert.Equal("My Label", rtlText);
			Assert.Equal(360d, rtlLabel.WidthRequest);
			Assert.Equal(FlowDirection.RightToLeft, rtlLabel.FlowDirection);
			Assert.Equal(new Thickness(60, 0, 0, 0), rtlLabel.Padding);

			var expectedLeft = 0d;
			var expectedRight = rtlLabel.Padding.Left;
			Assert.True(
				Math.Abs(rtlLeft - expectedLeft) <= Tolerance &&
				Math.Abs(rtlRight - expectedRight) <= Tolerance,
				$"RTL Label native padding should map configured start padding to the physical right edge. " +
				$"Expected Left={expectedLeft}, Right={expectedRight}; observed Left={rtlLeft}, Right={rtlRight}.");
		}

		static (ContentPage Page, Label Label) CreateHierarchy(FlowDirection flowDirection)
		{
			var label = new Label
			{
				Text = "My Label",
				BackgroundColor = Colors.Yellow,
				WidthRequest = 360,
				Padding = new Thickness(60, 0, 0, 0),
				FlowDirection = flowDirection,
			};

			var stack = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Spacing = 0,
				Children =
				{
					label,
					new BoxView
					{
						Color = Colors.Red,
						WidthRequest = 360,
						HeightRequest = 100,
					},
				},
			};

			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
				},
				Children = { stack },
			};

			Grid.SetRow(stack, 0);
			return (new ContentPage { Content = grid }, label);
		}
	}
}
#endif


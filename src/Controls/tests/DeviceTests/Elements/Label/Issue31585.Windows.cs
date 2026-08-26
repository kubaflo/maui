using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WThickness = Microsoft.UI.Xaml.Thickness;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31585")]
	public class Issue31585 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task AsymmetricLabelPaddingFollowsRightToLeftFlowDirection()
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
					handlers.AddHandler<BoxView, BoxViewHandler>();
				});
			});

			var leftToRightRoot = CreateHierarchy(FlowDirection.LeftToRight, out var leftToRightLabel);
			var leftToRightPadding = new WThickness(-1);

			await AttachAndRun<LayoutHandler>(leftToRightRoot, async _ =>
			{
				await AssertEventually(
					() => leftToRightLabel.Handler?.PlatformView is WTextBlock textBlock &&
						textBlock.ActualWidth > 0 &&
						textBlock.ActualHeight > 0,
					message: "The LTR Label was not attached and laid out.");

				var textBlock = Assert.IsType<WTextBlock>(leftToRightLabel.Handler.PlatformView);
				leftToRightPadding = textBlock.Padding;
			});

			Assert.Equal(50, leftToRightPadding.Left, 0.01);
			Assert.Equal(0, leftToRightPadding.Right, 0.01);

			var rightToLeftRoot = CreateHierarchy(FlowDirection.RightToLeft, out var rightToLeftLabel);
			var nativePadding = new WThickness(-1);
			WTextBlock platformLabel = null;
			bool attachmentCallbackOccurred = false;

			rightToLeftLabel.HandlerChanged += (_, _) =>
			{
				if (rightToLeftLabel.Handler?.PlatformView is WTextBlock textBlock)
				{
					platformLabel = textBlock;
					textBlock.Loaded += (_, _) =>
					{
						nativePadding = textBlock.Padding;
						attachmentCallbackOccurred = true;
					};
				}
			};

			await AttachAndRun<LayoutHandler>(rightToLeftRoot, async _ =>
			{
				await AssertEventually(
					() => attachmentCallbackOccurred,
					message: "The RTL Label native attachment callback did not occur.");

				Assert.True(attachmentCallbackOccurred);
				Assert.NotNull(platformLabel);
				Assert.IsType<LabelHandler>(rightToLeftLabel.Handler);
				Assert.Same(platformLabel, rightToLeftLabel.Handler.PlatformView);
				Assert.Equal("My Label", platformLabel.Text);
				Assert.Equal(FlowDirection.RightToLeft, rightToLeftLabel.FlowDirection);
				Assert.Equal(50, rightToLeftLabel.Padding.Left);
				Assert.Equal(0, rightToLeftLabel.Padding.Right);
				Assert.True(platformLabel.ActualWidth > 0);
				Assert.True(platformLabel.ActualHeight > 0);
			});

			bool paddingIsOnVisualRight =
				Math.Abs(nativePadding.Left) <= 0.01 &&
				Math.Abs(nativePadding.Right - rightToLeftLabel.Padding.Left) <= 0.01;

			Assert.True(
				paddingIsOnVisualRight,
				$"RTL Label native padding was applied to the wrong visual sides. Expected Left=0 and Right=50, but was Left={nativePadding.Left} and Right={nativePadding.Right}.");
		}

		static Grid CreateHierarchy(FlowDirection flowDirection, out Label label)
		{
			label = new Label
			{
				BackgroundColor = Colors.Yellow,
				FlowDirection = flowDirection,
				Padding = new Thickness(50, 0, 0, 0),
				Text = "My Label"
			};

			var stack = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				Spacing = 0,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					label,
					new BoxView
					{
						Color = Colors.Red,
						HeightRequest = 100,
						WidthRequest = 360
					}
				}
			};

			return new Grid
			{
				Padding = 24,
				Children = { stack }
			};
		}
	}
}


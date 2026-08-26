#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WPath = Microsoft.UI.Xaml.Shapes.Path;
using WPenLineCap = Microsoft.UI.Xaml.Media.PenLineCap;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29741")]
	public class Issue29741 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task StrokeLineCapMapsToNativeDashCap()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			ContentPage page = null;
			Border flatBorder = null;
			Border roundBorder = null;
			Border squareBorder = null;

			await InvokeOnMainThreadAsync(() =>
			{
				Border CreateBorder(PenLineCap lineCap) => new()
				{
					BackgroundColor = Colors.LightGray,
					HeightRequest = 140,
					HorizontalOptions = LayoutOptions.Center,
					Stroke = Colors.Blue,
					StrokeDashArray = new DoubleCollection { 5, 3 },
					StrokeLineCap = lineCap,
					StrokeThickness = 10,
					WidthRequest = 360
				};

				flatBorder = CreateBorder(PenLineCap.Flat);
				roundBorder = CreateBorder(PenLineCap.Round);
				squareBorder = CreateBorder(PenLineCap.Square);

				page = new ContentPage
				{
					Content = new ScrollView
					{
						Content = new VerticalStackLayout
						{
							Padding = 28,
							Spacing = 12,
							Children =
							{
								new Label { FontSize = 22, Text = "StrokeLineCap - Flat" },
								flatBorder,
								new Label { FontSize = 22, Text = "StrokeLineCap - Round" },
								roundBorder,
								new Label { FontSize = 22, Text = "StrokeLineCap - Square" },
								squareBorder
							}
						}
					}
				};
			});

			const int NotAttached = -1;
			const WPenLineCap Unobserved = (WPenLineCap)(-1);
			var attachmentState = NotAttached;
			var flatCap = Unobserved;
			var roundCap = Unobserved;
			var squareCap = Unobserved;

			async Task<WPath> GetRenderedPath(Border border, string borderName)
			{
				BorderHandler borderHandler = null;
				await AssertEventually(
					() =>
					{
						borderHandler = border.Handler as BorderHandler;
						return borderHandler?.PlatformView is not null;
					},
					message: $"{borderName} BorderHandler platform view was not created");

				Assert.NotNull(borderHandler);
				Assert.NotNull(borderHandler.PlatformView);

				WPath renderedPath = null;
				await AssertEventually(
					() =>
					{
						var childCount = WVisualTreeHelper.GetChildrenCount(borderHandler.PlatformView);
						for (var index = 0; index < childCount; index++)
						{
							if (WVisualTreeHelper.GetChild(borderHandler.PlatformView, index) is WPath path)
							{
								renderedPath = path;
								return true;
							}
						}

						return false;
					},
					message: $"{borderName} Border rendered Path was not created");

				Assert.NotNull(renderedPath);
				return renderedPath;
			}

			await AttachAndRun<PageHandler>(page, async _ =>
			{
				attachmentState = 1;
				flatCap = (await GetRenderedPath(flatBorder, "Flat")).StrokeDashCap;
				roundCap = (await GetRenderedPath(roundBorder, "Round")).StrokeDashCap;
				squareCap = (await GetRenderedPath(squareBorder, "Square")).StrokeDashCap;
			});

			Assert.NotEqual(NotAttached, attachmentState);
			Assert.NotEqual(Unobserved, flatCap);
			Assert.NotEqual(Unobserved, roundCap);
			Assert.NotEqual(Unobserved, squareCap);
			Assert.Equal(WPenLineCap.Flat, flatCap);
			Assert.True(
				flatCap == WPenLineCap.Flat &&
				roundCap == WPenLineCap.Round &&
				squareCap == WPenLineCap.Square,
				$"StrokeLineCap mapping failed: Flat Border native StrokeDashCap={flatCap} expected Flat; Round Border native StrokeDashCap={roundCap} expected Round; Square Border native StrokeDashCap={squareCap} expected Square");
		}
	}
}
#endif


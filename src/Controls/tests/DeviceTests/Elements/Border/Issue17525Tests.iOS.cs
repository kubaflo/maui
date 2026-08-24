#if MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using MauiContentView = Microsoft.Maui.Platform.ContentView;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double BorderSize = 101;
		const double StrokeThickness = 8;
		const nint BorderContentTag = 0x63D2A0;

		[Fact]
		public async Task PolygonContentIsClippedToInnerPath()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var ellipseBorder = CreateBorder(new Ellipse());
			var roundRectangleBorder = CreateBorder(new RoundRectangle());
			var polygon = new Polygon
			{
				Points = new PointCollection(
				[
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				]),
				StrokeThickness = 3
			};
			var polygonBorder = CreateBorder(polygon);
			var polygonLabel = (Label)polygonBorder.Content;
			var loadedObservation = -1;
			polygonBorder.Loaded += (_, _) => loadedObservation = 1;

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center
			};
			grid.Add(ellipseBorder, 0, 0);
			grid.Add(roundRectangleBorder, 0, 1);
			grid.Add(polygonBorder, 0, 2);

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Spacing = 10,
					VerticalOptions = LayoutOptions.Center,
					Children = { grid }
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Equal(1, loadedObservation);

				var polygonHandler = Assert.IsType<BorderHandler>(polygonBorder.Handler);
				var polygonPlatformView = Assert.IsType<MauiContentView>(polygonHandler.PlatformView);
				var taggedContent = Assert.Single(polygonPlatformView.Subviews, view => view.Tag == BorderContentTag);
				var polygonLabelPlatformView = Assert.IsAssignableFrom<UIKit.UIView>(polygonLabel.Handler.PlatformView);
				Assert.True(
					polygonLabelPlatformView == taggedContent || polygonLabelPlatformView.IsDescendantOfView(taggedContent),
					"The tagged Border content did not contain the rendered Label platform view.");
				Assert.InRange(polygonPlatformView.Frame.Width, BorderSize - 0.5, BorderSize + 0.5);
				Assert.InRange(polygonPlatformView.Frame.Height, BorderSize - 0.5, BorderSize + 0.5);
				Assert.InRange(taggedContent.Frame.Width, 1, BorderSize);
				Assert.InRange(taggedContent.Frame.Height, 1, BorderSize);

				var roundRectangleBitmap = await roundRectangleBorder.AsRawBitmapAsync();
				var polygonBitmap = await polygonBorder.AsRawBitmapAsync();

				var roundRectangleResult = MeasureRenderedContent(roundRectangleBitmap, null);
				Assert.True(roundRectangleResult.RedPixelCount >= 20 * roundRectangleBitmap.Density,
					"RoundRectangle did not render the recorded red Label content.");
				Assert.True(roundRectangleResult.GreenPixelCount >= 20 * roundRectangleBitmap.Density,
					"RoundRectangle did not render the recorded light-green stroke.");
				Assert.True(roundRectangleResult.MinimumInset >= StrokeThickness - 1.5,
					$"RoundRectangle control did not establish a clean inner-path oracle: minimum inset {roundRectangleResult.MinimumInset:F2}, expected {StrokeThickness:F2}, tolerance 1.50.");

				using var outerPath = ((IShape)polygon).PathForBounds(new Rect(0, 0, polygonPlatformView.Frame.Width, polygonPlatformView.Frame.Height));
				var outerPoints = outerPath.Points.Take(3).ToArray();
				Assert.Equal(3, outerPoints.Length);

				var polygonResult = MeasureRenderedContent(polygonBitmap, outerPoints);
				Assert.True(polygonResult.RedPixelCount >= 20 * polygonBitmap.Density,
					"Polygon did not render the recorded red Label content.");
				Assert.True(polygonResult.GreenPixelCount >= 20 * polygonBitmap.Density,
					"Polygon did not render the recorded light-green outer edge.");

				const double tolerance = 1.5;
				Assert.True(
					polygonResult.MinimumInset >= StrokeThickness - tolerance,
					$"Polygon content pixels crossed the expected inner path: minimum inset {polygonResult.MinimumInset:F2}, expected inset {StrokeThickness:F2}, tolerance {tolerance:F2}, violating-pixel count {polygonResult.ViolatingPixelCount}.");
			});
		}

		static Border CreateBorder(IShape shape)
		{
			return new Border
			{
				WidthRequest = BorderSize,
				HeightRequest = BorderSize,
				BackgroundColor = Colors.LightBlue,
				Stroke = Colors.LightGreen,
				StrokeThickness = StrokeThickness,
				StrokeShape = shape,
				Content = new Label
				{
					Text = "+",
					TextColor = Color.FromArgb("#0088ee"),
					BackgroundColor = Color.FromArgb("#99FF0000"),
					FontSize = 40,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};
		}

		static RenderedContentResult MeasureRenderedContent(RawBitmap bitmap, PointF[] polygonPoints)
		{
			var minimumInset = double.MaxValue;
			var redPixelCount = 0;
			var greenPixelCount = 0;
			var violatingPixelCount = 0;

			for (var row = 0; row < bitmap.PixelHeight; row++)
			{
				for (var column = 0; column < bitmap.PixelWidth; column++)
				{
					var offset = (row * bitmap.PixelWidth + column) * 4;
					var blue = bitmap.PixelBuffer[offset];
					var green = bitmap.PixelBuffer[offset + 1];
					var red = bitmap.PixelBuffer[offset + 2];
					var alpha = bitmap.PixelBuffer[offset + 3];

					if (alpha >= 240 && green >= 150 && green > red + 20 && green > blue + 20)
						greenPixelCount++;

					if (alpha < 240 || red < 180 || red < green + 60 || red < blue + 40)
						continue;

					redPixelCount++;
					var x = (column + 0.5) / bitmap.Density;
					var y = (row + 0.5) / bitmap.Density;
					var inset = polygonPoints is null
						? Math.Min(Math.Min(x, bitmap.Width - x), Math.Min(y, bitmap.Height - y))
						: MinimumDistanceToEdges(x, y, polygonPoints);

					minimumInset = Math.Min(minimumInset, inset);
					if (inset < StrokeThickness - 1.5)
						violatingPixelCount++;
				}
			}

			return new RenderedContentResult(minimumInset, redPixelCount, greenPixelCount, violatingPixelCount);
		}

		static double MinimumDistanceToEdges(double x, double y, PointF[] points)
		{
			var minimumDistance = double.MaxValue;
			var signedArea = 0d;
			for (var index = 0; index < points.Length; index++)
			{
				var start = points[index];
				var end = points[(index + 1) % points.Length];
				signedArea += start.X * end.Y - end.X * start.Y;
			}
			var inwardSign = Math.Sign(signedArea);

			for (var index = 0; index < points.Length; index++)
			{
				var start = points[index];
				var end = points[(index + 1) % points.Length];
				var edgeX = end.X - start.X;
				var edgeY = end.Y - start.Y;
				var edgeLength = Math.Sqrt(edgeX * edgeX + edgeY * edgeY);
				var distance = inwardSign * (edgeX * (y - start.Y) - edgeY * (x - start.X)) / edgeLength;
				minimumDistance = Math.Min(minimumDistance, distance);
			}

			return minimumDistance;
		}

		readonly record struct RenderedContentResult(
			double MinimumInset,
			int RedPixelCount,
			int GreenPixelCount,
			int ViolatingPixelCount);
	}
}
#endif


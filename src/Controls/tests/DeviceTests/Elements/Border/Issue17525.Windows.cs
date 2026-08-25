#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double RequestedSize = 101;
		const double BorderStrokeThickness = 8;
		const double ShapeStrokeThickness = 3;

		[Fact]
		public async Task PolygonContentDoesNotCoverInnerStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var rectangleScene = CreateScene(new RoundRectangle { StrokeThickness = ShapeStrokeThickness });
			var rectangleBitmap = await AttachAndRun(rectangleScene.Root, async _ =>
			{
				Assert.IsType<BorderHandler>(rectangleScene.TargetBorder.Handler);
				Assert.IsType<LabelHandler>(rectangleScene.ContentLabel.Handler);
				return await rectangleScene.TargetBorder.AsRawBitmapAsync();
			});

			var rectangleVertices = new[]
			{
				new Point(BorderStrokeThickness / 2, BorderStrokeThickness / 2),
				new Point(RequestedSize - BorderStrokeThickness / 2, BorderStrokeThickness / 2),
				new Point(RequestedSize - BorderStrokeThickness / 2, RequestedSize - BorderStrokeThickness / 2),
				new Point(BorderStrokeThickness / 2, RequestedSize - BorderStrokeThickness / 2),
			};
			var rectangleResult = AnalyzeBitmap(rectangleBitmap, rectangleVertices, BorderStrokeThickness / 2);

			Assert.True(rectangleResult.ContentPixels > 0, "The rectangle control did not render any classifiable content pixels.");
			Assert.True(rectangleResult.BandSamples > 0, "The rectangle inner-stroke band did not map to the rendered bitmap.");
			Assert.Equal(0, rectangleResult.LeakedContentPixels);

			var polygon = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50),
				},
				StrokeThickness = ShapeStrokeThickness,
			};
			var polygonScene = CreateScene(polygon);
			var observedSize = new Size(-1, -1);
			var sizeChanged = false;
			var sizeChangedSource = new TaskCompletionSource();

			polygonScene.TargetBorder.SizeChanged += OnTargetBorderSizeChanged;

			var polygonCapture = await AttachAndRun(polygonScene.Root, async _ =>
			{
				await sizeChangedSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

				var borderHandler = Assert.IsType<BorderHandler>(polygonScene.TargetBorder.Handler);
				var labelHandler = Assert.IsType<LabelHandler>(polygonScene.ContentLabel.Handler);
				Assert.NotNull(borderHandler.PlatformView);
				Assert.NotNull(labelHandler.PlatformView);
				Assert.True(labelHandler.PlatformView.ActualWidth > 0);
				Assert.True(labelHandler.PlatformView.ActualHeight > 0);
				Assert.Equal(RequestedSize, borderHandler.PlatformView.ActualWidth, 0.5);
				Assert.Equal(RequestedSize, borderHandler.PlatformView.ActualHeight, 0.5);

				return (
					Bitmap: await polygonScene.TargetBorder.AsRawBitmapAsync(),
					UsesInnerPath: borderHandler.PlatformView.IsInnerPath);
			});
			var polygonBitmap = polygonCapture.Bitmap;

			polygonScene.TargetBorder.SizeChanged -= OnTargetBorderSizeChanged;

			Assert.True(sizeChanged, "The Border did not report its post-attachment size.");
			Assert.NotEqual(new Size(-1, -1), observedSize);
			Assert.Equal(RequestedSize, observedSize.Width, 0.5);
			Assert.Equal(RequestedSize, observedSize.Height, 0.5);
			Assert.Equal(RequestedSize, polygonBitmap.Width, 1 / polygonBitmap.Density);
			Assert.Equal(RequestedSize, polygonBitmap.Height, 1 / polygonBitmap.Density);

			var renderedVertices = GetRenderedVertices(
				polygon.Points,
				observedSize,
				BorderStrokeThickness,
				polygon.StrokeThickness);
			var polygonResult = AnalyzeBitmap(polygonBitmap, renderedVertices, BorderStrokeThickness / 2);

			Assert.True(polygonResult.ContentPixels > 0, "The Polygon Border did not render any classifiable content pixels.");
			Assert.True(polygonResult.BandSamples > 0, "The Polygon Border inner-stroke band did not map to the rendered bitmap.");
			Assert.True(
				polygonCapture.UsesInnerPath && polygonResult.LeakedContentPixels == 0,
				$"Polygon Border content crossed the inner stroke edge: native inner path {polygonCapture.UsesInnerPath}, measured {polygonResult.LeakedContentPixels}, expected 0; frame {observedSize.Width:F2}x{observedSize.Height:F2}, density {polygonBitmap.Density:F2}, inset {BorderStrokeThickness / 2:F2}.");

			void OnTargetBorderSizeChanged(object sender, EventArgs args)
			{
				sizeChanged = true;
				observedSize = new Size(polygonScene.TargetBorder.Width, polygonScene.TargetBorder.Height);
				sizeChangedSource.TrySetResult();
			}
		}

		static (Grid Root, Border TargetBorder, Label ContentLabel) CreateScene(Shape strokeShape)
		{
			var contentLabel = new Label
			{
				BackgroundColor = Color.FromRgba(255, 0, 0, 153),
				FontSize = 64,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Text = "+",
				TextColor = Color.FromRgb(0, 136, 238),
			};
			var targetBorder = new Border
			{
				BackgroundColor = Colors.LightBlue,
				Content = contentLabel,
				HeightRequest = RequestedSize,
				Stroke = Colors.LightGreen,
				StrokeShape = strokeShape,
				StrokeThickness = BorderStrokeThickness,
				WidthRequest = RequestedSize,
			};
			var root = new Grid
			{
				Padding = 24,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
				},
				RowSpacing = 16,
			};
			var targetContainer = new Grid
			{
				VerticalOptions = LayoutOptions.Center,
			};
			targetContainer.Add(targetBorder);
			root.Add(targetContainer, row: 2);

			return (root, targetBorder, contentLabel);
		}

		static Point[] GetRenderedVertices(PointCollection points, Size surfaceSize, double borderStroke, double shapeStroke)
		{
			var minX = double.MaxValue;
			var minY = double.MaxValue;
			var maxX = double.MinValue;
			var maxY = double.MinValue;

			foreach (var point in points)
			{
				minX = Math.Min(minX, point.X);
				minY = Math.Min(minY, point.Y);
				maxX = Math.Max(maxX, point.X);
				maxY = Math.Max(maxY, point.Y);
			}

			var left = shapeStroke / 2;
			var top = shapeStroke / 2;
			var right = surfaceSize.Width - borderStroke - shapeStroke / 2;
			var bottom = surfaceSize.Height - borderStroke - shapeStroke / 2;
			var translateX = left > minX ? left - minX : maxX > right ? right - maxX : 0;
			var translateY = top > minY ? top - minY : maxY > bottom ? bottom - maxY : 0;
			var vertices = new Point[points.Count];

			for (var i = 0; i < points.Count; i++)
			{
				vertices[i] = new Point(
					points[i].X + translateX + borderStroke / 2,
					points[i].Y + translateY + borderStroke / 2);
			}

			return vertices;
		}

		static PixelAnalysisResult AnalyzeBitmap(RawBitmap bitmap, Point[] centerlineVertices, double innerInset)
		{
			var antialiasBand = 1.25 / bitmap.Density;
			var contentPixels = 0;
			var leakedContentPixels = 0;
			var bandSamples = 0;
			var orientation = GetSignedArea(centerlineVertices) >= 0 ? 1 : -1;

			for (var row = 0; row < bitmap.PixelHeight; row++)
			{
				for (var column = 0; column < bitmap.PixelWidth; column++)
				{
					var point = new Point((column + 0.5) / bitmap.Density, (row + 0.5) / bitmap.Density);
					var distance = GetMinimumSignedDistance(point, centerlineVertices, orientation);
					var inForbiddenBand = distance < innerInset - antialiasBand;
					var isContent = IsContentPixel(bitmap, column, row);

					if (isContent)
						contentPixels++;

					if (inForbiddenBand)
					{
						bandSamples++;
						if (isContent)
							leakedContentPixels++;
					}
				}
			}

			return new PixelAnalysisResult(contentPixels, leakedContentPixels, bandSamples);
		}

		static double GetSignedArea(Point[] vertices)
		{
			var area = 0d;
			for (var i = 0; i < vertices.Length; i++)
			{
				var next = vertices[(i + 1) % vertices.Length];
				area += vertices[i].X * next.Y - next.X * vertices[i].Y;
			}

			return area / 2;
		}

		static double GetMinimumSignedDistance(Point point, Point[] vertices, int orientation)
		{
			var minimumDistance = double.MaxValue;
			for (var i = 0; i < vertices.Length; i++)
			{
				var start = vertices[i];
				var end = vertices[(i + 1) % vertices.Length];
				var edgeX = end.X - start.X;
				var edgeY = end.Y - start.Y;
				var edgeLength = Math.Sqrt(edgeX * edgeX + edgeY * edgeY);
				var crossProduct = edgeX * (point.Y - start.Y) - edgeY * (point.X - start.X);
				minimumDistance = Math.Min(minimumDistance, orientation * crossProduct / edgeLength);
			}

			return minimumDistance;
		}

		static bool IsContentPixel(RawBitmap bitmap, int column, int row)
		{
			var index = (row * bitmap.PixelWidth + column) * 4;
			var blue = bitmap.PixelBuffer[index];
			var green = bitmap.PixelBuffer[index + 1];
			var red = bitmap.PixelBuffer[index + 2];
			var alpha = bitmap.PixelBuffer[index + 3];

			return alpha > 200 && red > 170 && green < 130 && blue < 130 && red > green + 70 && red > blue + 70;
		}

		readonly record struct PixelAnalysisResult(int ContentPixels, int LeakedContentPixels, int BandSamples);
	}
}
#endif


#if MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border, "Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double ExpectedInset = 8;
		const double PixelTolerance = 1.5;

		[Fact]
		public async Task PolygonContentClipUsesUniformStrokeInset()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var zeroStroke = await CaptureTriangle(0);
			var zeroStrokeInsets = MeasureContentInsets(zeroStroke.OuterPoints, zeroStroke.ClipPoints);

			Assert.True(
				zeroStrokeInsets.All(inset => Math.Abs(inset) <= PixelTolerance),
				$"Zero-stroke Polygon boundaries did not coincide: {FormatInsets(zeroStrokeInsets)}.");

			var stroked = await CaptureTriangle(ExpectedInset);
			var insets = MeasureContentInsets(stroked.OuterPoints, stroked.ClipPoints);
			var spread = insets.Max() - insets.Min();
			var everyInsetIsExpected = insets.All(inset => Math.Abs(inset - ExpectedInset) <= PixelTolerance);

			Assert.True(
				everyInsetIsExpected && spread <= PixelTolerance,
				$"Polygon Border inner clip edge insets were not uniform: {FormatInsets(insets)}; expected {ExpectedInset:F1}.");
		}

		async Task<(Point[] OuterPoints, Point[] ClipPoints)> CaptureTriangle(double strokeThickness)
		{
			var (border, polygon) = CreateTriangleBorder(strokeThickness);
			var grid = new Grid
			{
				WidthRequest = 101,
				HeightRequest = 101,
			};
			grid.Add(border);

			var loadedObserved = false;
			var arrangedFrame = new Rect(-1, -1, -1, -1);
			border.Loaded += (_, _) => loadedObserved = true;
			border.SizeChanged += (_, _) => arrangedFrame = border.Frame;

			var bitmap = await GetRawBitmap(grid, typeof(LayoutHandler)).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(101, bitmap.Width, PixelTolerance);
			Assert.Equal(101, bitmap.Height, PixelTolerance);
			Assert.True(loadedObserved, "The Polygon Border did not reach its native loaded state.");
			Assert.True(arrangedFrame.Width > 0 && arrangedFrame.Height > 0, "The Polygon Border did not reach its arranged state.");
			Assert.Equal(101, arrangedFrame.Width, PixelTolerance);
			Assert.Equal(101, arrangedFrame.Height, PixelTolerance);

			var nativeView = Assert.IsType<Microsoft.Maui.Platform.ContentView>(border.Handler.PlatformView);
			Assert.Equal(101, (double)nativeView.Frame.Width, PixelTolerance);
			Assert.Equal(101, (double)nativeView.Frame.Height, PixelTolerance);

			var nativeContent = Assert.Single(nativeView.Subviews);
			var mask = Assert.IsAssignableFrom<CAShapeLayer>(nativeContent.Layer.Mask);
			var nativePath = mask.Path;
			Assert.NotNull(nativePath);

			var pathPoints = nativePath.AsPathF().Points.Take(3).ToArray();
			Assert.Equal(3, pathPoints.Length);

			var originX = (double)nativeContent.Frame.X + (double)mask.Position.X
				- ((double)mask.AnchorPoint.X * (double)mask.Bounds.Width) - (double)mask.Bounds.X;
			var originY = (double)nativeContent.Frame.Y + (double)mask.Position.Y
				- ((double)mask.AnchorPoint.Y * (double)mask.Bounds.Height) - (double)mask.Bounds.Y;
			var clipPoints = pathPoints
				.Select(point => new Point(point.X + originX, point.Y + originY))
				.ToArray();
			var outerPoints = ((IShape)polygon)
				.PathForBounds(new Rect(0, 0, nativeView.Bounds.Width, nativeView.Bounds.Height))
				.Points
				.Take(3)
				.Select(point => new Point(point.X, point.Y))
				.ToArray();
			Assert.Equal(3, outerPoints.Length);

			return (outerPoints, clipPoints);
		}

		static (Border Border, Polygon Polygon) CreateTriangleBorder(double strokeThickness)
		{
			var polygon = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50),
				},
				StrokeThickness = 3,
			};
			var label = new Label
			{
				BackgroundColor = Color.FromArgb("#99FF0000"),
				FontSize = 40,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				Text = "+",
				TextColor = Color.FromArgb("#0088ee"),
			};

			var border = new Border
			{
				WidthRequest = 101,
				HeightRequest = 101,
				StrokeShape = polygon,
				StrokeThickness = strokeThickness,
				Stroke = Colors.LightGreen,
				BackgroundColor = Colors.LightBlue,
				Content = label,
			};

			return (border, polygon);
		}

		static double[] MeasureContentInsets(Point[] outerPoints, Point[] clipPoints) =>
			outerPoints
				.Select((point, index) => DistanceToEdge(point, outerPoints[(index + 1) % outerPoints.Length], clipPoints[index]))
				.ToArray();

		static double DistanceToEdge(Point start, Point end, Point point)
		{
			var edgeX = end.X - start.X;
			var edgeY = end.Y - start.Y;
			var pointX = point.X - start.X;
			var pointY = point.Y - start.Y;
			var edgeLength = Math.Sqrt((edgeX * edgeX) + (edgeY * edgeY));
			return Math.Abs((edgeX * pointY) - (edgeY * pointX)) / edgeLength;
		}

		static string FormatInsets(double[] insets) =>
			string.Join(", ", insets.Select(inset => inset.ToString("F1")));
	}
}
#endif


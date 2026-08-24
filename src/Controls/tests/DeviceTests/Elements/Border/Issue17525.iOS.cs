#if MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double BorderSize = 101;
		const double BorderStrokeThickness = 8;

		[Fact]
		public async Task PolygonContentIsClippedInsideStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var polygonPoints = new PointCollection
			{
				new Point(40, 10),
				new Point(70, 80),
				new Point(10, 50),
			};

			var circleLabelBorder = CreateBorder(new Ellipse(), CreateLabel());
			var roundRectangleLabelBorder = CreateBorder(new RoundRectangle(), CreateLabel());
			var polygonLabel = CreateLabel();
			var polygonLabelBorder = CreateBorder(
				new Polygon { Points = polygonPoints, StrokeThickness = 3 },
				polygonLabel);
			var circleImageBorder = CreateBorder(new Ellipse(), CreateImage());
			var roundRectangleImageBorder = CreateBorder(new RoundRectangle(), CreateImage());
			var polygonImage = CreateImage();
			var polygonImageBorder = CreateBorder(
				new Polygon
				{
					Points = new PointCollection(polygonPoints.ToArray()),
					StrokeThickness = 3,
				},
				polygonImage);

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Auto),
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center,
			};

			grid.Add(circleLabelBorder, 0, 0);
			grid.Add(roundRectangleLabelBorder, 0, 1);
			grid.Add(polygonLabelBorder, 0, 2);
			grid.Add(circleImageBorder, 1, 0);
			grid.Add(roundRectangleImageBorder, 1, 1);
			grid.Add(polygonImageBorder, 1, 2);

			var layout = new VerticalStackLayout
			{
				VerticalOptions = LayoutOptions.Center,
				Children = { grid },
			};
			var page = new ContentPage { Content = layout };
			var borders = new[]
			{
				circleLabelBorder,
				roundRectangleLabelBorder,
				polygonLabelBorder,
				circleImageBorder,
				roundRectangleImageBorder,
				polygonImageBorder,
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				foreach (var border in borders)
				{
				Assert.NotNull(border.Handler?.PlatformView);
				Assert.NotNull(border.Content?.Handler?.PlatformView);
				}

				await AssertEventually(
				() => borders.All(border =>
				{
					var nativeBorder = (UIView)border.Handler.PlatformView;
					var nativeContent = (UIView)border.Content.Handler.PlatformView;
					return nativeBorder.Frame.Width > 0 &&
						nativeBorder.Frame.Height > 0 &&
						nativeContent.Frame.Width > 0 &&
						nativeContent.Frame.Height > 0;
				}),
					timeout: 2000,
					message: "The recorded Border hierarchy did not receive nonzero native frames.");

				Assert.Contains(polygonLabelBorder, grid.Children);
				Assert.Equal(2, Grid.GetRow(polygonLabelBorder));
				Assert.Equal(0, Grid.GetColumn(polygonLabelBorder));
				Assert.Contains(polygonImageBorder, grid.Children);
				Assert.Equal(2, Grid.GetRow(polygonImageBorder));
				Assert.Equal(1, Grid.GetColumn(polygonImageBorder));
				Assert.Same(polygonLabel, polygonLabelBorder.Content);
				Assert.Same(polygonImage, polygonImageBorder.Content);
				Assert.Equal(Colors.LightBlue, polygonLabelBorder.BackgroundColor);
				Assert.Equal(Colors.LightGreen, Assert.IsType<SolidColorBrush>(polygonLabelBorder.Stroke).Color);
				Assert.Equal(BorderStrokeThickness, polygonLabelBorder.StrokeThickness);
				Assert.Equal("+", polygonLabel.Text);
				Assert.Equal(Color.FromArgb("#99FF0000"), polygonLabel.BackgroundColor);
				Assert.NotNull(polygonImage.Source);
				Assert.Equal(1, polygonImage.Scale);

				var polygon = Assert.IsType<Polygon>(polygonLabelBorder.StrokeShape);
				Assert.Equal(3, polygon.StrokeThickness);
				Assert.Equal(polygonPoints.ToArray(), polygon.Points.ToArray());

				CapturedBitmap[] bitmaps = new CapturedBitmap[borders.Length];
				for (int i = 0; i < borders.Length; i++)
				{
					bitmaps[i] = await CaptureBitmap(borders[i]);
					Assert.InRange(bitmaps[i].Width, BorderSize - 1, BorderSize + 1);
					Assert.InRange(bitmaps[i].Height, BorderSize - 1, BorderSize + 1);
				}

				var referenceLeakCount = CountContentPixelsInRoundRectangleStrokeBand(bitmaps[1]);
				var edgeLength = PolygonPerimeter(polygonPoints);
				var antialiasingAllowance = Math.Max(1, (int)Math.Ceiling(edgeLength * bitmaps[2].Density * 0.01));
				Assert.True(
					referenceLeakCount <= antialiasingAllowance,
					$"RoundRectangle reference leaked {referenceLeakCount} content pixels into its stroke band; expected at most {antialiasingAllowance}.");

				var polygonLeakCount = CountContentPixelsInPolygonStrokeBand(bitmaps[2], polygonPoints);
				Assert.True(
					polygonLeakCount <= antialiasingAllowance,
					$"Polygon inner clip leaked content into the stroke band: observed {polygonLeakCount} pixels in 101x101 bounds; tolerance {antialiasingAllowance}, expected maximum {antialiasingAllowance}.");
			});
		}

		static Border CreateBorder(Shape shape, View content) =>
			new Border
			{
				WidthRequest = BorderSize,
				HeightRequest = BorderSize,
				BackgroundColor = Colors.LightBlue,
				Stroke = Colors.LightGreen,
				StrokeThickness = BorderStrokeThickness,
				StrokeShape = shape,
				Content = content,
			};

		static Label CreateLabel() =>
			new Label
			{
				BackgroundColor = Color.FromArgb("#99FF0000"),
				FontSize = 40,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Text = "+",
				TextColor = Color.FromArgb("#0088ee"),
			};

		static Image CreateImage() =>
			new Image
			{
				Source = "dotnet_bot.png",
				Scale = 1,
			};

		async Task<CapturedBitmap> CaptureBitmap(Border border)
		{
			var nativeBorder = (UIView)border.Handler.PlatformView;
			using var image = await nativeBorder.ToBitmap(MauiContext);
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);

			var width = (int)cgImage.Width;
			var height = (int)cgImage.Height;
			var pixels = new byte[width * height * 4];
			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(
				pixels,
				width,
				height,
				8,
				4 * width,
				colorSpace,
				CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
			context.DrawImage(new CGRect(0, 0, width, height), cgImage);

			return new CapturedBitmap
			{
				PixelBuffer = pixels,
				PixelWidth = width,
				PixelHeight = height,
				Density = width / (double)image.Size.Width,
				Width = image.Size.Width,
				Height = image.Size.Height,
			};
		}

		static int CountContentPixelsInRoundRectangleStrokeBand(CapturedBitmap bitmap)
		{
			var inset = BorderStrokeThickness * bitmap.Density;
			int count = 0;

			for (int y = 0; y < bitmap.PixelHeight; y++)
			{
				for (int x = 0; x < bitmap.PixelWidth; x++)
				{
					if ((x < inset || y < inset || x >= bitmap.PixelWidth - inset || y >= bitmap.PixelHeight - inset) &&
						IsLabelBackground(bitmap, x, y))
					{
						count++;
					}
				}
			}

			return count;
		}

		static int CountContentPixelsInPolygonStrokeBand(CapturedBitmap bitmap, PointCollection points)
		{
			var density = bitmap.Density;
			var scaledPoints = points.Select(point => new Point(point.X * density, point.Y * density)).ToArray();
			var inset = BorderStrokeThickness * density;
			int count = 0;

			for (int y = 0; y < bitmap.PixelHeight; y++)
			{
				for (int x = 0; x < bitmap.PixelWidth; x++)
				{
					var sample = new Point(x + 0.5, y + 0.5);
					if (IsInsideTriangle(sample, scaledPoints) &&
						DistanceToPolygon(sample, scaledPoints) < inset &&
						IsLabelBackground(bitmap, x, y))
					{
						count++;
					}
				}
			}

			return count;
		}

		static bool IsLabelBackground(CapturedBitmap bitmap, int x, int y)
		{
			var bufferRow = bitmap.PixelHeight - y - 1;
			var offset = ((bufferRow * bitmap.PixelWidth) + x) * 4;
			var red = bitmap.PixelBuffer[offset];
			var green = bitmap.PixelBuffer[offset + 1];
			var blue = bitmap.PixelBuffer[offset + 2];
			var alpha = bitmap.PixelBuffer[offset + 3];
			return alpha > 200 && red > 180 && red > green + 60 && red > blue + 60;
		}

		static bool IsInsideTriangle(Point point, Point[] triangle)
		{
			var first = Cross(triangle[0], triangle[1], point);
			var second = Cross(triangle[1], triangle[2], point);
			var third = Cross(triangle[2], triangle[0], point);
			return (first >= 0 && second >= 0 && third >= 0) ||
				(first <= 0 && second <= 0 && third <= 0);
		}

		static double DistanceToPolygon(Point point, Point[] polygon)
		{
			var minimum = double.MaxValue;
			for (int i = 0; i < polygon.Length; i++)
				minimum = Math.Min(minimum, DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length]));
			return minimum;
		}

		static double DistanceToSegment(Point point, Point start, Point end)
		{
			var deltaX = end.X - start.X;
			var deltaY = end.Y - start.Y;
			var lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
			var projection = Math.Clamp(
				(((point.X - start.X) * deltaX) + ((point.Y - start.Y) * deltaY)) / lengthSquared,
				0,
				1);
			var nearestX = start.X + (projection * deltaX);
			var nearestY = start.Y + (projection * deltaY);
			var xDistance = point.X - nearestX;
			var yDistance = point.Y - nearestY;
			return Math.Sqrt((xDistance * xDistance) + (yDistance * yDistance));
		}

		static double PolygonPerimeter(PointCollection points)
		{
			double perimeter = 0;
			for (int i = 0; i < points.Count; i++)
			{
				var start = points[i];
				var end = points[(i + 1) % points.Count];
				var xDistance = end.X - start.X;
				var yDistance = end.Y - start.Y;
				perimeter += Math.Sqrt((xDistance * xDistance) + (yDistance * yDistance));
			}
			return perimeter;
		}

		static double Cross(Point start, Point end, Point point) =>
			((end.X - start.X) * (point.Y - start.Y)) -
			((end.Y - start.Y) * (point.X - start.X));

		sealed class CapturedBitmap
		{
			public byte[] PixelBuffer { get; init; } = Array.Empty<byte>();
			public int PixelWidth { get; init; }
			public int PixelHeight { get; init; }
			public double Density { get; init; }
			public double Width { get; init; }
			public double Height { get; init; }
		}
	}
}
#endif


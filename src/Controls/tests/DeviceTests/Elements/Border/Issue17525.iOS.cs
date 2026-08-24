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
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using MauiWindow = Microsoft.Maui.Controls.Window;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17525 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task PolygonClipsContentInsideStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler(typeof(MauiWindow), typeof(WindowHandlerStub));
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			static (ContentPage Page, Border Border) CreateScene(Shape strokeShape)
			{
				var label = new Label
				{
					BackgroundColor = Color.FromArgb("#99FF0000"),
					FontSize = 40,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Text = "+",
					TextColor = Color.FromArgb("#0088ee")
				};

				var border = new Border
				{
					WidthRequest = 101,
					HeightRequest = 101,
					BackgroundColor = Colors.LightBlue,
					StrokeThickness = 8,
					Stroke = Colors.LightGreen,
					StrokeShape = strokeShape,
					Content = label
				};

				var grid = new Grid
				{
					ColumnDefinitions = new ColumnDefinitionCollection
					{
						new ColumnDefinition(GridLength.Star)
					},
					HorizontalOptions = LayoutOptions.Center
				};
				grid.Add(border);

				return (new ContentPage
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						VerticalOptions = LayoutOptions.Center,
						Children = { grid }
					}
				}, border);
			}

			var rectangleScene = CreateScene(new RoundRectangle
			{
				CornerRadius = 0,
				StrokeThickness = 3
			});

			await CreateHandlerAndAddToWindow(rectangleScene.Page, async () =>
			{
				var rectangleView = rectangleScene.Border.Handler?.PlatformView as UIKit.UIView;
				Assert.NotNull(rectangleView);
				await AssertEventually(() => rectangleView.Window is not null && rectangleView.Bounds.Width > 0);

				var rectanglePixels = await CapturePixels(rectangleView);
				Assert.True(CountRedPixels(rectanglePixels) > 0, "The rectangular control did not render its red Label content.");
				Assert.True(CountGreenPixels(rectanglePixels) > 0, "The rectangular control did not render its green stroke.");
				Assert.Equal(0, CountRedPixelsOutsideRectangle(rectanglePixels, rectangleScene.Border.StrokeThickness / 2));
			});

			var polygon = new Polygon
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				},
				StrokeThickness = 3
			};
			var polygonScene = CreateScene(polygon);
			var observedSize = new Size(-1, -1);
			polygonScene.Border.SizeChanged += (_, _) =>
				observedSize = new Size(polygonScene.Border.Width, polygonScene.Border.Height);

			await CreateHandlerAndAddToWindow(polygonScene.Page, async () =>
			{
				var polygonView = polygonScene.Border.Handler?.PlatformView as UIKit.UIView;
				Assert.NotNull(polygonView);
				await AssertEventually(() => observedSize.Width > 0 && observedSize.Height > 0);
				Assert.Equal(101, observedSize.Width, 1d);
				Assert.Equal(101, observedSize.Height, 1d);
				Assert.Equal(101, (double)polygonView.Frame.Width, 1d);
				Assert.Equal(101, (double)polygonView.Frame.Height, 1d);

				var polygonPixels = await CapturePixels(polygonView);
				Assert.True(CountRedPixels(polygonPixels) > 0, "The polygon control did not render its red Label content.");
				Assert.True(CountGreenPixels(polygonPixels) > 0, "The polygon control did not render its green stroke.");

				var vertices = polygon.Points.Select(point => new Point(point.X, point.Y)).ToArray();
				var insetVertices = InsetPolygon(vertices, polygonScene.Border.StrokeThickness / 2);
				var escapedRedPixels = CountRedPixelsOutsidePolygon(polygonPixels, insetVertices, antialiasingTolerance: 1.5);

				Assert.True(
					escapedRedPixels == 0,
					$"Polygon border content escaped the inner clip: observed red pixels outside expected inset polygon={escapedRedPixels}");
			});
		}

		async Task<PixelBuffer> CapturePixels(UIKit.UIView view)
		{
			using var image = await view.ToBitmap(MauiContext);
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);
			Assert.Equal(CGImageByteOrderInfo.ByteOrder32Little, cgImage.ByteOrderInfo);

			using var data = cgImage.DataProvider.CopyData();
			return new PixelBuffer(
				data.ToArray(),
				(int)cgImage.Width,
				(int)cgImage.Height,
				(int)cgImage.BytesPerRow,
				(double)cgImage.Width / (double)view.Bounds.Width);
		}

		static int CountRedPixels(PixelBuffer pixels) =>
			CountPixels(pixels, (_, _, red, green, blue) => IsRed(red, green, blue));

		static int CountGreenPixels(PixelBuffer pixels) =>
			CountPixels(pixels, (_, _, red, green, blue) =>
				green > 150 && green > red * 1.15 && green > blue * 1.15);

		static int CountRedPixelsOutsideRectangle(PixelBuffer pixels, double inset) =>
			CountPixels(pixels, (x, y, red, green, blue) =>
			{
				var logicalX = (x + 0.5) / pixels.Density;
				var logicalY = (y + 0.5) / pixels.Density;
				return IsRed(red, green, blue) &&
					(logicalX < inset || logicalY < inset ||
					 logicalX > pixels.Width / pixels.Density - inset ||
					 logicalY > pixels.Height / pixels.Density - inset);
			});

		static int CountRedPixelsOutsidePolygon(PixelBuffer pixels, Point[] polygon, double antialiasingTolerance) =>
			CountPixels(pixels, (x, y, red, green, blue) =>
			{
				if (!IsRed(red, green, blue))
					return false;

				var point = new Point((x + 0.5) / pixels.Density, (y + 0.5) / pixels.Density);
				for (int i = 0; i < polygon.Length; i++)
				{
					var start = polygon[i];
					var end = polygon[(i + 1) % polygon.Length];
					if (SignedDistance(point, start, end) < -antialiasingTolerance)
						return true;
				}

				return false;
			});

		static int CountPixels(PixelBuffer pixels, Func<int, int, byte, byte, byte, bool> predicate)
		{
			var count = 0;
			for (int y = 0; y < pixels.Height; y++)
			{
				for (int x = 0; x < pixels.Width; x++)
				{
					var index = y * pixels.BytesPerRow + x * 4;
					var blue = pixels.Bytes[index];
					var green = pixels.Bytes[index + 1];
					var red = pixels.Bytes[index + 2];
					if (predicate(x, y, red, green, blue))
						count++;
				}
			}

			return count;
		}

		static bool IsRed(byte red, byte green, byte blue) =>
			red > 180 && red > green * 1.5 && red > blue * 1.5;

		static Point[] InsetPolygon(Point[] polygon, double inset)
		{
			var result = new Point[polygon.Length];
			for (int i = 0; i < polygon.Length; i++)
			{
				var previous = polygon[(i + polygon.Length - 1) % polygon.Length];
				var current = polygon[i];
				var next = polygon[(i + 1) % polygon.Length];
				var previousLine = OffsetLine(previous, current, inset);
				var nextLine = OffsetLine(current, next, inset);
				result[i] = Intersect(previousLine.Start, previousLine.End, nextLine.Start, nextLine.End);
			}

			return result;
		}

		static (Point Start, Point End) OffsetLine(Point start, Point end, double inset)
		{
			var dx = end.X - start.X;
			var dy = end.Y - start.Y;
			var length = Math.Sqrt(dx * dx + dy * dy);
			var offsetX = -dy / length * inset;
			var offsetY = dx / length * inset;
			return (
				new Point(start.X + offsetX, start.Y + offsetY),
				new Point(end.X + offsetX, end.Y + offsetY));
		}

		static Point Intersect(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd)
		{
			var firstX = firstEnd.X - firstStart.X;
			var firstY = firstEnd.Y - firstStart.Y;
			var secondX = secondEnd.X - secondStart.X;
			var secondY = secondEnd.Y - secondStart.Y;
			var denominator = firstX * secondY - firstY * secondX;
			var deltaX = secondStart.X - firstStart.X;
			var deltaY = secondStart.Y - firstStart.Y;
			var scale = (deltaX * secondY - deltaY * secondX) / denominator;
			return new Point(firstStart.X + scale * firstX, firstStart.Y + scale * firstY);
		}

		static double SignedDistance(Point point, Point start, Point end)
		{
			var dx = end.X - start.X;
			var dy = end.Y - start.Y;
			return (dx * (point.Y - start.Y) - dy * (point.X - start.X)) /
				Math.Sqrt(dx * dx + dy * dy);
		}

		readonly record struct PixelBuffer(
			byte[] Bytes,
			int Width,
			int Height,
			int BytesPerRow,
			double Density);
	}
#endif
}


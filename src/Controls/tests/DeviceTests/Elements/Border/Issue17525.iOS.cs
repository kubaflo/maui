#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Border)]
	[Category("Issue17525")]
	public class Issue17525 : ControlsHandlerTestBase
	{
		const double BorderSize = 101;
		const double BorderThickness = 8;

		[Fact]
		public async Task PolygonContentIsClippedToInsetPath()
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

			var grid = new Grid
			{
				WidthRequest = 212,
				HeightRequest = 323,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				ColumnSpacing = 10,
				RowSpacing = 10,
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
				}
			};
			var stack = new VerticalStackLayout
			{
				VerticalOptions = LayoutOptions.Center,
				Children = { grid }
			};
			var page = new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = stack
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var polygon = GetPolygonPoints();
				int loadedGeneration = -1;
				var targetLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var targetLabel = CreateLabel();
				var targetBorder = CreateBorder(targetLabel, BorderThickness);
				targetBorder.Loaded += (_, _) =>
				{
					loadedGeneration = 1;
					targetLoaded.TrySetResult();
				};
				grid.Add(targetBorder, 0, 2);

				await targetLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.Equal(1, loadedGeneration);
				await AssertEventually(() => HasExpectedNativeFrame(targetBorder));
				Assert.Same(grid, targetBorder.Parent);
				Assert.Equal(0, Grid.GetColumn(targetBorder));
				Assert.Equal(2, Grid.GetRow(targetBorder));
				Assert.Same(targetLabel, targetBorder.Content);
				Assert.Equal("+", targetLabel.Text);
				Assert.Equal(40, targetLabel.FontSize);
				Assert.Equal(Color.FromArgb("#99FF0000"), targetLabel.BackgroundColor);
				Assert.Equal(Color.FromArgb("#0088EE"), targetLabel.TextColor);

				using var targetImage = await Capture(targetBorder);
				var targetLabelFrame = GetLabelFrame(targetBorder, targetLabel);
				var innerPolygon = InsetPolygon(polygon, BorderThickness);
				var result = AnalyzeInset(targetImage, targetLabelFrame, polygon, innerPolygon);

				Assert.True(
					result.ContentExpected >= 10 && result.MaskExpected >= 10 && result.Mismatched == 0,
					$"Polygon Border inner-path pixels differed from the expected inset polygon: matched={result.Matched}, expected={result.Expected}, mismatched={result.Mismatched}, content={result.ContentExpected}, mask={result.MaskExpected}.");
			});
		}

		static Label CreateLabel() =>
			new()
			{
				Text = "+",
				FontSize = 40,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				BackgroundColor = Color.FromArgb("#99FF0000"),
				TextColor = Color.FromArgb("#0088EE")
			};

		static Border CreateBorder(Label label, double strokeThickness)
		{
			var border = new Border
			{
				WidthRequest = BorderSize,
				HeightRequest = BorderSize,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				BackgroundColor = Colors.LightBlue,
				Stroke = Colors.LightGreen,
				StrokeThickness = strokeThickness,
				StrokeShape = new Polygon
				{
					Points = new PointCollection(GetPolygonPoints()),
					StrokeThickness = 3
				},
				Content = label
			};
			Grid.SetColumn(border, 0);
			Grid.SetRow(border, 2);
			return border;
		}

		static Point[] GetPolygonPoints() =>
			new[]
			{
				new Point(40, 10),
				new Point(70, 80),
				new Point(10, 50)
			};

		static bool HasExpectedNativeFrame(Border border) =>
			border.Handler?.PlatformView is UIView view &&
			Math.Abs(view.Frame.Width - BorderSize) < 0.5 &&
			Math.Abs(view.Frame.Height - BorderSize) < 0.5;

		async Task<UIImage> Capture(Border border)
		{
			var platformView = border.Handler?.PlatformView as UIView;
			Assert.NotNull(platformView);
			return await platformView.ToBitmap(MauiContext);
		}

		static CGRect GetLabelFrame(Border border, Label label)
		{
			var borderView = border.Handler?.PlatformView as UIView;
			var labelView = label.Handler?.PlatformView as UIView;
			Assert.NotNull(borderView);
			Assert.NotNull(labelView);
			Assert.Equal(BorderSize, borderView.Bounds.Width, 1);
			Assert.Equal(BorderSize, borderView.Bounds.Height, 1);
			var frame = labelView.ConvertRectToView(labelView.Bounds, borderView);
			Assert.True(frame.Width > 0 && frame.Height > 0);
			return frame;
		}

		static PixelResult AnalyzeInset(UIImage image, CGRect labelFrame, Point[] outerPolygon, Point[] innerPolygon)
		{
			using var pixels = PixelBuffer.Create(image);
			int matched = 0;
			int expected = 0;
			int mismatched = 0;
			int contentExpected = 0;
			int maskExpected = 0;

			ForEachSample(labelFrame, pixels, point =>
			{
				if (pixels.IsGlyph(point))
					return;

				if (!IsInside(point, outerPolygon) ||
					DistanceToEdges(point, outerPolygon) < BorderThickness / 2 + 1 ||
					DistanceToEdges(point, innerPolygon) < 1.5)
				{
					return;
				}

				bool insideInner = IsInside(point, innerPolygon);
				if (insideInner)
					contentExpected++;
				else
					maskExpected++;
				bool matches = insideInner
					? pixels.IsColor(point, 222, 86, 92)
					: pixels.IsColor(point, 173, 216, 230);
				expected++;
				if (matches)
					matched++;
				else
					mismatched++;
			});

			return new PixelResult(matched, expected, mismatched, contentExpected, maskExpected);
		}

		static void ForEachSample(CGRect labelFrame, PixelBuffer pixels, Action<Point> action)
		{
			int minX = Math.Max(0, (int)Math.Ceiling(labelFrame.Left + 1));
			int maxX = Math.Min((int)Math.Floor(labelFrame.Right - 1), pixels.Width - 1);
			int minY = Math.Max(0, (int)Math.Ceiling(labelFrame.Top + 1));
			int maxY = Math.Min((int)Math.Floor(labelFrame.Bottom - 1), pixels.Height - 1);

			for (int y = minY; y <= maxY; y++)
			{
				for (int x = minX; x <= maxX; x++)
					action(new Point(x + 0.5, y + 0.5));
			}
		}

		static Point[] InsetPolygon(Point[] polygon, double inset)
		{
			var result = new Point[polygon.Length];
			for (int i = 0; i < polygon.Length; i++)
			{
				int previous = (i + polygon.Length - 1) % polygon.Length;
				var previousLine = OffsetLine(polygon[previous], polygon[i], inset);
				var nextLine = OffsetLine(polygon[i], polygon[(i + 1) % polygon.Length], inset);
				result[i] = Intersect(previousLine.Start, previousLine.End, nextLine.Start, nextLine.End);
			}
			return result;
		}

		static (Point Start, Point End) OffsetLine(Point start, Point end, double inset)
		{
			double dx = end.X - start.X;
			double dy = end.Y - start.Y;
			double length = Math.Sqrt(dx * dx + dy * dy);
			double offsetX = -dy / length * inset;
			double offsetY = dx / length * inset;
			return (
				new Point(start.X + offsetX, start.Y + offsetY),
				new Point(end.X + offsetX, end.Y + offsetY));
		}

		static Point Intersect(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd)
		{
			double firstX = firstEnd.X - firstStart.X;
			double firstY = firstEnd.Y - firstStart.Y;
			double secondX = secondEnd.X - secondStart.X;
			double secondY = secondEnd.Y - secondStart.Y;
			double determinant = firstX * secondY - firstY * secondX;
			double deltaX = secondStart.X - firstStart.X;
			double deltaY = secondStart.Y - firstStart.Y;
			double factor = (deltaX * secondY - deltaY * secondX) / determinant;
			return new Point(firstStart.X + factor * firstX, firstStart.Y + factor * firstY);
		}

		static bool IsInside(Point point, Point[] polygon)
		{
			bool inside = false;
			for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
			{
				if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
					point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
					(polygon[j].Y - polygon[i].Y) + polygon[i].X)
				{
					inside = !inside;
				}
			}
			return inside;
		}

		static double DistanceToEdges(Point point, Point[] polygon)
		{
			double minimum = double.MaxValue;
			for (int i = 0; i < polygon.Length; i++)
			{
				var start = polygon[i];
				var end = polygon[(i + 1) % polygon.Length];
				double dx = end.X - start.X;
				double dy = end.Y - start.Y;
				double distance = Math.Abs(dx * (start.Y - point.Y) - (start.X - point.X) * dy) /
					Math.Sqrt(dx * dx + dy * dy);
				minimum = Math.Min(minimum, distance);
			}
			return minimum;
		}

		readonly record struct PixelResult(
			int Matched,
			int Expected,
			int Mismatched,
			int ContentExpected,
			int MaskExpected);

		sealed class PixelBuffer : IDisposable
		{
			byte[] Data { get; set; }
			CGBitmapContext Context { get; set; }

			public static PixelBuffer Create(UIImage image)
			{
				var cgImage = image.CGImage;
				Assert.NotNull(cgImage);
				int pixelWidth = (int)cgImage.Width;
				int pixelHeight = (int)cgImage.Height;
				int width = (int)Math.Round(image.Size.Width);
				int height = (int)Math.Round(image.Size.Height);
				Assert.True(width > 0 && height > 0);
				var data = new byte[pixelWidth * pixelHeight * 4];
				using var colorSpace = CGColorSpace.CreateDeviceRGB();
				var context = new CGBitmapContext(
					data,
					pixelWidth,
					pixelHeight,
					8,
					pixelWidth * 4,
					colorSpace,
					CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
				context.DrawImage(new CGRect(0, 0, pixelWidth, pixelHeight), cgImage);
				return new PixelBuffer
				{
					Data = data,
					Context = context,
					Width = width,
					Height = height,
					PixelWidth = pixelWidth,
					PixelHeight = pixelHeight
				};
			}

			public int Width { get; private set; }
			public int Height { get; private set; }
			int PixelWidth { get; set; }
			int PixelHeight { get; set; }

			public bool IsGlyph(Point point)
			{
				var (red, green, blue, alpha) = GetColor(point);
				return alpha > 100 && blue > red + 20 && blue > green + 20;
			}

			public bool IsColor(Point point, byte red, byte green, byte blue)
			{
				var (actualRed, actualGreen, actualBlue, alpha) = GetColor(point);
				const int tolerance = 28;
				return Math.Abs(actualRed - red) <= tolerance &&
					Math.Abs(actualGreen - green) <= tolerance &&
					Math.Abs(actualBlue - blue) <= tolerance &&
					alpha >= 240;
			}

			(byte Red, byte Green, byte Blue, byte Alpha) GetColor(Point point)
			{
				int x = Math.Clamp((int)(point.X * PixelWidth / Width), 0, PixelWidth - 1);
				int y = PixelHeight - 1 - Math.Clamp((int)(point.Y * PixelHeight / Height), 0, PixelHeight - 1);
				int index = (y * PixelWidth + x) * 4;
				return (Data[index], Data[index + 1], Data[index + 2], Data[index + 3]);
			}

			public void Dispose() => Context.Dispose();
		}
	}
}
#endif


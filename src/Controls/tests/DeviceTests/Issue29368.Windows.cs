#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29368")]
	public class Issue29368 : ControlsHandlerTestBase
	{
		const int SurfaceSize = 300;
		const int TileSize = 20;
		const int ColorTolerance = 24;

		[Fact]
		public async Task PatternPaintStartsAtRequestedZeroOrigin()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
				});
			});

			var referenceDrawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var targetDrawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var referenceDrawable = new DirectPatternDrawable
			{
				OnDrawn = () => referenceDrawn.TrySetResult()
			};
			var targetDrawable = new PatternPaintDrawable
			{
				OnDrawn = () => targetDrawn.TrySetResult()
			};

			var reference = await CaptureFrame(referenceDrawable, referenceDrawn.Task);
			var target = await CaptureFrame(targetDrawable, targetDrawn.Task);

			Assert.Equal(reference.Width, target.Width);
			Assert.Equal(reference.Height, target.Height);
			Assert.True(reference.Width > 0 && reference.Height > 0);

			var rasterScale = reference.Width / (double)SurfaceSize;
			Assert.InRange(rasterScale, 0.5, 4);
			Assert.InRange(Math.Abs(reference.Height / (double)SurfaceSize - rasterScale), 0, 0.02);

			var silver = ToPixelColor(Colors.Silver);
			var white = ToPixelColor(Colors.White);
			Assert.True(ColorDistance(silver, white) > ColorTolerance * 2);

			var referenceMismatch = CountMaskMismatches(reference, rasterScale, silver, white);
			const int allowedMismatch = 2;
			Assert.True(
				referenceMismatch <= allowedMismatch,
				$"Direct pattern calibration mismatch: mismatches={referenceMismatch}, allowed={allowedMismatch}, frame={reference.Width}x{reference.Height}");

			Assert.True(ContainsColor(target, silver), "PatternPaint did not render any silver pattern pixels.");

			var targetMismatch = CountMaskMismatches(target, rasterScale, silver, white);
			Assert.True(
				targetMismatch <= allowedMismatch,
				$"PatternPaint origin mismatch at requested (0,0): target mismatches={targetMismatch}, allowed={allowedMismatch}, frame={target.Width}x{target.Height}");
		}

		async Task<CapturedFrame> CaptureFrame(ObservedDrawable drawable, Task drawn)
		{
			var graphicsView = new GraphicsView
			{
				Drawable = drawable,
				WidthRequest = SurfaceSize,
				HeightRequest = SurfaceSize,
				BackgroundColor = Colors.White
			};

			var page = new ContentPage
			{
				Title = "Pattern origin",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 12,
					HorizontalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							Text = "PatternPaint bounds and rectangle both start at X=0, Y=0.",
							HorizontalTextAlignment = TextAlignment.Center
						},
						new Border
						{
							Stroke = Colors.Black,
							StrokeThickness = 1,
							Padding = 0,
							Content = graphicsView
						},
						new Label
						{
							Text = "The silver cross-hatch should begin at the canvas top-left corner.",
							HorizontalTextAlignment = TextAlignment.Center
						},
						new Label
						{
							Text = "Rendered pattern",
							FontAttributes = FontAttributes.Bold,
							HorizontalTextAlignment = TextAlignment.Center
						},
						new Button { Text = "Check pattern origin" }
					}
				}
			};

			CapturedFrame captured = null;
			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await drawn;
				var bitmap = await graphicsView.AsRawBitmapAsync();
				captured = new CapturedFrame
				{
					Width = bitmap.PixelWidth,
					Height = bitmap.PixelHeight,
					Pixels = bitmap.PixelBuffer
				};
			});

			Assert.NotNull(captured);
			return captured;
		}

		static int CountMaskMismatches(CapturedFrame frame, double scale, PixelColor silver, PixelColor white)
		{
			var mismatches = 0;
			var searchRadius = Math.Max(1, (int)Math.Ceiling(scale));

			foreach (var tile in new[] { 1, 3, 5, 7, 9, 11, 13 })
			{
				mismatches += MatchesNear(frame, LogicalPixel(tile * TileSize + 5, scale), LogicalPixel(5, scale), searchRadius, silver) ? 0 : 1;
				mismatches += MatchesNear(frame, LogicalPixel(tile * TileSize + 15, scale), LogicalPixel(5, scale), searchRadius, silver) ? 0 : 1;
				mismatches += MatchesAt(frame, LogicalPixel(tile * TileSize + 5, scale), LogicalPixel(10, scale), white) ? 0 : 1;
			}

			return mismatches;
		}

		static int LogicalPixel(int value, double scale) =>
			(int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

		static bool ContainsColor(CapturedFrame frame, PixelColor expected)
		{
			for (var y = 0; y < frame.Height; y++)
			{
				for (var x = 0; x < frame.Width; x++)
				{
					if (MatchesAt(frame, x, y, expected))
						return true;
				}
			}

			return false;
		}

		static bool MatchesNear(CapturedFrame frame, int centerX, int centerY, int radius, PixelColor expected)
		{
			Assert.InRange(centerX - radius, 0, frame.Width - 1);
			Assert.InRange(centerX + radius, 0, frame.Width - 1);
			Assert.InRange(centerY - radius, 0, frame.Height - 1);
			Assert.InRange(centerY + radius, 0, frame.Height - 1);

			for (var y = centerY - radius; y <= centerY + radius; y++)
			{
				for (var x = centerX - radius; x <= centerX + radius; x++)
				{
					if (MatchesAt(frame, x, y, expected))
						return true;
				}
			}

			return false;
		}

		static bool MatchesAt(CapturedFrame frame, int x, int y, PixelColor expected)
		{
			Assert.InRange(x, 0, frame.Width - 1);
			Assert.InRange(y, 0, frame.Height - 1);

			var offset = (y * frame.Width + x) * 4;
			var actual = new PixelColor
			{
				Red = frame.Pixels[offset + 2],
				Green = frame.Pixels[offset + 1],
				Blue = frame.Pixels[offset]
			};
			return ColorDistance(actual, expected) <= ColorTolerance;
		}

		static PixelColor ToPixelColor(Color color) =>
			new()
			{
				Red = (byte)Math.Round(color.Red * 255),
				Green = (byte)Math.Round(color.Green * 255),
				Blue = (byte)Math.Round(color.Blue * 255)
			};

		static int ColorDistance(PixelColor first, PixelColor second) =>
			Math.Max(
				Math.Abs(first.Red - second.Red),
				Math.Max(Math.Abs(first.Green - second.Green), Math.Abs(first.Blue - second.Blue)));

		struct PixelColor
		{
			public byte Red { get; set; }
			public byte Green { get; set; }
			public byte Blue { get; set; }
		}

		sealed class CapturedFrame
		{
			public int Width { get; set; }
			public int Height { get; set; }
			public byte[] Pixels { get; set; }
		}

		abstract class ObservedDrawable : IDrawable
		{
			public Action OnDrawn { get; set; }

			public void Draw(ICanvas canvas, RectF dirtyRect)
			{
				DrawPattern(canvas);
				OnDrawn();
			}

			protected abstract void DrawPattern(ICanvas canvas);
		}

		sealed class PatternPaintDrawable : ObservedDrawable
		{
			protected override void DrawPattern(ICanvas canvas)
			{
				IPattern pattern;
				using (var picture = new PictureCanvas(0, 0, TileSize, TileSize))
				{
					picture.StrokeColor = Colors.Silver;
					picture.StrokeSize = 2;
					picture.DrawLine(0, 0, TileSize, TileSize);
					picture.DrawLine(0, TileSize, TileSize, 0);
					pattern = new PicturePattern(picture.Picture, TileSize, TileSize);
				}

				var bounds = new RectF(0, 0, SurfaceSize, SurfaceSize);
				canvas.SetFillPaint(new PatternPaint { Pattern = pattern }, bounds);
				canvas.FillRectangle(bounds);
			}
		}

		sealed class DirectPatternDrawable : ObservedDrawable
		{
			protected override void DrawPattern(ICanvas canvas)
			{
				canvas.FillColor = Colors.White;
				canvas.FillRectangle(0, 0, SurfaceSize, SurfaceSize);
				canvas.StrokeColor = Colors.Silver;
				canvas.StrokeSize = 2;

				for (var y = 0; y < SurfaceSize; y += TileSize)
				{
					for (var x = 0; x < SurfaceSize; x += TileSize)
					{
						canvas.DrawLine(x, y, x + TileSize, y + TileSize);
						canvas.DrawLine(x, y + TileSize, x + TileSize, y);
					}
				}
			}
		}
	}
}
#endif


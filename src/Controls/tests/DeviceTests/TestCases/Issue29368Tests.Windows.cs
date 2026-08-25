#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29368")]
	public class Issue29368 : ControlsHandlerTestBase
	{
		const int CanvasSize = 300;
		const int PictureSize = 20;
		const int PatternStep = 30;

		[Fact]
		public async Task PatternPaintStartsAtFillBoundsOrigin()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
				});
			});

			var clickedGeneration = -1;
			var patternDrawGeneration = -1;
			var drawable = new PatternPositionDrawable();
			drawable.Initialize(generation => patternDrawGeneration = generation);
			var headingLabel = new Label
			{
				Text = "PatternPaint at canvas X=0, Y=0",
				FontSize = 20,
				HorizontalTextAlignment = TextAlignment.Center
			};
			var graphicsView = new GraphicsView
			{
				AutomationId = "PatternView",
				WidthRequest = CanvasSize,
				HeightRequest = CanvasSize,
				BackgroundColor = Colors.White,
				Drawable = drawable
			};
			var drawButton = new Button
			{
				AutomationId = "DrawPatternButton",
				Text = "Draw pattern at 0,0"
			};
			var resultLabel = new Label
			{
				AutomationId = "ResultLabel",
				Text = "Pattern origin reference",
				FontSize = 18,
				HorizontalTextAlignment = TextAlignment.Center
			};
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 12,
				HorizontalOptions = LayoutOptions.Center,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(headingLabel, 0, 0);
			grid.Add(graphicsView, 0, 1);
			grid.Add(drawButton, 0, 2);
			grid.Add(resultLabel, 0, 3);

			var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			drawButton.Clicked += (_, _) =>
			{
				clickedGeneration = drawable.Generation;
				drawable.ShowPattern();
				graphicsView.Invalidate();
				clicked.TrySetResult();
			};
			var page = new ContentPage { Content = grid };
			var window = new Window(page);

			await CreateHandlerAndAddToWindow<IWindowHandler>(window, async _ =>
			{
				await drawable.WaitForCurrentDrawAsync();

				var directBitmap = await graphicsView.AsRawBitmapAsync();
				Assert.Equal(CanvasSize, directBitmap.Width, 1);
				Assert.Equal(CanvasSize, directBitmap.Height, 1);

				var directPhase = FindPatternPhase(directBitmap);
				const double tolerance = 1;
				Assert.True(directPhase.SilverSamples >= 100, "The direct-draw control did not contain enough silver samples.");
				Assert.True(directPhase.WhiteSamples >= 100, "The direct-draw control did not contain enough white samples.");
				Assert.True(
					DistanceFromOrigin(directPhase.X) <= tolerance &&
					DistanceFromOrigin(directPhase.Y) <= tolerance,
					$"Direct-draw phase detector control failed: observed ({directPhase.X},{directPhase.Y}).");
				var directEdgeSamples = CountSilverEdgeSamples(directBitmap);
				Assert.True(directEdgeSamples >= 40, $"The direct-draw control contained only {directEdgeSamples} silver edge samples.");

				drawable.ShowInitial();
				graphicsView.Invalidate();
				await drawable.WaitForCurrentDrawAsync();
				var initialGeneration = drawable.Generation;
				var initialBitmap = await graphicsView.AsRawBitmapAsync();
				Assert.True(IsRed(GetPixel(initialBitmap, 20, 1)), "The initial origin axes were not rendered.");
				Assert.True(IsWhite(GetPixel(initialBitmap, 100, 100)), "The initial canvas was not white.");

				var platformButton = Assert.IsAssignableFrom<WButton>(drawButton.Handler.PlatformView);
				var automationPeer = new ButtonAutomationPeer(platformButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(
					automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();

				await clicked.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await drawable.WaitForCurrentDrawAsync();

				Assert.True(clickedGeneration >= initialGeneration);
				Assert.True(patternDrawGeneration > clickedGeneration);

				var patternBitmap = await graphicsView.AsRawBitmapAsync();
				Assert.Equal(CanvasSize, patternBitmap.Width, 1);
				Assert.Equal(CanvasSize, patternBitmap.Height, 1);

				var patternPhase = FindPatternPhase(patternBitmap);
				Assert.True(patternPhase.SilverSamples >= 100, "The rendered pattern did not contain enough silver samples.");
				Assert.True(patternPhase.WhiteSamples >= 100, "The rendered pattern did not contain enough white samples.");

				var patternEdgeSamples = CountSilverEdgeSamples(patternBitmap);
				var mismatchedEdgeSamples = CountMismatchedEdgeSamples(directBitmap, patternBitmap);
				Assert.True(
					mismatchedEdgeSamples == 0,
					$"Issue29368 PatternPaint origin mismatch: PatternPaint changed {mismatchedEdgeSamples} edge pixels at the direct rendering's fill-origin grid; the direct rendering had {directEdgeSamples} silver samples and PatternPaint had {patternEdgeSamples}.");

				Assert.True(
					DistanceFromOrigin(patternPhase.X) <= tolerance &&
					DistanceFromOrigin(patternPhase.Y) <= tolerance,
					$"Issue29368 PatternPaint origin mismatch: observed phase ({patternPhase.X},{patternPhase.Y}), expected (0,0) within {tolerance}; matched {patternPhase.Matched}/{patternPhase.Sampled} pattern samples.");
			});
		}

		static PhaseResult FindPatternPhase(RawBitmap bitmap)
		{
			(double X, double Y)[] silverOffsets =
			[
				(5, 5), (10, 10), (15, 15), (5, 15), (15, 5)
			];
			(double X, double Y)[] whiteOffsets =
			[
				(5, 10), (10, 5), (10, 15), (15, 10)
			];

			var best = new PhaseResult(0, 0, -1, 0, 0, 0);
			for (var phaseXStep = 0; phaseXStep < PatternStep * 2; phaseXStep++)
			{
				for (var phaseYStep = 0; phaseYStep < PatternStep * 2; phaseYStep++)
				{
					var phaseX = phaseXStep / 2d;
					var phaseY = phaseYStep / 2d;
					var matched = 0;
					var sampled = 0;
					var silverSamples = 0;
					var whiteSamples = 0;

					for (var tileX = 2; tileX <= 8; tileX++)
					{
						for (var tileY = 2; tileY <= 8; tileY++)
						{
							foreach (var offset in silverOffsets)
							{
								var color = GetPixel(bitmap,
									(tileX * PatternStep) + phaseX + offset.X,
									(tileY * PatternStep) + phaseY + offset.Y);
								sampled++;
								if (IsSilver(color))
								{
									matched++;
									silverSamples++;
								}
							}

							foreach (var offset in whiteOffsets)
							{
								var color = GetPixel(bitmap,
									(tileX * PatternStep) + phaseX + offset.X,
									(tileY * PatternStep) + phaseY + offset.Y);
								sampled++;
								if (IsWhite(color))
								{
									matched++;
									whiteSamples++;
								}
							}
						}
					}

					if (matched > best.Matched)
						best = new PhaseResult(phaseX, phaseY, matched, sampled, silverSamples, whiteSamples);
				}
			}

			return best;
		}

		static int CountSilverEdgeSamples(RawBitmap bitmap)
		{
			var silverSamples = 0;
			for (var position = 60; position < CanvasSize; position += PatternStep)
			{
				for (var offset = -2; offset <= 2; offset++)
				{
					if (IsSilver(GetPixel(bitmap, position + offset, 1)))
						silverSamples++;
					if (IsSilver(GetPixel(bitmap, 1, position + offset)))
						silverSamples++;
				}
			}

			return silverSamples;
		}

		static int CountMismatchedEdgeSamples(RawBitmap expected, RawBitmap actual)
		{
			var mismatchedSamples = 0;
			for (var position = 60; position < CanvasSize; position += PatternStep)
			{
				for (var offset = -2; offset <= 2; offset++)
				{
					if (!ColorsMatch(
						GetPixel(expected, position + offset, 1),
						GetPixel(actual, position + offset, 1)))
						mismatchedSamples++;
					if (!ColorsMatch(
						GetPixel(expected, 1, position + offset),
						GetPixel(actual, 1, position + offset)))
						mismatchedSamples++;
				}
			}

			return mismatchedSamples;
		}

		static bool ColorsMatch((byte R, byte G, byte B) expected, (byte R, byte G, byte B) actual) =>
			Math.Abs(expected.R - actual.R) <= 4 &&
			Math.Abs(expected.G - actual.G) <= 4 &&
			Math.Abs(expected.B - actual.B) <= 4;

		static (byte R, byte G, byte B) GetPixel(RawBitmap bitmap, double logicalX, double logicalY)
		{
			var x = (int)Math.Round(logicalX * bitmap.Density);
			var y = (int)Math.Round(logicalY * bitmap.Density);
			if ((uint)x >= (uint)bitmap.PixelWidth || (uint)y >= (uint)bitmap.PixelHeight)
				throw new InvalidOperationException($"Pattern sample ({x},{y}) is outside the {bitmap.PixelWidth}x{bitmap.PixelHeight} native bitmap.");
			var index = ((y * bitmap.PixelWidth) + x) * 4;
			return (bitmap.PixelBuffer[index + 2], bitmap.PixelBuffer[index + 1], bitmap.PixelBuffer[index]);
		}

		static bool IsSilver((byte R, byte G, byte B) color) =>
			color.R is >= 145 and <= 230 &&
			Math.Abs(color.R - color.G) <= 4 &&
			Math.Abs(color.R - color.B) <= 4;

		static bool IsWhite((byte R, byte G, byte B) color) =>
			color.R >= 240 && color.G >= 240 && color.B >= 240;

		static bool IsRed((byte R, byte G, byte B) color) =>
			color.R >= 200 && color.G <= 80 && color.B <= 80;

		static double DistanceFromOrigin(double phase) =>
			Math.Min(phase, PatternStep - phase);

		readonly record struct PhaseResult(
			double X,
			double Y,
			int Matched,
			int Sampled,
			int SilverSamples,
			int WhiteSamples);

		sealed class PatternPositionDrawable : IDrawable
		{
			Action<int> _patternDrawn;
			DrawMode _mode;
			TaskCompletionSource _currentDraw;

			public void Initialize(Action<int> patternDrawn)
			{
				_patternDrawn = patternDrawn;
				_mode = DrawMode.Direct;
				_currentDraw = NewCompletionSource();
				Generation = -1;
			}

			public int Generation { get; private set; }

			public Task WaitForCurrentDrawAsync() =>
				_currentDraw.Task.WaitAsync(TimeSpan.FromSeconds(5));

			public void ShowInitial()
			{
				_mode = DrawMode.Initial;
				_currentDraw = NewCompletionSource();
			}

			public void ShowPattern()
			{
				_mode = DrawMode.Pattern;
				_currentDraw = NewCompletionSource();
			}

			public void Draw(ICanvas canvas, RectF dirtyRect)
			{
				canvas.FillColor = Colors.White;
				canvas.FillRectangle(dirtyRect);

				if (_mode == DrawMode.Direct)
				{
					using var picture = CreatePicture();
					for (var x = 0; x < CanvasSize; x += PatternStep)
					{
						for (var y = 0; y < CanvasSize; y += PatternStep)
						{
							canvas.SaveState();
							canvas.Translate(x, y);
							picture.Picture.Draw(canvas);
							canvas.RestoreState();
						}
					}
				}
				else if (_mode == DrawMode.Pattern)
				{
					IPattern pattern;
					using (var picture = CreatePicture())
					{
						pattern = new PicturePattern(picture.Picture, PatternStep, PatternStep);
					}

					var patternPaint = new PatternPaint
					{
						Pattern = pattern,
						ForegroundColor = Colors.Silver
					};
					var fillBounds = new RectF(0, 0, CanvasSize, CanvasSize);
					canvas.SetFillPaint(patternPaint, fillBounds);
					canvas.FillRectangle(fillBounds);
				}

				canvas.StrokeColor = Colors.Red;
				canvas.StrokeSize = 4;
				canvas.DrawLine(0, 0, 40, 0);
				canvas.DrawLine(0, 0, 0, 40);

				Generation++;
				if (_mode == DrawMode.Pattern)
					_patternDrawn(Generation);
				_currentDraw.TrySetResult();
			}

			static PictureCanvas CreatePicture()
			{
				var picture = new PictureCanvas(0, 0, PictureSize, PictureSize)
				{
					StrokeColor = Colors.Silver,
					StrokeSize = 2
				};
				picture.DrawLine(0, 0, PictureSize, PictureSize);
				picture.DrawLine(0, PictureSize, PictureSize, 0);
				return picture;
			}

			static TaskCompletionSource NewCompletionSource() =>
				new(TaskCreationOptions.RunContinuationsAsynchronously);

			enum DrawMode
			{
				Direct,
				Initial,
				Pattern
			}
		}
	}
}
#endif


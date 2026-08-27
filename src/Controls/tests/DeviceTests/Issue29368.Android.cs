#if ANDROID
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using ABitmap = global::Android.Graphics.Bitmap;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue29368")]
public class Issue29368 : ControlsHandlerTestBase
{
	const float SurfaceSize = 300;
	const float TileSize = 20;
	const float StrokeSize = 2;

	[Fact]
	public async Task PatternPaintStartsAtFillRectangleOrigin()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
			});
		});

		var referenceDrawState = new DrawState();
		referenceDrawState.Value = -1;
		var referenceSurface = CreateSurface(new ReferenceDrawable { DrawState = referenceDrawState });
		var referencePage = CreatePage(referenceSurface);
		var referencePhase = await CapturePhase(referencePage, referenceSurface, referenceDrawState);
		var tolerance = Math.Max(2, referencePhase.Density * 1.25);
		var edgeTolerance = Math.Max(1, Math.Ceiling(StrokeSize * referencePhase.Density / 2));

		Assert.True(
			referencePhase.TopInkOffset <= edgeTolerance &&
			referencePhase.LeftInkOffset <= edgeTolerance &&
			referencePhase.Strength > 0,
			$"Reference pattern did not establish the zero-origin oracle: top={referencePhase.TopInkOffset}px, left={referencePhase.LeftInkOffset}px, tolerance={edgeTolerance:F2}px, strength={referencePhase.Strength:F3}.");

		var patternDrawState = new DrawState();
		patternDrawState.Value = -1;
		var patternSurface = CreateSurface(new PatternDrawable { DrawState = patternDrawState });
		var patternPage = CreatePage(patternSurface);
		var patternPhase = await CapturePhase(patternPage, patternSurface, patternDrawState);
		var minimumPhaseStrength = referencePhase.Strength * 0.8;
		var tilePixels = (int)Math.Round(TileSize * patternPhase.Density);
		var xPhaseDifference = CircularDistance(patternPhase.X, referencePhase.X, tilePixels);
		var yPhaseDifference = CircularDistance(patternPhase.Y, referencePhase.Y, tilePixels);

		Assert.True(
			xPhaseDifference <= tolerance &&
			yPhaseDifference <= tolerance &&
			patternPhase.TopInkOffset <= edgeTolerance &&
			patternPhase.LeftInkOffset <= edgeTolerance &&
			patternPhase.Strength >= minimumPhaseStrength,
			$"PatternPaint origin mismatch: X phase delta={xPhaseDifference:F2}px, Y phase delta={yPhaseDifference:F2}px, pattern phase=({patternPhase.X:F2},{patternPhase.Y:F2})px, reference phase=({referencePhase.X:F2},{referencePhase.Y:F2})px, top={patternPhase.TopInkOffset}px, left={patternPhase.LeftInkOffset}px, expected reference-aligned phase within {tolerance:F2}px and zero-origin ink within {edgeTolerance:F2}px, phase strength={patternPhase.Strength:F3}, minimum phase strength={minimumPhaseStrength:F3}, dimensions={patternPhase.Width}x{patternPhase.Height}px, density={patternPhase.Density:F2}, ink count={patternPhase.InkCount}.");
	}

	async Task<PatternPhase> CapturePhase(ContentPage page, GraphicsView surface, DrawState drawState)
	{
		var result = new PatternPhase(double.NaN, double.NaN, -1, -1, float.NaN, -1, double.NaN, -1, -1);

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			await AssertHelpers.AssertEventually(
				() => Volatile.Read(ref drawState.Value) == 1,
				timeout: 5000,
				message: "The GraphicsView did not receive a post-attachment Draw callback.");
			Assert.Equal(1, Volatile.Read(ref drawState.Value));

			var platformView = surface.Handler?.PlatformView as PlatformTouchGraphicsView;
			Assert.NotNull(platformView);
			Assert.True(platformView.MeasuredWidth > 0 && platformView.MeasuredHeight > 0);

			var density = platformView.Context.Resources.DisplayMetrics.Density;
			var expectedPixels = (int)Math.Round(SurfaceSize * density);
			Assert.InRange(platformView.MeasuredWidth, expectedPixels - 1, expectedPixels + 1);
			Assert.InRange(platformView.MeasuredHeight, expectedPixels - 1, expectedPixels + 1);

			var rootView = platformView.RootView;
			Assert.NotNull(rootView);

			var rootLocation = new int[2];
			var surfaceLocation = new int[2];
			rootView.GetLocationOnScreen(rootLocation);
			platformView.GetLocationOnScreen(surfaceLocation);

			var surfaceX = surfaceLocation[0] - rootLocation[0];
			var surfaceY = surfaceLocation[1] - rootLocation[1];

			using var rootBitmap = await rootView.ToBitmap(MauiContext);
			Assert.InRange(surfaceX, 0, rootBitmap.Width - platformView.MeasuredWidth);
			Assert.InRange(surfaceY, 0, rootBitmap.Height - platformView.MeasuredHeight);

			using var bitmap = ABitmap.CreateBitmap(
				rootBitmap,
				surfaceX,
				surfaceY,
				platformView.MeasuredWidth,
				platformView.MeasuredHeight);
			Assert.NotNull(bitmap);
			result = MeasurePhase(bitmap, density);
		});

		Assert.False(double.IsNaN(result.X));
		Assert.True(result.InkCount > 0);
		return result;
	}

	static ContentPage CreatePage(GraphicsView surface) =>
		new()
		{
			Title = "Home",
			Content = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label
					{
						Margin = new Thickness(20, 12, 20, 0),
						Text = "Expected: the first silver diagonal begins at the top-left corner (0,0).",
						HorizontalTextAlignment = TextAlignment.Center
					},
					surface,
					new Label
					{
						Text = "Pattern origin",
						FontAttributes = FontAttributes.Bold,
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			}
		};

	static GraphicsView CreateSurface(IDrawable drawable) =>
		new()
		{
			Drawable = drawable,
			WidthRequest = SurfaceSize,
			HeightRequest = SurfaceSize,
			HorizontalOptions = LayoutOptions.Center,
			BackgroundColor = Colors.White
		};

	static PatternPhase MeasurePhase(ABitmap bitmap, float density)
	{
		var tilePixels = (int)Math.Round(TileSize * density);
		var strokePixels = Math.Max(1, (int)Math.Ceiling(StrokeSize * density));
		Assert.True(tilePixels > 0);
		Assert.True(bitmap.Width > tilePixels * 4 && bitmap.Height > tilePixels * 4);

		var differenceBands = new double[tilePixels];
		var sumBands = new double[tilePixels];
		var inkCount = 0;
		var backgroundCount = 0;
		var topInkOffset = tilePixels + 1;
		var leftInkOffset = tilePixels + 1;
		var margin = tilePixels;

		for (int y = 0; y < tilePixels; y++)
		{
			for (int x = 0; x < tilePixels; x++)
			{
				var color = new global::Android.Graphics.Color(bitmap.GetPixel(x, y));
				var darkness = 255 - (color.R + color.G + color.B) / 3.0;

				if (darkness <= 20)
					continue;

				if (y <= strokePixels)
					topInkOffset = Math.Min(topInkOffset, x);
				if (x <= strokePixels)
					leftInkOffset = Math.Min(leftInkOffset, y);
			}
		}

		for (int y = margin; y < bitmap.Height - margin; y++)
		{
			for (int x = margin; x < bitmap.Width - margin; x++)
			{
				var color = new global::Android.Graphics.Color(bitmap.GetPixel(x, y));
				var darkness = 255 - (color.R + color.G + color.B) / 3.0;
				differenceBands[Mod(x - y, tilePixels)] += darkness;
				sumBands[Mod(x + y, tilePixels)] += darkness;

				if (darkness > 20)
					inkCount++;
				else if (darkness < 5)
					backgroundCount++;
			}
		}

		Assert.True(inkCount > tilePixels);
		Assert.True(backgroundCount > inkCount);

		var differencePhase = FindBandCenter(differenceBands, strokePixels);
		var sumPhase = FindBandCenter(sumBands, strokePixels);
		var (xPhase, yPhase) = ResolveAxes(differencePhase, sumPhase, tilePixels);
		var strength = (GetBandStrength(differenceBands) + GetBandStrength(sumBands)) / 2;

		return new PatternPhase(xPhase, yPhase, bitmap.Width, bitmap.Height, density, inkCount, strength, topInkOffset, leftInkOffset);
	}

	static double GetBandStrength(double[] bands)
	{
		var minimum = double.MaxValue;
		var maximum = double.MinValue;
		double total = 0;

		foreach (var value in bands)
		{
			minimum = Math.Min(minimum, value);
			maximum = Math.Max(maximum, value);
			total += value;
		}

		Assert.True(total > 0);
		return (maximum - minimum) / total;
	}

	static double FindBandCenter(double[] bands, int radius)
	{
		var peak = 0;
		for (int i = 1; i < bands.Length; i++)
		{
			if (bands[i] > bands[peak])
				peak = i;
		}

		var baseline = double.MaxValue;
		foreach (var value in bands)
			baseline = Math.Min(baseline, value);

		double weightedOffset = 0;
		double totalWeight = 0;
		for (int offset = -radius; offset <= radius; offset++)
		{
			var weight = Math.Max(0, bands[Mod(peak + offset, bands.Length)] - baseline);
			weightedOffset += offset * weight;
			totalWeight += weight;
		}

		Assert.True(totalWeight > 0);
		return Mod(peak + weightedOffset / totalWeight, bands.Length);
	}

	static (double X, double Y) ResolveAxes(double difference, double sum, int period)
	{
		var bestX = double.MaxValue;
		var bestY = double.MaxValue;
		var bestDistance = double.MaxValue;

		for (int differenceWrap = -1; differenceWrap <= 1; differenceWrap++)
		{
			for (int sumWrap = -1; sumWrap <= 1; sumWrap++)
			{
				var unwrappedDifference = difference + differenceWrap * period;
				var unwrappedSum = sum + sumWrap * period;
				var x = (unwrappedDifference + unwrappedSum) / 2;
				var y = (unwrappedSum - unwrappedDifference) / 2;
				var distance = x * x + y * y;

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestX = x;
					bestY = y;
				}
			}
		}

		return (bestX, bestY);
	}

	static int Mod(int value, int divisor) => (value % divisor + divisor) % divisor;

	static double Mod(double value, int divisor) => (value % divisor + divisor) % divisor;

	static double CircularDistance(double value, double reference, int period)
	{
		var difference = Math.Abs(value - reference) % period;
		return Math.Min(difference, period - difference);
	}

	sealed class DrawState
	{
		public int Value;
	}

	sealed class ReferenceDrawable : IDrawable
	{
		public DrawState DrawState;

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.FillColor = Colors.White;
			canvas.FillRectangle(0, 0, SurfaceSize, SurfaceSize);
			canvas.StrokeColor = Colors.Silver;
			canvas.StrokeSize = StrokeSize;

			for (float y = 0; y < SurfaceSize; y += TileSize)
			{
				for (float x = 0; x < SurfaceSize; x += TileSize)
				{
					canvas.DrawLine(x, y, x + TileSize, y + TileSize);
					canvas.DrawLine(x, y + TileSize, x + TileSize, y);
				}
			}

			Interlocked.Exchange(ref DrawState.Value, 1);
		}
	}

	sealed class PatternDrawable : IDrawable
	{
		public DrawState DrawState;

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.FillColor = Colors.White;
			canvas.FillRectangle(0, 0, SurfaceSize, SurfaceSize);

			IPattern pattern;
			using (var picture = new PictureCanvas(0, 0, TileSize, TileSize))
			{
				picture.StrokeColor = Colors.Silver;
				picture.StrokeSize = StrokeSize;
				picture.DrawLine(0, 0, TileSize, TileSize);
				picture.DrawLine(0, TileSize, TileSize, 0);
				pattern = new PicturePattern(picture.Picture, TileSize, TileSize);
			}

			var paint = new PatternPaint
			{
				Pattern = pattern,
				ForegroundColor = Colors.Silver
			};

			canvas.SetFillPaint(paint, new RectF(0, 0, SurfaceSize, SurfaceSize));
			canvas.FillRectangle(0, 0, SurfaceSize, SurfaceSize);
			Interlocked.Exchange(ref DrawState.Value, 1);
		}
	}

	readonly record struct PatternPhase(
		double X,
		double Y,
		int Width,
		int Height,
		float Density,
		int InkCount,
		double Strength,
		int TopInkOffset,
		int LeftInkOffset);
}
#endif


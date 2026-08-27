#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue28910")]
public class Issue28910 : ControlsHandlerTestBase
{
	const float PatternStart = 10;
	const float PatternSize = 250;
	const float TileSize = 10;

	[Fact]
	public async Task SetBlurSpreadsPatternIntoSurroundingBand()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
			});
		});

		var crisp = await RenderPatternAsync(0);
		var blurred = await RenderPatternAsync(10);

		var background = Average(
			crisp.Pixels.GetPixel(2, 2),
			crisp.Pixels.GetPixel(crisp.Pixels.Width - 3, 2),
			crisp.Pixels.GetPixel(2, crisp.Pixels.Height - 3));
		var silver = Average(crisp.LineSamples);
		var silverDistance = ColorDistance(silver, background);
		Assert.True(silverDistance > 80,
			$"The unblurred control did not render distinguishable silver pattern lines; color distance was {silverDistance:F1}.");

		var colorThreshold = Math.Max(12, silverDistance * 0.08);
		Assert.Equal(crisp.Pixels.Width, blurred.Pixels.Width);
		Assert.Equal(crisp.Pixels.Height, blurred.Pixels.Height);
		var blurredBackground = Average(
			blurred.Pixels.GetPixel(2, 2),
			blurred.Pixels.GetPixel(blurred.Pixels.Width - 3, 2),
			blurred.Pixels.GetPixel(2, blurred.Pixels.Height - 3));
		Assert.True(ColorDistance(blurredBackground, background) <= colorThreshold,
			"The control background changed between the zero-radius and radius-10 renders.");

		var crispLineCount = CountDifferent(crisp.LineSamples, background, colorThreshold);
		Assert.True(crispLineCount >= crisp.LineSamples.Count * 0.8,
			$"The unblurred control rendered only {crispLineCount} of {crisp.LineSamples.Count} expected silver line samples.");

		var crispBandCount = CountDifferent(crisp.BandSamples, background, colorThreshold);
		Assert.True(crispBandCount <= crisp.BandSamples.Count * 0.2,
			$"The zero-radius control unexpectedly colored {crispBandCount} of {crisp.BandSamples.Count} clean band samples.");

		var blurredLineCount = CountDifferent(blurred.LineSamples, background, colorThreshold);
		Assert.True(blurredLineCount >= blurred.LineSamples.Count * 0.5,
			$"SetBlur(10) removed the pattern itself; only {blurredLineCount} of {blurred.LineSamples.Count} line samples remained.");

		var blurredBandCount = CountDifferent(blurred.BandSamples, background, colorThreshold);
		var requiredBandCount = (int)Math.Ceiling(blurred.BandSamples.Count * 0.6);
		Assert.True(blurredBandCount >= requiredBandCount,
			$"SetBlur(10) did not spread the silver pattern into the expected blur band: observed {blurredBandCount} of {blurred.BandSamples.Count} colored band pixels; required at least {requiredBandCount} with color distance {colorThreshold:F1}.");
	}

	async Task<RenderedPattern> RenderPatternAsync(float blurRadius)
	{
		var drawCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var drawable = new PatternDrawable
		{
			BlurRadius = blurRadius,
			DrawCompletion = drawCompletion,
		};
		var graphicsView = new GraphicsView
		{
			Drawable = drawable,
			HeightRequest = 300,
			WidthRequest = 400,
		};
		var stack = new VerticalStackLayout
		{
			Padding = new Thickness(30, 0),
			Spacing = 25,
			VerticalOptions = LayoutOptions.Center,
			Children = { graphicsView },
		};
		var page = new ContentPage
		{
			Content = new ScrollView
			{
				Content = stack,
			},
		};
		var result = new TaskCompletionSource<RenderedPattern>(TaskCreationOptions.RunContinuationsAsynchronously);

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			await drawCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.True(drawable.DidDraw, "The drawable did not receive its first post-attachment Draw callback.");

			var handler = Assert.IsType<GraphicsViewHandler>(graphicsView.Handler);
			var nativeView = handler.PlatformView;
			Assert.NotNull(nativeView);
			Assert.True(nativeView.Frame.Width > 0 && nativeView.Frame.Height > 0,
				$"The native GraphicsView had an invalid frame of {nativeView.Frame}.");

			using var image = await nativeView.ToBitmap(MauiContext);
			var pixels = PixelImage.FromImage(image);
			Assert.True(pixels.Width > 0 && pixels.Height > 0,
				$"The native GraphicsView bitmap had invalid dimensions {pixels.Width}x{pixels.Height}.");

			var scaleX = pixels.Width / (double)nativeView.Bounds.Width;
			var scaleY = pixels.Height / (double)nativeView.Bounds.Height;
			Assert.True(scaleX > 0 && scaleY > 0, $"The native bitmap had invalid scale {scaleX:F2}x{scaleY:F2}.");

			var lineSamples = SamplePattern(pixels, scaleX, scaleY, 5, 5);
			var bandSamples = SamplePattern(pixels, scaleX, scaleY, 5, 1);
			result.SetResult(new RenderedPattern
			{
				Pixels = pixels,
				LineSamples = lineSamples,
				BandSamples = bandSamples,
			});
		});

		return await result.Task;
	}

	static List<PixelColor> SamplePattern(PixelImage pixels, double scaleX, double scaleY, float offsetX, float offsetY)
	{
		var samples = new List<PixelColor>();
		for (var y = PatternStart + TileSize; y < PatternStart + PatternSize - TileSize; y += TileSize)
		{
			for (var x = PatternStart + TileSize; x < PatternStart + PatternSize - TileSize; x += TileSize)
			{
				var pixelX = (int)Math.Round((x + offsetX) * scaleX);
				var pixelY = (int)Math.Round((y + offsetY) * scaleY);
				Assert.InRange(pixelX, 0, pixels.Width - 1);
				Assert.InRange(pixelY, 0, pixels.Height - 1);
				Assert.InRange(x + offsetX, PatternStart, PatternStart + PatternSize);
				Assert.InRange(y + offsetY, PatternStart, PatternStart + PatternSize);
				samples.Add(pixels.GetPixel(pixelX, pixelY));
			}
		}

		Assert.NotEmpty(samples);
		return samples;
	}

	static int CountDifferent(IReadOnlyList<PixelColor> samples, PixelColor background, double threshold)
	{
		var count = 0;
		foreach (var sample in samples)
		{
			if (ColorDistance(sample, background) > threshold)
				count++;
		}

		return count;
	}

	static PixelColor Average(params PixelColor[] colors) => Average((IReadOnlyList<PixelColor>)colors);

	static PixelColor Average(IReadOnlyList<PixelColor> colors)
	{
		double red = 0;
		double green = 0;
		double blue = 0;
		double alpha = 0;
		foreach (var color in colors)
		{
			red += color.Red;
			green += color.Green;
			blue += color.Blue;
			alpha += color.Alpha;
		}

		return new PixelColor
		{
			Red = red / colors.Count,
			Green = green / colors.Count,
			Blue = blue / colors.Count,
			Alpha = alpha / colors.Count,
		};
	}

	static double ColorDistance(PixelColor first, PixelColor second)
	{
		var red = first.Red - second.Red;
		var green = first.Green - second.Green;
		var blue = first.Blue - second.Blue;
		var alpha = first.Alpha - second.Alpha;
		return Math.Sqrt(red * red + green * green + blue * blue + alpha * alpha);
	}

	sealed class PatternDrawable : IDrawable
	{
		public float BlurRadius { get; set; }
		public TaskCompletionSource DrawCompletion { get; set; }

		public bool DidDraw { get; private set; }

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			IBlurrableCanvas blurrableCanvas = new ScalingCanvas(canvas);
			blurrableCanvas.SetBlur(BlurRadius);

			IPattern pattern;
			using (var picture = new PictureCanvas(0, 0, TileSize, TileSize))
			{
				picture.StrokeColor = Colors.Silver;
				picture.DrawLine(0, 0, TileSize, TileSize);
				picture.DrawLine(0, TileSize, TileSize, 0);
				pattern = new PicturePattern(picture.Picture, TileSize, TileSize);
			}

			canvas.SetFillPaint(new PatternPaint { Pattern = pattern }, RectF.Zero);
			canvas.FillRectangle(PatternStart, PatternStart, PatternSize, PatternSize);
			DidDraw = true;
			DrawCompletion.TrySetResult();
		}
	}

	sealed class RenderedPattern
	{
		public PixelImage Pixels { get; set; }
		public List<PixelColor> LineSamples { get; set; }
		public List<PixelColor> BandSamples { get; set; }
	}

	struct PixelColor
	{
		public double Red { get; set; }
		public double Green { get; set; }
		public double Blue { get; set; }
		public double Alpha { get; set; }
	}

	sealed class PixelImage
	{
		public byte[] Bytes { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }

		public PixelColor GetPixel(int x, int y)
		{
			var index = (y * Width + x) * 4;
			return new PixelColor
			{
				Red = Bytes[index],
				Green = Bytes[index + 1],
				Blue = Bytes[index + 2],
				Alpha = Bytes[index + 3],
			};
		}

		public static PixelImage FromImage(UIImage image)
		{
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);
			var width = (int)cgImage.Width;
			var height = (int)cgImage.Height;
			var bytes = new byte[width * height * 4];
			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(
				bytes,
				width,
				height,
				8,
				width * 4,
				colorSpace,
				CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
			context.DrawImage(new CGRect(0, 0, width, height), cgImage);
			return new PixelImage
			{
				Bytes = bytes,
				Width = width,
				Height = height,
			};
		}
	}
}
#endif


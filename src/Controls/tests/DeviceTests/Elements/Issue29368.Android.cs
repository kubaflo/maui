using System;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using ABitmap = Android.Graphics.Bitmap;
using ACanvas = Android.Graphics.Canvas;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue29368")]
public class Issue29368 : ControlsHandlerTestBase
{
	const int CanvasSize = 300;
	const int PatternStep = 10;

	[Fact]
	public async Task PatternPaintStartsAtGraphicsViewOrigin()
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
				handlers.AddHandler<Button, ButtonHandler>();
			});
		});

		var drawable = new PatternOriginDrawable();
		var graphicsView = new GraphicsView
		{
			BackgroundColor = Colors.White,
			Drawable = drawable,
			HeightRequest = CanvasSize,
			WidthRequest = CanvasSize,
			HorizontalOptions = LayoutOptions.Center
		};
		var button = new Button
		{
			Text = "Draw pattern at 0,0"
		};
		var clickObservation = -1;
		button.Clicked += (_, _) =>
		{
			clickObservation++;
			drawable.Mode = DrawMode.Pattern;
			graphicsView.Invalidate();
		};

		var page = new ContentPage
		{
			Title = "Home",
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "PatternPaint origin at (0,0)",
						FontSize = 20,
						HorizontalOptions = LayoutOptions.Center
					},
					new Label
					{
						Text = "The silver crosshatch should begin at the red top-left origin.",
						HorizontalTextAlignment = TextAlignment.Center
					},
					graphicsView,
					button,
					new Label
					{
						Text = "Silver crosshatch pattern",
						FontAttributes = FontAttributes.Bold,
						HorizontalTextAlignment = TextAlignment.Center
					}
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var graphicsHandler = Assert.IsType<GraphicsViewHandler>(graphicsView.Handler);
			var platformGraphicsView = Assert.IsType<PlatformTouchGraphicsView>(graphicsHandler.PlatformView);
			var platformButton = Assert.IsAssignableFrom<AppCompatButton>(button.Handler.PlatformView);
			var density = platformGraphicsView.Context.Resources.DisplayMetrics.Density;

			Assert.InRange(Math.Abs((platformGraphicsView.Width / density) - CanvasSize), 0, 1);
			Assert.InRange(Math.Abs((platformGraphicsView.Height / density) - CanvasSize), 0, 1);

			var previousDrawCount = drawable.DrawCount;
			drawable.Mode = DrawMode.DirectReference;
			graphicsView.Invalidate();
			await AssertEventually(
				() => drawable.DrawCount > previousDrawCount,
				message: "The direct crosshatch reference was not drawn.");

			using var reference = Capture(platformGraphicsView);
			ValidateZeroOriginReference(reference);

			previousDrawCount = drawable.DrawCount;
			drawable.Mode = DrawMode.White;
			graphicsView.Invalidate();
			await AssertEventually(
				() => drawable.DrawCount > previousDrawCount,
				message: "The white pre-click canvas was not drawn.");

			using var beforeClick = Capture(platformGraphicsView);
			Assert.True(CountDarkPixels(beforeClick) < (beforeClick.Width * beforeClick.Height / 100),
				"The pre-click GraphicsView was not white.");

			platformButton.PerformClick();
			await AssertEventually(
				() => clickObservation == 0,
				message: "The attached native button click was not observed.");
			await AssertEventually(
				() => drawable.PatternDrawCount >= 0,
				message: "The post-click PatternPaint redraw was not observed.");

			using var actual = Capture(platformGraphicsView);
			Assert.True(CountDarkPixels(actual) > (actual.Width * actual.Height / 50),
				"The post-click bitmap did not differ from the white canvas.");

			var (mismatchedPixels, comparedPixels) = CountMaskMismatches(reference, actual);
			var allowedMismatches = comparedPixels / 20;
			Assert.True(
				mismatchedPixels <= allowedMismatches,
				$"PatternPaint origin mismatch: {mismatchedPixels} mismatched pixels exceeded {allowedMismatches} allowed pixels; native size={actual.Width}x{actual.Height}, pattern step={PatternStep}.");
		});
	}

	static ABitmap Capture(PlatformTouchGraphicsView platformView)
	{
		var rootView = Assert.IsAssignableFrom<AView>(platformView.RootView);
		using var rootBitmap = ABitmap.CreateBitmap(rootView.Width, rootView.Height, ABitmap.Config.Argb8888!);
		using (var canvas = new ACanvas(rootBitmap))
			rootView.Draw(canvas);

		var viewLocation = new int[2];
		var rootLocation = new int[2];
		platformView.GetLocationOnScreen(viewLocation);
		rootView.GetLocationOnScreen(rootLocation);
		var left = viewLocation[0] - rootLocation[0];
		var top = viewLocation[1] - rootLocation[1];

		Assert.True(left >= 0 && top >= 0 &&
			left + platformView.Width <= rootBitmap.Width &&
			top + platformView.Height <= rootBitmap.Height,
			$"The native GraphicsView frame ({left},{top},{platformView.Width},{platformView.Height}) was outside the root bitmap ({rootBitmap.Width},{rootBitmap.Height}).");

		return ABitmap.CreateBitmap(rootBitmap, left, top, platformView.Width, platformView.Height);
	}

	static void ValidateZeroOriginReference(ABitmap bitmap)
	{
		var lineSamples = 0;
		var gapSamples = 0;
		var lineGrayTotal = 0;
		var gapGrayTotal = 0;

		for (var y = 0; y < CanvasSize; y += PatternStep * 2)
		{
			for (var x = 0; x < CanvasSize; x += PatternStep * 2)
			{
				var lineGray = GetGray(bitmap, ToPixel(x + 2, bitmap.Width), ToPixel(y + 2, bitmap.Height));
				var gapGray = GetGray(bitmap, ToPixel(x + 2, bitmap.Width), ToPixel(y + 5, bitmap.Height));
				lineGrayTotal += lineGray;
				gapGrayTotal += gapGray;

				if (lineGray < 224)
					lineSamples++;

				if (gapGray >= 224)
					gapSamples++;
			}
		}

		const int ExpectedSamples = 225;
		Assert.True(lineSamples > ExpectedSamples * 9 / 10,
			$"The direct reference did not contain the expected zero-origin diagonal: {lineSamples}/{ExpectedSamples} samples.");
		Assert.True(gapSamples > ExpectedSamples * 9 / 10,
			$"The direct reference did not contain the expected 10x10 gaps: {gapSamples}/{ExpectedSamples} samples.");
		Assert.True((gapGrayTotal - lineGrayTotal) / ExpectedSamples > 30,
			"The direct reference did not provide sufficient silver/white pixel separation.");
	}

	static int ToPixel(int logicalCoordinate, int pixelSize) =>
		Math.Min(pixelSize - 1, (int)((logicalCoordinate + 0.5f) * pixelSize / CanvasSize));

	static int CountDarkPixels(ABitmap bitmap)
	{
		var count = 0;
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (IsDark(bitmap, x, y))
					count++;
			}
		}

		return count;
	}

	static (int MismatchedPixels, int ComparedPixels) CountMaskMismatches(ABitmap expected, ABitmap actual)
	{
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);

		var mismatched = 0;
		var compared = 0;
		for (var y = 0; y < expected.Height; y++)
		{
			for (var x = 0; x < expected.Width; x++)
			{
				var expectedValue = GetGray(expected, x, y);
				if (expectedValue > 210 && expectedValue < 245)
					continue;

				compared++;
				var actualValue = GetGray(actual, x, y);
				if ((expectedValue <= 210 && actualValue > 225) ||
					(expectedValue >= 245 && actualValue < 230))
				{
					mismatched++;
				}
			}
		}

		return (mismatched, compared);
	}

	static bool IsDark(ABitmap bitmap, int x, int y) => GetGray(bitmap, x, y) < 224;

	static int GetGray(ABitmap bitmap, int x, int y)
	{
		var pixel = bitmap.GetPixel(x, y);
		return (AColor.GetRedComponent(pixel) + AColor.GetGreenComponent(pixel) + AColor.GetBlueComponent(pixel)) / 3;
	}

	enum DrawMode
	{
		White,
		DirectReference,
		Pattern
	}

	sealed class PatternOriginDrawable : IDrawable
	{
		public DrawMode Mode { get; set; }
		public int DrawCount { get; private set; }
		public int PatternDrawCount { get; private set; } = -1;

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			DrawCount++;
			canvas.FillColor = Colors.White;
			canvas.FillRectangle(0, 0, CanvasSize, CanvasSize);

			if (Mode == DrawMode.White)
				return;

			if (Mode == DrawMode.DirectReference)
			{
				canvas.StrokeColor = Colors.Silver;
				for (var y = 0; y < CanvasSize; y += PatternStep)
				{
					for (var x = 0; x < CanvasSize; x += PatternStep)
					{
						canvas.DrawLine(x, y, x + PatternStep, y + PatternStep);
						canvas.DrawLine(x, y + PatternStep, x + PatternStep, y);
					}
				}

				return;
			}

			IPattern pattern;
			using (var picture = new PictureCanvas(0, 0, PatternStep, PatternStep))
			{
				picture.StrokeColor = Colors.Silver;
				picture.DrawLine(0, 0, PatternStep, PatternStep);
				picture.DrawLine(0, PatternStep, PatternStep, 0);
				pattern = new PicturePattern(picture.Picture, PatternStep, PatternStep);
			}

			var patternPaint = new PatternPaint
			{
				Pattern = pattern
			};

			canvas.SetFillPaint(patternPaint, new RectF(0, 0, CanvasSize, CanvasSize));
			canvas.FillRectangle(0, 0, CanvasSize, CanvasSize);
			PatternDrawCount++;
		}
	}
}


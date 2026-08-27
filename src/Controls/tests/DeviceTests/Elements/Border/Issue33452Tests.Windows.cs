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
	[Category("Issue33452")]
	public class Issue33452 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task LowContrastLinearGradientRendersWithoutBroadColorBands()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var referenceBrush = CreateGradient("#101010", "#F0F0F0", "#101010");
			var referenceBorder = new Border
			{
				Background = referenceBrush,
			};
			var referencePage = new ContentPage
			{
				Content = referenceBorder,
			};

			var referenceCapture = await CaptureRenderedBorder(referencePage, referenceBorder);
			var referenceAnalysis = AnalyzeGradient(referenceCapture.Bitmap, referenceBrush);
			var referenceSpan = GetLargestChannelSpan(referenceBrush);
			var referenceRunBound = GetSmoothRunBound(referenceCapture.Bitmap.PixelWidth, referenceSpan);

			const int colorTolerance = 3;
			Assert.True(referenceSpan > colorTolerance);
			Assert.True(
				referenceAnalysis.MaxColorError <= colorTolerance,
				$"High-contrast gradient control colors were inaccurate: maximum error {referenceAnalysis.MaxColorError}, samples {referenceAnalysis.Samples}, expected {referenceAnalysis.Expected}.");
			Assert.True(
				referenceAnalysis.LongestRun <= referenceRunBound,
				$"High-contrast gradient control was not clean: longest run {referenceAnalysis.LongestRun}, bound {referenceRunBound}, samples {referenceAnalysis.Samples}.");

			var targetBrush = CreateGradient("#3B3B3B", "#454545", "#3B3B3B");
			var targetBorder = new Border
			{
				Background = targetBrush,
			};
			var sampleButton = new Button
			{
				Text = "Gradient sample",
			};

			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
				RowSpacing = 16,
			};
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 24,
				Text = "Issue 33452: Linear gradient banding",
			}, 0, 0);
			grid.Add(targetBorder, 0, 1);
			grid.Add(sampleButton, 0, 2);
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Text = "Low-contrast gradient",
			}, 0, 3);

			var targetPage = new ContentPage
			{
				Title = "Linear gradient banding",
				Content = grid,
			};

			var targetCapture = await CaptureRenderedBorder(targetPage, targetBorder);

			Assert.Equal(1, Grid.GetRow(targetBorder));
			Assert.True(targetCapture.NativeWidth > 0 && targetCapture.NativeHeight > 0,
				$"The native Border frame must be positive, but was {targetCapture.NativeWidth:F1}x{targetCapture.NativeHeight:F1}.");

			var targetSpan = GetLargestChannelSpan(targetBrush);
			Assert.True(targetSpan > colorTolerance,
				$"The arranged channel separation {targetSpan} must exceed tolerance {colorTolerance}.");

			var targetAnalysis = AnalyzeGradient(targetCapture.Bitmap, targetBrush);
			Assert.True(
				targetAnalysis.MaxColorError <= colorTolerance,
				$"Issue33452 gradient banding: low-contrast Border background contains endpoint or midpoint colors outside tolerance {colorTolerance}; maximum error was {targetAnalysis.MaxColorError}, samples {targetAnalysis.Samples}, expected {targetAnalysis.Expected}.");

			var smoothRunBound = GetSmoothRunBound(targetCapture.Bitmap.PixelWidth, targetSpan);
			Assert.True(
				targetAnalysis.LongestRun <= smoothRunBound,
				$"Issue33452 gradient banding: low-contrast Border background contains an identical-color run of {targetAnalysis.LongestRun} columns; smooth-gradient bound {smoothRunBound}, frame {targetCapture.NativeWidth:F1}x{targetCapture.NativeHeight:F1}, bitmap {targetCapture.Bitmap.PixelWidth}x{targetCapture.Bitmap.PixelHeight}, samples {targetAnalysis.Samples}, expected {targetAnalysis.Expected}.");
		}

		async Task<(RawBitmap Bitmap, double NativeWidth, double NativeHeight)> CaptureRenderedBorder(ContentPage page, Border border)
		{
			RawBitmap renderedBitmap = null;
			double nativeWidth = 0;
			double nativeHeight = 0;

			await CreateHandlerAndAddToWindow<IWindowHandler>(new Controls.Window(page), async _ =>
			{
				var borderHandler = border.Handler as BorderHandler;
				Assert.NotNull(borderHandler);
				Assert.Same(border, borderHandler.VirtualView);

				var nativeBorder = borderHandler.PlatformView;
				Assert.NotNull(nativeBorder);

				var renderedPixelsReady = false;
				await AssertHelpers.AssertEventually(async () =>
				{
					renderedBitmap = await border.AsRawBitmapAsync();
					renderedPixelsReady = HasRenderedSamples(renderedBitmap);
					return renderedPixelsReady;
				}, timeout: 5000, interval: 100, message: "The Border did not produce rendered endpoint and midpoint pixels.");

				Assert.True(renderedPixelsReady, "Rendered endpoint and midpoint pixels were not observed.");
				nativeWidth = nativeBorder.ActualWidth;
				nativeHeight = nativeBorder.ActualHeight;
			});

			Assert.NotNull(renderedBitmap);
			return (renderedBitmap, nativeWidth, nativeHeight);
		}

		static bool HasRenderedSamples(RawBitmap bitmap)
		{
			if (bitmap is null || bitmap.PixelWidth < 20 || bitmap.PixelHeight < 10)
				return false;

			var y = bitmap.PixelHeight / 2;
			return GetAlpha(bitmap, 2, y) > 0
				&& GetAlpha(bitmap, bitmap.PixelWidth / 2, y) > 0
				&& GetAlpha(bitmap, bitmap.PixelWidth - 3, y) > 0;
		}

		static (int LongestRun, int MaxColorError, string Samples, string Expected) AnalyzeGradient(
			RawBitmap bitmap,
			LinearGradientBrush brush)
		{
			var left = Math.Max(2, bitmap.PixelWidth / 50);
			var right = bitmap.PixelWidth - left - 1;
			var rows = new[]
			{
				bitmap.PixelHeight / 4,
				bitmap.PixelHeight * 3 / 8,
				bitmap.PixelHeight / 2,
				bitmap.PixelHeight * 5 / 8,
				bitmap.PixelHeight * 3 / 4,
			};

			Assert.True(left < right);
			foreach (var row in rows)
				Assert.InRange(row, 1, bitmap.PixelHeight - 2);

			var longestRun = 0;
			var currentRun = 0;
			var maximumError = 0;
			var previousRed = -1;
			var previousGreen = -1;
			var previousBlue = -1;

			for (var x = left; x <= right; x++)
			{
				Assert.InRange(x, 1, bitmap.PixelWidth - 2);

				var red = 0;
				var green = 0;
				var blue = 0;
				var expected = GetExpectedColor(brush, (double)x / (bitmap.PixelWidth - 1));
				foreach (var row in rows)
				{
					var pixel = GetPixel(bitmap, x, row);
					red += pixel.Red;
					green += pixel.Green;
					blue += pixel.Blue;
					maximumError = Math.Max(maximumError, Math.Abs(pixel.Red - expected.Red));
					maximumError = Math.Max(maximumError, Math.Abs(pixel.Green - expected.Green));
					maximumError = Math.Max(maximumError, Math.Abs(pixel.Blue - expected.Blue));
				}

				maximumError = Math.Max(maximumError, Math.Abs((int)Math.Round((double)red / rows.Length) - expected.Red));
				maximumError = Math.Max(maximumError, Math.Abs((int)Math.Round((double)green / rows.Length) - expected.Green));
				maximumError = Math.Max(maximumError, Math.Abs((int)Math.Round((double)blue / rows.Length) - expected.Blue));

				if (red == previousRed && green == previousGreen && blue == previousBlue)
				{
					currentRun++;
				}
				else
				{
					currentRun = 1;
					previousRed = red;
					previousGreen = green;
					previousBlue = blue;
				}

				longestRun = Math.Max(longestRun, currentRun);
			}

			var leftSample = GetMeanPixel(bitmap, left, rows);
			var middleSample = GetMeanPixel(bitmap, bitmap.PixelWidth / 2, rows);
			var rightSample = GetMeanPixel(bitmap, right, rows);
			var leftExpected = GetExpectedColor(brush, (double)left / (bitmap.PixelWidth - 1));
			var middleExpected = GetExpectedColor(brush, (double)(bitmap.PixelWidth / 2) / (bitmap.PixelWidth - 1));
			var rightExpected = GetExpectedColor(brush, (double)right / (bitmap.PixelWidth - 1));

			return (
				longestRun,
				maximumError,
				$"{FormatColor(leftSample)}/{FormatColor(middleSample)}/{FormatColor(rightSample)}",
				$"{FormatColor(leftExpected)}/{FormatColor(middleExpected)}/{FormatColor(rightExpected)}");
		}

		static LinearGradientBrush CreateGradient(string start, string middle, string end) =>
			new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0),
				GradientStops = new GradientStopCollection
				{
					new GradientStop(Color.FromArgb(start), 0),
					new GradientStop(Color.FromArgb(middle), 0.5f),
					new GradientStop(Color.FromArgb(end), 1),
				},
			};

		static int GetLargestChannelSpan(LinearGradientBrush brush)
		{
			var first = ToRgb(brush.GradientStops[0].Color);
			var middle = ToRgb(brush.GradientStops[1].Color);
			return Math.Max(Math.Abs(first.Red - middle.Red),
				Math.Max(Math.Abs(first.Green - middle.Green), Math.Abs(first.Blue - middle.Blue)));
		}

		static int GetSmoothRunBound(int width, int channelSpan) =>
			Math.Max(12, (int)Math.Ceiling((double)width / (channelSpan * 8)) + 2);

		static (int Red, int Green, int Blue) GetExpectedColor(LinearGradientBrush brush, double position)
		{
			var first = brush.GradientStops[0];
			var middle = brush.GradientStops[1];
			var last = brush.GradientStops[2];
			if (position <= middle.Offset)
				return Interpolate(ToRgb(first.Color), ToRgb(middle.Color), position / middle.Offset);

			return Interpolate(ToRgb(middle.Color), ToRgb(last.Color), (position - middle.Offset) / (last.Offset - middle.Offset));
		}

		static (int Red, int Green, int Blue) Interpolate(
			(int Red, int Green, int Blue) start,
			(int Red, int Green, int Blue) end,
			double amount) =>
			(
				InterpolateChannel(start.Red, end.Red, amount),
				InterpolateChannel(start.Green, end.Green, amount),
				InterpolateChannel(start.Blue, end.Blue, amount)
			);

		static int InterpolateChannel(int start, int end, double amount)
			=> (int)Math.Round(start + ((end - start) * amount));

		static (int Red, int Green, int Blue) ToRgb(Color color) =>
			(
				(int)Math.Round(color.Red * 255),
				(int)Math.Round(color.Green * 255),
				(int)Math.Round(color.Blue * 255)
			);

		static (int Red, int Green, int Blue) GetMeanPixel(RawBitmap bitmap, int x, int[] rows)
		{
			var red = 0;
			var green = 0;
			var blue = 0;
			foreach (var row in rows)
			{
				var pixel = GetPixel(bitmap, x, row);
				red += pixel.Red;
				green += pixel.Green;
				blue += pixel.Blue;
			}

			return (
				(int)Math.Round((double)red / rows.Length),
				(int)Math.Round((double)green / rows.Length),
				(int)Math.Round((double)blue / rows.Length)
			);
		}

		static (int Red, int Green, int Blue) GetPixel(RawBitmap bitmap, int x, int y)
		{
			var offset = ((y * bitmap.PixelWidth) + x) * 4;
			return (
				bitmap.PixelBuffer[offset + 2],
				bitmap.PixelBuffer[offset + 1],
				bitmap.PixelBuffer[offset]
			);
		}

		static byte GetAlpha(RawBitmap bitmap, int x, int y) =>
			bitmap.PixelBuffer[(((y * bitmap.PixelWidth) + x) * 4) + 3];

		static string FormatColor((int Red, int Green, int Blue) color) =>
			$"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
	}
}
#endif


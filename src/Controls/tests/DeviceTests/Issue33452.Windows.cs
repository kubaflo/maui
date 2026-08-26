#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WContentPanel = Microsoft.Maui.Platform.ContentPanel;
using WRoutedEventHandler = Microsoft.UI.Xaml.RoutedEventHandler;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue33452")]
	public class Issue33452 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task LowContrastLinearGradientRendersWithoutBoxLikeBands()
		{
			const double viewportWidth = 1000;
			const double viewportHeight = 700;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var targetBorder = CreateGradientBorder(
				Color.FromArgb("#3B3B3B"),
				Color.FromArgb("#454545"),
				Color.FromArgb("#3B3B3B"));

			var grid = new Grid
			{
				Padding = 32,
				RowSpacing = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(360),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};

			var titleLabel = new Label
			{
				Text = "MAUI LinearGradientBrush",
				TextColor = Colors.White,
				FontSize = 24,
				HorizontalOptions = LayoutOptions.Center,
			};
			var observationButton = new Button
			{
				Text = "Record visible box-like bands",
			};
			var resultLabel = new Label
			{
				Text = "Rendered gradient",
				TextColor = Colors.White,
				FontSize = 20,
				HorizontalOptions = LayoutOptions.Center,
			};

			grid.Add(titleLabel, 0, 0);
			grid.Add(targetBorder, 0, 1);
			grid.Add(observationButton, 0, 2);
			grid.Add(resultLabel, 0, 3);

			var page = new ContentPage
			{
				Title = "Linear gradient rendering",
				BackgroundColor = Color.FromArgb("#202020"),
				WidthRequest = viewportWidth,
				HeightRequest = viewportHeight,
				Content = grid,
			};

			bool targetLoaded = false;
			bool targetBitmapCompleted = false;
			double targetNativeWidth = -1;
			double targetNativeHeight = -1;
			RawBitmap targetBitmap = null;
			WContentPanel targetNativeBorder = null;

			await AttachAndRun(page, async _ =>
			{
				targetNativeBorder = targetBorder.Handler.PlatformView as WContentPanel;
				Assert.NotNull(targetNativeBorder);

				targetLoaded = await WaitUntilNativeLoaded(targetNativeBorder);
				targetNativeWidth = targetNativeBorder.ActualWidth;
				targetNativeHeight = targetNativeBorder.ActualHeight;
				targetBitmap = await targetBorder.AsRawBitmapAsync().WaitAsync(TimeSpan.FromSeconds(5));
				targetBitmapCompleted =
					targetBitmap.PixelWidth > 0 &&
					targetBitmap.PixelHeight > 0 &&
					targetBitmap.PixelBuffer.Length == targetBitmap.PixelWidth * targetBitmap.PixelHeight * 4;
			});

			Assert.NotNull(targetNativeBorder);
			Assert.True(targetLoaded, "The native Border must be loaded before its rendering is inspected.");
			Assert.Equal(viewportWidth - grid.Padding.HorizontalThickness, targetNativeWidth, 1);
			Assert.Equal(360, targetNativeHeight, 1);
			Assert.NotNull(targetBitmap);
			Assert.True(targetBitmapCompleted, "The target Border bitmap must complete with nonempty BGRA pixels.");
			Assert.InRange(Math.Abs(targetBitmap.Width - targetNativeWidth), 0, 1);
			Assert.InRange(Math.Abs(targetBitmap.Height - targetNativeHeight), 0, 1);

			const int endpointValue = 59;
			const int centerValue = 69;
			const int colorTolerance = 3;
			Assert.True(
				centerValue - endpointValue > colorTolerance,
				"The arranged gradient color span must exceed the sampling tolerance.");

			AssertNeighborhoodMatches(targetBitmap, 0.1, 0.5, 61, colorTolerance);
			AssertNeighborhoodMatches(targetBitmap, 0.5, 0.5, centerValue, colorTolerance);
			AssertNeighborhoodMatches(targetBitmap, 0.9, 0.5, 61, colorTolerance);

			var calibrationBorder = CreateGradientBorder(Colors.Black, Colors.White, Colors.Black);
			calibrationBorder.WidthRequest = targetNativeWidth;
			calibrationBorder.HeightRequest = targetNativeHeight;

			bool calibrationLoaded = false;
			bool calibrationBitmapCompleted = false;
			RawBitmap calibrationBitmap = null;
			WContentPanel calibrationNativeBorder = null;

			await AttachAndRun(calibrationBorder, async _ =>
			{
				calibrationNativeBorder = calibrationBorder.Handler.PlatformView as WContentPanel;
				Assert.NotNull(calibrationNativeBorder);

				calibrationLoaded = await WaitUntilNativeLoaded(calibrationNativeBorder);
				calibrationBitmap = await calibrationBorder.AsRawBitmapAsync().WaitAsync(TimeSpan.FromSeconds(5));
				calibrationBitmapCompleted =
					calibrationBitmap.PixelWidth > 0 &&
					calibrationBitmap.PixelHeight > 0 &&
					calibrationBitmap.PixelBuffer.Length == calibrationBitmap.PixelWidth * calibrationBitmap.PixelHeight * 4;
			});

			Assert.NotNull(calibrationNativeBorder);
			Assert.True(calibrationLoaded, "The calibration Border must be loaded before its rendering is inspected.");
			Assert.NotNull(calibrationBitmap);
			Assert.True(calibrationBitmapCompleted, "The calibration bitmap must complete with nonempty BGRA pixels.");
			Assert.InRange(Math.Abs(calibrationBitmap.Width - targetBitmap.Width), 0, 1);
			Assert.InRange(Math.Abs(calibrationBitmap.Height - targetBitmap.Height), 0, 1);

			int allowedRun = (int)Math.Ceiling(calibrationBitmap.PixelWidth / (2d * 255)) + 4;
			int calibrationMaximumRun = GetMaximumConstantColorRun(calibrationBitmap);
			Assert.True(
				calibrationMaximumRun <= allowedRun,
				$"The band analyzer must resolve the arranged high-contrast gradient; measured run {calibrationMaximumRun}, allowed {allowedRun}.");

			int targetMaximumRun = GetMaximumConstantColorRun(targetBitmap);
			Assert.True(
				targetMaximumRun <= allowedRun,
				$"Issue 33452 gradient banding: maximum constant-color run {targetMaximumRun} pixels exceeded allowed {allowedRun} pixels.");
		}

		static Border CreateGradientBorder(Color start, Color center, Color end)
		{
			return new Border
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(1, 0),
					GradientStops = new GradientStopCollection
					{
						new GradientStop(start, 0),
						new GradientStop(center, 0.5f),
						new GradientStop(end, 1),
					},
				},
			};
		}

		static async Task<bool> WaitUntilNativeLoaded(WContentPanel nativeBorder)
		{
			if (nativeBorder.IsLoaded)
				return true;

			var loadedCompletion = new TaskCompletionSource<bool>();
			WRoutedEventHandler loadedHandler = null;
			loadedHandler = (_, _) =>
			{
				nativeBorder.Loaded -= loadedHandler;
				loadedCompletion.TrySetResult(true);
			};
			nativeBorder.Loaded += loadedHandler;

			await loadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
			return nativeBorder.IsLoaded;
		}

		static void AssertNeighborhoodMatches(RawBitmap bitmap, double relativeX, double relativeY, int expected, int tolerance)
		{
			int centerX = (int)Math.Round((bitmap.PixelWidth - 1) * relativeX);
			int centerY = (int)Math.Round((bitmap.PixelHeight - 1) * relativeY);
			long channelTotal = 0;
			int channelCount = 0;

			for (int y = centerY - 2; y <= centerY + 2; y++)
			{
				for (int x = centerX - 2; x <= centerX + 2; x++)
				{
					Assert.InRange(x, 0, bitmap.PixelWidth - 1);
					Assert.InRange(y, 0, bitmap.PixelHeight - 1);

					int pixel = ((y * bitmap.PixelWidth) + x) * 4;
					channelTotal += bitmap.PixelBuffer[pixel];
					channelTotal += bitmap.PixelBuffer[pixel + 1];
					channelTotal += bitmap.PixelBuffer[pixel + 2];
					channelCount += 3;
				}
			}

			double actual = channelTotal / (double)channelCount;
			Assert.InRange(actual, expected - tolerance, expected + tolerance);
		}

		static int GetMaximumConstantColorRun(RawBitmap bitmap)
		{
			int maximumRun = 0;
			int startX = bitmap.PixelWidth / 20;
			int endX = bitmap.PixelWidth - startX;
			int[] scanlines =
			{
				bitmap.PixelHeight * 9 / 20,
				bitmap.PixelHeight / 2,
				bitmap.PixelHeight * 11 / 20,
			};

			foreach (int y in scanlines)
			{
				Assert.InRange(y, 0, bitmap.PixelHeight - 1);
				Assert.InRange(startX, 0, bitmap.PixelWidth - 1);
				Assert.InRange(endX - 1, 0, bitmap.PixelWidth - 1);

				int currentRun = 1;
				for (int x = startX + 1; x < endX; x++)
				{
					if (PixelsEqual(bitmap, x - 1, y, x, y))
					{
						currentRun++;
					}
					else
					{
						maximumRun = Math.Max(maximumRun, currentRun);
						currentRun = 1;
					}
				}

				maximumRun = Math.Max(maximumRun, currentRun);
			}

			return maximumRun;
		}

		static bool PixelsEqual(RawBitmap bitmap, int firstX, int firstY, int secondX, int secondY)
		{
			int first = ((firstY * bitmap.PixelWidth) + firstX) * 4;
			int second = ((secondY * bitmap.PixelWidth) + secondX) * 4;

			return
				bitmap.PixelBuffer[first] == bitmap.PixelBuffer[second] &&
				bitmap.PixelBuffer[first + 1] == bitmap.PixelBuffer[second + 1] &&
				bitmap.PixelBuffer[first + 2] == bitmap.PixelBuffer[second + 2] &&
				bitmap.PixelBuffer[first + 3] == bitmap.PixelBuffer[second + 3];
		}
	}
}
#endif


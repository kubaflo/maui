#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.ImageAnalysis;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue33452")]
	public class Issue33452 : ControlsHandlerTestBase
	{
		const int MaximumSmoothRun = 8;
		const int ColorTolerance = 2;

		[Fact]
		public async Task SubtleHorizontalGradientRendersWithoutFlatColorBands()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
				});
			});

			var calibrationStops = new GradientStopCollection
			{
				new GradientStop(Colors.Black, 0),
				new GradientStop(Colors.White, 0.5f),
				new GradientStop(Colors.Black, 1),
			};
			var calibrationBorder = new Border
			{
				WidthRequest = 512,
				HeightRequest = 64,
				Background = new LinearGradientBrush(calibrationStops, new Point(0, 0), new Point(1, 0)),
			};

			var calibrationBitmap = await GetRawBitmap(calibrationBorder, typeof(BorderHandler));
			var calibrationHandler = Assert.IsType<BorderHandler>(calibrationBorder.Handler);
			Assert.Same(calibrationBorder, calibrationHandler.VirtualView);
			AssertGradientConfiguration(calibrationHandler, calibrationStops);
			Assert.Equal(512, calibrationBitmap.Width, 2);
			Assert.Equal(64, calibrationBitmap.Height, 2);
			Assert.Equal(512, calibrationHandler.PlatformView.ActualWidth, 2);
			Assert.Equal(64, calibrationHandler.PlatformView.ActualHeight, 2);
			AssertExpectedGradientSamples(calibrationBitmap, calibrationStops);
			Assert.True(
				GetLongestIdenticalColorRun(calibrationBitmap) <= MaximumSmoothRun,
				"The high-contrast calibration gradient did not produce a smooth rendered scanline.");

			var targetStops = new GradientStopCollection
			{
				new GradientStop(Color.FromArgb("#3B3B3B"), 0),
				new GradientStop(Color.FromArgb("#454545"), 0.5f),
				new GradientStop(Color.FromArgb("#3B3B3B"), 1),
			};
			var targetBorder = new Border
			{
				Background = new LinearGradientBrush(targetStops, new Point(0, 0), new Point(1, 0)),
			};
			var mauiLabel = new Label
			{
				Text = "Maui",
				FontSize = 28,
				FontAttributes = FontAttributes.Bold,
				TextColor = Colors.Red,
			};
			var resultLabel = new Label
			{
				Text = "Gradient",
				FontSize = 20,
				FontAttributes = FontAttributes.Bold,
			};
			var checkButton = new Button
			{
				Text = "Check visible gradient",
			};
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};
			grid.Add(mauiLabel);
			grid.Add(targetBorder);
			grid.Add(resultLabel);
			grid.Add(checkButton);
			Grid.SetRow(targetBorder, 1);
			Grid.SetRow(resultLabel, 2);
			Grid.SetRow(checkButton, 3);

			var page = new ContentPage { Content = grid };
			var density = DeviceDisplay.Current.MainDisplayInfo.Density;
			Assert.True(density > 0);
			var window = new Window(page)
			{
				Width = 1280 / density,
				Height = 720 / density,
			};
			var targetLoaded = false;
			targetBorder.Loaded += (_, _) => targetLoaded = true;

			await CreateHandlerAndAddToWindow<IWindowHandler>(window, async _ =>
			{
				await OnLoadedAsync(targetBorder);
				Assert.True(targetLoaded, "The target Border did not complete attachment.");

				var nativePage = Assert.IsAssignableFrom<WFrameworkElement>(page.Handler.PlatformView);
				var rasterScale = nativePage.XamlRoot.RasterizationScale;
				Assert.InRange(nativePage.ActualWidth * rasterScale, 1200, 1280);
				Assert.InRange(nativePage.ActualHeight * rasterScale, 600, 720);

				var targetHandler = Assert.IsType<BorderHandler>(targetBorder.Handler);
				Assert.Same(targetBorder, targetHandler.VirtualView);
				Assert.Equal(1, Grid.GetRow(targetBorder));
				Assert.True(targetBorder.Frame.Y >= mauiLabel.Frame.Bottom);
				AssertGradientConfiguration(targetHandler, targetStops);

				var targetBitmap = await targetBorder.AsRawBitmapAsync();
				Assert.True(targetBitmap.PixelWidth >= 1000);
				Assert.True(targetBitmap.PixelHeight >= 300);
				Assert.InRange(
					targetHandler.PlatformView.ActualWidth * rasterScale,
					targetBitmap.PixelWidth - 2,
					targetBitmap.PixelWidth + 2);
				Assert.InRange(
					targetHandler.PlatformView.ActualHeight * rasterScale,
					targetBitmap.PixelHeight - 2,
					targetBitmap.PixelHeight + 2);
				AssertExpectedGradientSamples(targetBitmap, targetStops);

				var longestRun = GetLongestIdenticalColorRun(targetBitmap);
				Assert.True(
					longestRun <= MaximumSmoothRun,
					$"Issue 33452 gradient contains a visible flat color band: longest identical-color run was {longestRun} physical pixels.");
			});
		}

		static void AssertGradientConfiguration(BorderHandler handler, GradientStopCollection expectedStops)
		{
			var borderPath = handler.PlatformView.BorderPath;
			Assert.NotNull(borderPath);
			var nativeBrush = Assert.IsType<WLinearGradientBrush>(borderPath.Fill);

			Assert.Equal(0, nativeBrush.StartPoint.X);
			Assert.Equal(0, nativeBrush.StartPoint.Y);
			Assert.Equal(1, nativeBrush.EndPoint.X);
			Assert.Equal(0, nativeBrush.EndPoint.Y);
			Assert.Equal(expectedStops.Count, nativeBrush.GradientStops.Count);

			for (int i = 0; i < expectedStops.Count; i++)
			{
				Assert.Equal((double)expectedStops[i].Offset, nativeBrush.GradientStops[i].Offset, 3);
				Assert.Equal(ToByte(expectedStops[i].Color.Red), nativeBrush.GradientStops[i].Color.R);
				Assert.Equal(ToByte(expectedStops[i].Color.Green), nativeBrush.GradientStops[i].Color.G);
				Assert.Equal(ToByte(expectedStops[i].Color.Blue), nativeBrush.GradientStops[i].Color.B);
			}
		}

		static void AssertExpectedGradientSamples(RawBitmap bitmap, GradientStopCollection stops)
		{
			var endpoint = ExpectedColor(stops, 0);
			var midpoint = ExpectedColor(stops, 0.5);
			Assert.True(
				Math.Abs(midpoint.Red - endpoint.Red) > ColorTolerance,
				"The arranged endpoint and midpoint colors must be distinguishable by the pixel oracle.");

			int y = bitmap.PixelHeight / 2;
			AssertColorNear(endpoint, PixelAt(bitmap, 2, y));
			AssertColorNear(midpoint, PixelAt(bitmap, bitmap.PixelWidth / 2, y));
			AssertColorNear(endpoint, PixelAt(bitmap, bitmap.PixelWidth - 3, y));
		}

		static int GetLongestIdenticalColorRun(RawBitmap bitmap)
		{
			Assert.True(bitmap.PixelWidth > 6);
			Assert.True(bitmap.PixelHeight > 6);

			int longestRun = 0;
			int centerY = bitmap.PixelHeight / 2;
			for (int y = centerY - 2; y <= centerY + 2; y++)
			{
				var previous = PixelAt(bitmap, 2, y);
				int currentRun = 1;

				for (int x = 3; x < bitmap.PixelWidth - 2; x++)
				{
					var current = PixelAt(bitmap, x, y);
					if (current.Red == previous.Red &&
						current.Green == previous.Green &&
						current.Blue == previous.Blue)
					{
						currentRun++;
					}
					else
					{
						longestRun = Math.Max(longestRun, currentRun);
						currentRun = 1;
						previous = current;
					}
				}

				longestRun = Math.Max(longestRun, currentRun);
			}

			return longestRun;
		}

		static (byte Blue, byte Green, byte Red, byte Alpha) PixelAt(RawBitmap bitmap, int x, int y)
		{
			Assert.InRange(x, 0, bitmap.PixelWidth - 1);
			Assert.InRange(y, 0, bitmap.PixelHeight - 1);

			int index = ((y * bitmap.PixelWidth) + x) * 4;
			return (
				bitmap.PixelBuffer[index],
				bitmap.PixelBuffer[index + 1],
				bitmap.PixelBuffer[index + 2],
				bitmap.PixelBuffer[index + 3]);
		}

		static (byte Blue, byte Green, byte Red, byte Alpha) ExpectedColor(
			GradientStopCollection stops,
			double position)
		{
			GradientStop start = stops[0];
			GradientStop end = stops[stops.Count - 1];

			for (int i = 1; i < stops.Count; i++)
			{
				if (position <= stops[i].Offset)
				{
					start = stops[i - 1];
					end = stops[i];
					break;
				}
			}

			double range = end.Offset - start.Offset;
			double progress = range == 0 ? 0 : (position - start.Offset) / range;
			return (
				Interpolate(start.Color.Blue, end.Color.Blue, progress),
				Interpolate(start.Color.Green, end.Color.Green, progress),
				Interpolate(start.Color.Red, end.Color.Red, progress),
				Interpolate(start.Color.Alpha, end.Color.Alpha, progress));
		}

		static byte Interpolate(float start, float end, double progress) =>
			ToByte(start + ((end - start) * progress));

		static byte ToByte(double value) =>
			(byte)Math.Round(value * byte.MaxValue, MidpointRounding.AwayFromZero);

		static void AssertColorNear(
			(byte Blue, byte Green, byte Red, byte Alpha) expected,
			(byte Blue, byte Green, byte Red, byte Alpha) actual)
		{
			Assert.InRange(Math.Abs(actual.Blue - expected.Blue), 0, ColorTolerance);
			Assert.InRange(Math.Abs(actual.Green - expected.Green), 0, ColorTolerance);
			Assert.InRange(Math.Abs(actual.Red - expected.Red), 0, ColorTolerance);
			Assert.InRange(Math.Abs(actual.Alpha - expected.Alpha), 0, ColorTolerance);
		}
	}
}
#endif


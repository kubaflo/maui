#if MACCATALYST
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.DatePicker)]
[Category("Issue30532")]
public class Issue30532 : ControlsHandlerTestBase
{
	const string DisplayedTime = "12:30";
	const double CharacterSpacing = 10;
	const double PixelToleranceInPoints = 3;

	[Fact]
	public async Task CharacterSpacingExpandsRenderedTimeAfterAttachment()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<StackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var timePicker = new TimePicker
		{
			Time = new TimeSpan(12, 30, 0),
			Format = "HH:mm",
			CharacterSpacing = 0,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		var page = new ContentPage
		{
			Content = new StackLayout
			{
				Spacing = 20,
				Margin = new Thickness(20),
				Children =
				{
					new Label
					{
						Text = "The TimePicker should visibly spread its characters after CharacterSpacing is applied.",
						HorizontalOptions = LayoutOptions.Center
					},
					timePicker,
					new Label { Text = "Character spacing rendering", HorizontalOptions = LayoutOptions.Center },
					new Button { Text = "Apply CharacterSpacing 10", HorizontalOptions = LayoutOptions.Center },
					new Button { Text = "Check rendered spacing", HorizontalOptions = LayoutOptions.Center }
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var handler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var platformView = Assert.IsType<UIDatePicker>(handler.PlatformView);

			AssertNativePickerState(timePicker, platformView);
			var scale = (double)platformView.Window.Screen.Scale;
			var tolerance = PixelToleranceInPoints * scale;
			var calibration = MeasureCalibrationSpans(scale);
			Assert.True(
				calibration.Spaced - calibration.Baseline > tolerance,
				$"The rendered-glyph-span oracle could not distinguish kerned text: baseline {calibration.Baseline}, spaced {calibration.Spaced}, scale {scale}, tolerance {tolerance}.");

			var baselineSpan = MeasureRenderedGlyphSpan(platformView, scale);
			var propertyChanged = false;
			timePicker.PropertyChanged += OnPropertyChanged;

			timePicker.CharacterSpacing = CharacterSpacing;

			Assert.True(propertyChanged, "TimePicker did not raise CharacterSpacing property changed after attachment.");
			Assert.Equal(CharacterSpacing, timePicker.CharacterSpacing);
			await WaitForNativeRedraw();
			Assert.Same(platformView, Assert.IsType<TimePickerHandler>(timePicker.Handler).PlatformView);
			AssertNativePickerState(timePicker, platformView);
			var spacedSpan = MeasureRenderedGlyphSpan(platformView, scale);
			var requiredSpan = baselineSpan + ((DisplayedTime.Length - 1) * CharacterSpacing * scale) - tolerance;

			Assert.True(
				spacedSpan >= requiredSpan,
				$"Mac Catalyst TimePicker glyph span mismatch: baseline {baselineSpan}, observed {spacedSpan}, required {requiredSpan}, scale {scale}, tolerance {tolerance}.");

			timePicker.PropertyChanged -= OnPropertyChanged;

			void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == TimePicker.CharacterSpacingProperty.PropertyName)
					propertyChanged = true;
			}
		});
	}

	static void AssertNativePickerState(TimePicker timePicker, UIDatePicker platformView)
	{
		Assert.Equal("HH:mm", timePicker.Format);
		Assert.Equal(new TimeSpan(12, 30, 0), timePicker.Time);
		Assert.Equal(12, platformView.Date.ToDateTime().Hour);
		Assert.Equal(30, platformView.Date.ToDateTime().Minute);
		Assert.False(platformView.Hidden);
		Assert.True(platformView.Alpha > 0);
		Assert.True(platformView.Bounds.Width > 0 && platformView.Bounds.Height > 0);
		Assert.NotNull(platformView.Window);

		var frameInWindow = platformView.ConvertRectToView(platformView.Bounds, platformView.Window);
		Assert.True(frameInWindow.IntersectsWith(platformView.Window.Bounds), "The native TimePicker is not visible in its window.");
	}

	static (int Baseline, int Spaced) MeasureCalibrationSpans(double scale)
	{
		var baseline = CreateCalibrationLabel(0);
		var spaced = CreateCalibrationLabel(CharacterSpacing);
		try
		{
			Assert.Equal(DisplayedTime, baseline.Text);
			Assert.Equal(DisplayedTime, spaced.Text);
			return (MeasureRenderedGlyphSpan(baseline, scale), MeasureRenderedGlyphSpan(spaced, scale));
		}
		finally
		{
			baseline.Dispose();
			spaced.Dispose();
		}
	}

	static UILabel CreateCalibrationLabel(double characterSpacing)
	{
		var attributedText = new NSMutableAttributedString(DisplayedTime);
		attributedText.AddAttribute(
			UIStringAttributeKey.KerningAdjustment,
			new NSNumber(characterSpacing),
			new NSRange(0, attributedText.Length));

		return new UILabel(new CGRect(0, 0, 200, 50))
		{
			AttributedText = attributedText,
			BackgroundColor = UIColor.Clear,
			TextColor = UIColor.Black
		};
	}

	static async Task WaitForNativeRedraw()
	{
		var redraw = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var displayLink = CADisplayLink.Create(() => redraw.TrySetResult());
		displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
		await redraw.Task.WaitAsync(TimeSpan.FromSeconds(2));
		displayLink.RemoveFromRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
	}

	static int MeasureRenderedGlyphSpan(UIView view, double scale)
	{
		var capture = Capture(view, scale);
		var hasTransparentBackground = CountPixels(capture, (_, _, _, alpha) => alpha < 16) > capture.Width * capture.Height / 2;
		(byte Red, byte Green, byte Blue) background = hasTransparentBackground
			? ((byte)0, (byte)0, (byte)0)
			: GetBackgroundColor(capture);
		var minimumX = capture.Width;
		var maximumX = -1;
		var foregroundPixels = 0;

		VisitPixels(capture, (x, _, red, green, blue, alpha) =>
		{
			var isForeground = hasTransparentBackground
				? alpha >= 24
				: alpha >= 24 && ColorDistance(red, green, blue, background) >= 48;
			if (!isForeground)
				return;

			foregroundPixels++;
			minimumX = Math.Min(minimumX, x);
			maximumX = Math.Max(maximumX, x);
		});

		Assert.True(foregroundPixels > 10, "Rendered text capture did not contain enough foreground pixels.");
		Assert.InRange(minimumX, 0, capture.Width - 1);
		Assert.InRange(maximumX, minimumX, capture.Width - 1);
		return maximumX - minimumX + 1;
	}

	static PixelCapture Capture(UIView view, double scale)
	{
		Assert.True(view.Bounds.Width > 0 && view.Bounds.Height > 0);
		var format = new UIGraphicsImageRendererFormat { Opaque = false, Scale = (nfloat)scale };
		using var renderer = new UIGraphicsImageRenderer(view.Bounds.Size, format);
		using var image = renderer.CreateImage(context => view.Layer.RenderInContext(context.CGContext));
		var cgImage = image.CGImage;
		var width = (int)cgImage.Width;
		var height = (int)cgImage.Height;
		var pixels = new byte[width * height * 4];
		using var colorSpace = CGColorSpace.CreateDeviceRGB();
		using var context = new CGBitmapContext(
			pixels,
			width,
			height,
			8,
			width * 4,
			colorSpace,
			CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
		context.DrawImage(new CGRect(0, 0, width, height), cgImage);
		Assert.Equal(width * height * 4, pixels.Length);
		return new PixelCapture(width, height, pixels);
	}

	static int CountPixels(PixelCapture capture, Func<byte, byte, byte, byte, bool> predicate)
	{
		var count = 0;
		VisitPixels(capture, (_, _, red, green, blue, alpha) =>
		{
			if (predicate(red, green, blue, alpha))
				count++;
		});
		return count;
	}

	static (byte Red, byte Green, byte Blue) GetBackgroundColor(PixelCapture capture)
	{
		long red = 0;
		long green = 0;
		long blue = 0;
		var count = 0;

		for (var x = 0; x < capture.Width; x++)
		{
			AddPixel(x, 0);
			AddPixel(x, capture.Height - 1);
		}

		for (var y = 1; y < capture.Height - 1; y++)
		{
			AddPixel(0, y);
			AddPixel(capture.Width - 1, y);
		}

		Assert.True(count > 0, "Rendered text capture did not contain an opaque background sample.");
		return ((byte)(red / count), (byte)(green / count), (byte)(blue / count));

		void AddPixel(int x, int y)
		{
			var pixel = GetPixel(capture, x, y);
			if (pixel.Alpha < 24)
				return;

			red += pixel.Red;
			green += pixel.Green;
			blue += pixel.Blue;
			count++;
		}
	}

	static (byte Red, byte Green, byte Blue, byte Alpha) GetPixel(PixelCapture capture, int x, int y)
	{
		Assert.InRange(x, 0, capture.Width - 1);
		Assert.InRange(y, 0, capture.Height - 1);
		var offset = ((y * capture.Width) + x) * 4;
		return (capture.Pixels[offset], capture.Pixels[offset + 1], capture.Pixels[offset + 2], capture.Pixels[offset + 3]);
	}

	static int ColorDistance(byte red, byte green, byte blue, (byte Red, byte Green, byte Blue) reference) =>
		Math.Abs(red - reference.Red) + Math.Abs(green - reference.Green) + Math.Abs(blue - reference.Blue);

	static void VisitPixels(PixelCapture capture, Action<int, int, byte, byte, byte, byte> visitor)
	{
		for (var y = 0; y < capture.Height; y++)
		{
			for (var x = 0; x < capture.Width; x++)
			{
				var offset = ((y * capture.Width) + x) * 4;
				Assert.InRange(offset + 3, 3, capture.Pixels.Length - 1);
				visitor(
					x,
					y,
					capture.Pixels[offset],
					capture.Pixels[offset + 1],
					capture.Pixels[offset + 2],
					capture.Pixels[offset + 3]);
			}
		}
	}

	readonly record struct PixelCapture(int Width, int Height, byte[] Pixels);
}
#endif


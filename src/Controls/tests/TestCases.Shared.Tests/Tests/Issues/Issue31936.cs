#if IOS
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31936 : _IssuesUITest
{
	const double CalibrationTolerance = 5;
	const double GlyphCenterTolerance = 3;
	const double LightGlyphLuminanceDelta = 1;

	public Issue31936(TestDevice device) : base(device) { }

	public override string Issue => "Back button FontImageSource glyph is not vertically centered on iOS 26";

	[Test]
	[Category(UITestCategories.Shell)]
	public void FontImageSourceBackGlyphIsVerticallyCentered()
	{
		App.SetOrientationPortrait();
		var window = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(window.Height, Is.GreaterThan(window.Width), "The test requires portrait orientation.");
		App.WaitForElement("Issue31936RootMarker");

		var rootTitleQuery = AppiumQuery.ByXPath("//XCUIElementTypeNavigationBar//XCUIElementTypeStaticText[@name='Commands']");
		var rootTitle = App.WaitForElement(rootTitleQuery);
		var rootMeasurement = MeasureContrastingPixels(App.Screenshot(), window, rootTitle.GetRect());
		Assert.That(rootMeasurement.PixelCount, Is.GreaterThan(10), "The Commands title must contain a nontrivial rendered pixel cluster.");
		Assert.That(rootMeasurement.GlyphBounds.Height, Is.GreaterThan(2), "The Commands title must span multiple rendered pixel rows.");
		Assert.That(
			Math.Abs(rootMeasurement.CentroidY - rootMeasurement.FrameCenterY),
			Is.LessThanOrEqualTo(CalibrationTolerance),
			$"The title calibration pixel centroid was not centered: centroid={rootMeasurement.CentroidY:F2}, frame center={rootMeasurement.FrameCenterY:F2}.");

		App.Tap("Issue31936OpenG4");
		var transitionState = -1;
		App.WaitForElement("Issue31936G4Marker");
		var g4TitleQuery = AppiumQuery.ByXPath("//XCUIElementTypeNavigationBar//XCUIElementTypeStaticText[@name='G4']");
		var g4Title = App.WaitForElement(g4TitleQuery);
		Assert.That(g4Title.GetRect().Width, Is.GreaterThan(0), "The expected G4 page title must be present after navigation.");

		var backButtonQuery = AppiumQuery.ByXPath("//XCUIElementTypeNavigationBar//XCUIElementTypeButton[1]");
		var backButton = App.WaitForElement(backButtonQuery);
		transitionState = 1;
		Assert.That(transitionState, Is.EqualTo(1), "The G4 navigation and native back-button transition must complete.");

		var measurement = MeasureContrastingPixels(App.Screenshot(), window, backButton.GetRect(), lightForeground: true);
		Assert.That(measurement.PixelCount, Is.GreaterThan(5), "The native back button must contain a nontrivial rendered glyph cluster.");
		Assert.That(measurement.GlyphBounds.Height, Is.GreaterThan(2), "The custom back glyph must span multiple rendered pixel rows.");

		var offset = measurement.CentroidY - measurement.FrameCenterY;
		Assert.That(
			Math.Abs(offset),
			Is.LessThanOrEqualTo(GlyphCenterTolerance),
			$"Issue31936 custom back glyph vertical center mismatch: observed centroid={measurement.CentroidY:F2}, expected frame center={measurement.FrameCenterY:F2}, offset={offset:F2}, tolerance={GlyphCenterTolerance:F2}, frame={measurement.FrameBounds}, glyphBounds={measurement.GlyphBounds}");
	}

	static PixelMeasurement MeasureContrastingPixels(byte[] screenshot, Rectangle windowBounds, Rectangle frame, bool lightForeground = false)
	{
		Assert.That(frame.Width, Is.GreaterThan(0), "The native element frame must have positive width.");
		Assert.That(frame.Height, Is.GreaterThan(0), "The native element frame must have positive height.");

		using var image = new MagickImage(screenshot);
		var scaleX = image.Width / (double)windowBounds.Width;
		var scaleY = image.Height / (double)windowBounds.Height;
		var left = Math.Max(0, (int)Math.Floor((frame.Left - windowBounds.Left) * scaleX));
		var top = Math.Max(0, (int)Math.Floor((frame.Top - windowBounds.Top) * scaleY));
		var right = Math.Min((int)image.Width, (int)Math.Ceiling((frame.Right - windowBounds.Left) * scaleX));
		var bottom = Math.Min((int)image.Height, (int)Math.Ceiling((frame.Bottom - windowBounds.Top) * scaleY));
		Assert.That(right, Is.GreaterThan(left), "The native element frame must be inside the screenshot horizontally.");
		Assert.That(bottom, Is.GreaterThan(top), "The native element frame must be inside the screenshot vertically.");

		using var pixels = image.GetPixels();
		var backgroundLuminance = lightForeground
			? double.NaN
			: (
				PixelLuminance(pixels, left, top) +
				PixelLuminance(pixels, right - 1, top) +
				PixelLuminance(pixels, left, bottom - 1) +
				PixelLuminance(pixels, right - 1, bottom - 1)) / 4;

		var scanLeft = lightForeground ? left + ((right - left) / 4) : left + 1;
		var scanRight = lightForeground ? right - ((right - left) / 4) : right - 1;
		var neighborOffset = Math.Max(1, (right - left) / 40);

		var count = 0;
		var sumY = 0d;
		var minX = right;
		var minY = bottom;
		var maxX = left;
		var maxY = top;
		for (var y = top + 1; y < bottom - 1; y++)
		{
			for (var x = scanLeft; x < scanRight; x++)
			{
				var color = pixels.GetPixel(x, y).ToColor();
				if (color is null)
					continue;

				var luminance = Luminance(color);
				if (lightForeground
					? luminance - (
						PixelLuminance(pixels, x - neighborOffset, y) +
						PixelLuminance(pixels, x + neighborOffset, y)) / 2 < LightGlyphLuminanceDelta
					: Math.Abs(luminance - backgroundLuminance) < 80)
					continue;

				count++;
				sumY += y + 0.5;
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		var frameBounds = new Rectangle(left, top, right - left, bottom - top);
		var glyphBounds = count == 0
			? Rectangle.Empty
			: Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
		var centroidY = count == 0 ? double.NaN : sumY / count;
		return new PixelMeasurement(centroidY, top + ((bottom - top) / 2d), count, frameBounds, glyphBounds);
	}

	static double PixelLuminance(IPixelCollection<byte> pixels, int x, int y)
	{
		var color = pixels.GetPixel(x, y).ToColor();
		if (color is null)
			throw new InvalidOperationException("The native element background pixel must be readable.");
		return Luminance(color);
	}

	static double Luminance(IMagickColor<byte> color) =>
		(0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);

	readonly record struct PixelMeasurement(
		double CentroidY,
		double FrameCenterY,
		int PixelCount,
		Rectangle FrameBounds,
		Rectangle GlyphBounds);
}
#endif

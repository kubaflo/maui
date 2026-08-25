#if ANDROID
using System.Drawing;
using System.Globalization;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30990 : _IssuesUITest
{
	const string IconToolbarItemId = "Issue30990IconToolbarItem";
	const string StatusLabelId = "Issue30990StatusLabel";
	const string TextToolbarItemId = "Issue30990TextToolbarItem";
	const int ColorTolerance = 24;

	public Issue30990(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Shell toolbar ignores shell properties";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorAppliesToToolbarIcon()
	{
		App.SetOrientationPortrait();

		var loaded = App.WaitForTextToBePresentInElement(
			StatusLabelId,
			"LOADED:",
			TimeSpan.FromSeconds(10));
		Assert.That(loaded, Is.True, "The ContentPage Loaded callback did not report the active Shell color.");

		var statusElement = App.FindElement(StatusLabelId);
		if (statusElement is null)
			throw new InvalidOperationException("The loaded status element was not found.");

		var status = statusElement.GetText();
		if (status is null)
			throw new InvalidOperationException("The loaded status element did not expose text.");

		var statusParts = status.Split('|');
		Assert.That(statusParts, Has.Length.EqualTo(2), $"Unexpected loaded status: {status}");
		Assert.That(statusParts[1], Is.EqualTo("groceries.png"), "The toolbar did not use the reported file image source.");

		var expected = ParseRgba(statusParts[0]);
		Assert.That(expected, Is.EqualTo((Red: (byte)255, Green: (byte)0, Blue: (byte)0, Alpha: (byte)255)),
			$"The active Shell foreground color was not red: {statusParts[0]}");

		var textItem = App.WaitForElement(
			AppiumQuery.ByXPath($"//*[@content-desc='{TextToolbarItemId}']"));
		if (textItem is null)
			throw new InvalidOperationException("The Text 1 toolbar action was not found.");

		var textItemValue = textItem.GetText();
		Assert.That(textItemValue, Is.EqualTo("Text 1"), "The identified text toolbar action was not Text 1.");

		var iconItem = App.WaitForElement(
			AppiumQuery.ByXPath($"//*[@content-desc='{IconToolbarItemId}']"));
		if (iconItem is null)
			throw new InvalidOperationException("The groceries toolbar action was not found.");

		var textRectangle = textItem.GetRect();
		var iconRectangle = iconItem.GetRect();
		Assert.That(textRectangle.Width, Is.GreaterThan(0), "The Text 1 toolbar action had no native width.");
		Assert.That(textRectangle.Height, Is.GreaterThan(0), "The Text 1 toolbar action had no native height.");
		Assert.That(iconRectangle.Width, Is.GreaterThan(0), "The groceries toolbar action had no native width.");
		Assert.That(iconRectangle.Height, Is.GreaterThan(0), "The groceries toolbar action had no native height.");

		var screenshotBytes = App.Screenshot();
		if (screenshotBytes is null)
			throw new InvalidOperationException("The rendered screen could not be captured.");

		using var screenshot = new MagickImage(screenshotBytes);
		var screenshotWidth = checked((int)screenshot.Width);
		var screenshotHeight = checked((int)screenshot.Height);
		Assert.That(screenshotHeight, Is.GreaterThan(screenshotWidth), "The test device was not in portrait orientation.");
		AssertRectangleInsideScreenshot(textRectangle, screenshotWidth, screenshotHeight, "Text 1");
		AssertRectangleInsideScreenshot(iconRectangle, screenshotWidth, screenshotHeight, "groceries");

		var textResult = AnalyzeInsetRegion(screenshot, textRectangle, expected);
		var textRequired = Math.Max(24, textResult.Sampled / 100);
		Assert.That(textResult.Matched, Is.GreaterThanOrEqualTo(textRequired),
			$"The Text 1 toolbar item did not prove the red-pixel oracle: matched={textResult.Matched}, required={textRequired}, sampled={textResult.Sampled}, dominant={textResult.Dominant}.");

		var iconResult = AnalyzeInsetRegion(screenshot, iconRectangle, expected);
		var iconRequired = Math.Max(24, iconResult.Sampled / 100);
		Assert.That(iconResult.Matched, Is.GreaterThanOrEqualTo(iconRequired),
			$"Issue30990 toolbar icon tint mismatch: matched={iconResult.Matched}, required={iconRequired}, sampled={iconResult.Sampled}, tolerance={ColorTolerance}, expected=#{expected.Red:X2}{expected.Green:X2}{expected.Blue:X2}{expected.Alpha:X2}, rectangle={iconRectangle}, dominant={iconResult.Dominant}.");
	}

	static (byte Red, byte Green, byte Blue, byte Alpha) ParseRgba(string loadedColor)
	{
		const string prefix = "LOADED:#";
		Assert.That(loadedColor, Does.StartWith(prefix), $"Unexpected loaded color status: {loadedColor}");
		var rgba = loadedColor[prefix.Length..];
		Assert.That(rgba, Has.Length.EqualTo(8), $"Unexpected RGBA value: {rgba}");

		return (
			byte.Parse(rgba.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			byte.Parse(rgba.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			byte.Parse(rgba.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			byte.Parse(rgba.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
	}

	static void AssertRectangleInsideScreenshot(Rectangle rectangle, int width, int height, string identity)
	{
		Assert.That(rectangle.X, Is.GreaterThanOrEqualTo(0), $"{identity} started outside the screenshot.");
		Assert.That(rectangle.Y, Is.GreaterThanOrEqualTo(0), $"{identity} started outside the screenshot.");
		Assert.That(rectangle.Right, Is.LessThanOrEqualTo(width), $"{identity} extended outside the screenshot.");
		Assert.That(rectangle.Bottom, Is.LessThanOrEqualTo(height), $"{identity} extended outside the screenshot.");
	}

	static PixelResult AnalyzeInsetRegion(
		MagickImage screenshot,
		Rectangle rectangle,
		(byte Red, byte Green, byte Blue, byte Alpha) expected)
	{
		var inset = Math.Max(2, Math.Min(rectangle.Width, rectangle.Height) / 10);
		var left = rectangle.Left + inset;
		var top = rectangle.Top + inset;
		var right = rectangle.Right - inset;
		var bottom = rectangle.Bottom - inset;
		var matched = 0;
		var sampled = 0;
		var colors = new Dictionary<(byte Red, byte Green, byte Blue), int>();

		using var pixels = screenshot.GetPixels();
		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				var color = pixels.GetPixel(x, y).ToColor();
				if (color is null)
					throw new InvalidOperationException($"The pixel at ({x}, {y}) did not expose a color.");

				sampled++;

				if (WithinTolerance(color.R, expected.Red) &&
					WithinTolerance(color.G, expected.Green) &&
					WithinTolerance(color.B, expected.Blue) &&
					WithinTolerance(color.A, expected.Alpha))
				{
					matched++;
				}

				var bucket = ((byte)(color.R & 0xF0), (byte)(color.G & 0xF0), (byte)(color.B & 0xF0));
				colors[bucket] = colors.GetValueOrDefault(bucket) + 1;
			}
		}

		Assert.That(sampled, Is.GreaterThan(0), $"The inset region for {rectangle} contained no pixels.");
		var dominant = colors.MaxBy(pair => pair.Value).Key;
		return new PixelResult(matched, sampled, $"#{dominant.Red:X2}{dominant.Green:X2}{dominant.Blue:X2}");
	}

	static bool WithinTolerance(byte actual, byte expected) =>
		Math.Abs(actual - expected) <= ColorTolerance;

	readonly record struct PixelResult(int Matched, int Sampled, string Dominant);
}
#endif

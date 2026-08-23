#if ANDROID
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37306 : _IssuesUITest
{
	const int ColorTolerance = 8;
	const string ScrollViewId = "Issue37306ScrollView";

	public Issue37306(TestDevice device) : base(device)
	{
	}

	public override string Issue => "[Android] ScrollView clips content at the safe-area inset while scrolling";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ContentScrollsThroughBottomSafeAreaInset()
	{
		App.SetOrientationPortrait();
		var scrollView = App.WaitForElement(ScrollViewId);
		using var initialScreenshot = new MagickImage(App.Screenshot());
		var screenshotWidth = checked((int)initialScreenshot.Width);
		var screenshotHeight = checked((int)initialScreenshot.Height);

		Assert.That(screenshotHeight, Is.GreaterThan(screenshotWidth),
			"The Android device must be in portrait orientation.");

		var diagnostics = App.WaitForElement(
			AppiumQuery.ByXPath("//*[contains(@content-desc,'Inset=') and contains(@content-desc,'Default=True') and not(contains(@content-desc,'Inset=-1'))]"),
			timeout: TimeSpan.FromSeconds(10)).GetAttribute<string>("content-desc") ?? string.Empty;
		var bottomInset = ReadInt(diagnostics, "Inset");

		Assert.That(bottomInset, Is.GreaterThan(0), "The root window must provide a nonzero bottom system-bar inset.");

		var firstItem = App.WaitForElement("Issue37306Item0");
		var firstRect = firstItem.GetRect();
		Assert.That(firstRect.Width, Is.GreaterThan(20), "Item 0 must have a nonempty native width.");
		Assert.That(firstRect.Height, Is.GreaterThan(20), "Item 0 must have a nonempty native height.");

		var initialPixel = ReadPixel(initialScreenshot, firstRect.X + 8, firstRect.Y + 8);
		Assert.That(IsWhite(initialPixel), Is.True,
			$"Item 0 must initially render white, but sampled RGB({initialPixel.Red},{initialPixel.Green},{initialPixel.Blue}).");

		var scrollRect = scrollView.GetRect();
		App.DragCoordinates(
			scrollRect.CenterX(),
			scrollRect.Y + (scrollRect.Height * 3 / 4),
			scrollRect.CenterX(),
			scrollRect.Y + (scrollRect.Height / 5));

		var scrolledElement = App.WaitForElement(
			AppiumQuery.ByXPath("//*[contains(@content-desc,'Scrolled=true')]"),
			timeout: TimeSpan.FromSeconds(10));
		diagnostics = scrolledElement.GetAttribute<string>("content-desc") ?? string.Empty;
		var scrollOffset = ReadDouble(diagnostics, "Scroll");
		Assert.That(scrollOffset, Is.GreaterThan(0), "The Scrolled callback must report a positive native scroll offset.");
		App.WaitForElement("Issue37306Item12", timeout: TimeSpan.FromSeconds(10));

		var sampleY = screenshotHeight - Math.Max(2, bottomInset / 2);
		NativeItemBounds sampledItem = default;
		for (var alignmentAttempt = 0; alignmentAttempt < 8; alignmentAttempt++)
		{
			if (TryFindItemAcrossSampleLine(sampleY, out sampledItem))
				break;

			var previousDiagnostics = App.WaitForElement(ScrollViewId)
				.GetAttribute<string>("content-desc") ?? string.Empty;
			var previousScrollValue = ReadValue(previousDiagnostics, "Scroll");
			App.DragCoordinates(
				scrollRect.CenterX(),
				sampleY - Math.Max(30, bottomInset),
				scrollRect.CenterX(),
				sampleY - Math.Max(50, bottomInset + 20));
			App.WaitForElement(
				AppiumQuery.ByXPath(
					$"//*[contains(@content-desc,'Inset=') and contains(@content-desc,'Default=True') and not(contains(@content-desc,'Scroll={previousScrollValue};'))]"),
				timeoutMessage: $"The alignment drag must change the scroll offset from {previousScrollValue} before another drag is sent.",
				timeout: TimeSpan.FromSeconds(10));
		}

		Assert.That(sampledItem.Number, Is.GreaterThanOrEqualTo(0),
			$"A numbered white item must have native bounds crossing y={sampleY} inside the bottom inset.");
		Assert.That(sampleY, Is.GreaterThanOrEqualTo(screenshotHeight - bottomInset),
			"The sample must be inside the runtime bottom system-bar inset band.");
		Assert.That(sampleY, Is.GreaterThan(sampledItem.Top).And.LessThan(sampledItem.Bottom),
			$"The sample must be inside Item {sampledItem.Number}'s native bounds.");
		Assert.That(sampledItem.Bottom - sampledItem.Top, Is.EqualTo(firstRect.Height).Within(2),
			$"Item {sampledItem.Number} must retain the same native height as the initial 56-unit Item 0.");

		using var screenshot = new MagickImage(App.Screenshot());
		var finalScreenshotWidth = checked((int)screenshot.Width);
		var finalScreenshotHeight = checked((int)screenshot.Height);
		Assert.That(sampleY, Is.InRange(0, finalScreenshotHeight - 1), "The sample y-coordinate must be inside the screenshot.");
		var sampleX = sampledItem.Left + 8;
		Assert.That(sampleX, Is.InRange(0, finalScreenshotWidth - 1), "The sample x-coordinate must be inside the screenshot.");
		Assert.That(sampleX, Is.GreaterThan(sampledItem.Left), "The sample must be right of the item's left edge.");
		Assert.That(sampleX, Is.LessThan(sampledItem.Right), "The sample must be inside the identified item.");
		Assert.That(sampleX, Is.LessThan(sampledItem.Left + ((sampledItem.Right - sampledItem.Left) / 4)),
			"The sample must stay in the left quarter of the item, away from its centered label.");

		var sampledPixel = ReadPixel(screenshot, sampleX, sampleY);
		Assert.That(IsWhite(sampledPixel), Is.True,
			$"Safe-area sample inside the white item was amber RGB({sampledPixel.Red},{sampledPixel.Green},{sampledPixel.Blue}); expected white RGB(255,255,255) within tolerance 8.");
	}

	bool TryFindItemAcrossSampleLine(int sampleY, out NativeItemBounds itemBounds)
	{
		for (var itemNumber = 5; itemNumber < 30; itemNumber++)
		{
			var elements = App.FindElements($"Issue37306Item{itemNumber}");
			if (elements.Count == 0)
				continue;

			var description = elements.First().GetAttribute<string>("content-desc") ?? string.Empty;
			if (!description.Contains("Top=", StringComparison.Ordinal))
				continue;

			var candidate = new NativeItemBounds(
				itemNumber,
				ReadInt(description, "Left"),
				ReadInt(description, "Top"),
				ReadInt(description, "Right"),
				ReadInt(description, "Bottom"));

			if (candidate.Top + 2 < sampleY && candidate.Bottom - 2 > sampleY)
			{
				itemBounds = candidate;
				return true;
			}
		}

		itemBounds = new NativeItemBounds(-1, 0, 0, 0, 0);
		return false;
	}

	static bool IsWhite(RgbPixel pixel) =>
		Math.Abs(pixel.Red - 255) <= ColorTolerance &&
		Math.Abs(pixel.Green - 255) <= ColorTolerance &&
		Math.Abs(pixel.Blue - 255) <= ColorTolerance;

	static int ReadInt(string diagnostics, string key) =>
		int.Parse(ReadValue(diagnostics, key), System.Globalization.CultureInfo.InvariantCulture);

	static double ReadDouble(string diagnostics, string key) =>
		double.Parse(ReadValue(diagnostics, key), System.Globalization.CultureInfo.InvariantCulture);

	static string ReadValue(string diagnostics, string key)
	{
		var prefix = $"{key}=";
		var start = diagnostics.IndexOf(prefix, StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Diagnostic '{key}' was missing from '{diagnostics}'.");
		start += prefix.Length;
		var end = diagnostics.IndexOf(';', start);
		return end < 0 ? diagnostics[start..] : diagnostics[start..end];
	}

	static RgbPixel ReadPixel(MagickImage image, int x, int y)
	{
		Assert.That(x, Is.InRange(0, checked((int)image.Width) - 1));
		Assert.That(y, Is.InRange(0, checked((int)image.Height) - 1));
		using var pixels = image.GetPixels();
		var color = pixels.GetPixel(x, y).ToColor()
			?? throw new AssertionException($"ImageMagick could not read the pixel at ({x},{y}).");
		return new RgbPixel(color.R, color.G, color.B);
	}

	readonly record struct NativeItemBounds(int Number, int Left, int Top, int Right, int Bottom);

	readonly record struct RgbPixel(int Red, int Green, int Blue);
}
#endif

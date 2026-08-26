#if WINDOWS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31510 : _IssuesUITest
{
	public Issue31510(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell flyout and TitleBar transparency overlap";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SemiTransparentFlyoutMatchesTitleBarInOverlap()
	{
		var setupElement = App.WaitForElement("Issue31510NativeSetup");
		if (setupElement is null)
			throw new AssertionException("Issue31510 native setup element was not found.");

		var setupText = setupElement.GetText();
		Assert.That(setupText, Is.EqualTo("Mica=BaseAlt;TitleBar=#85FFFFFF;Flyout=#85FFFFFF"));

		var titleBarElement = App.WaitForElement("Issue31510TitleBarProbe");
		var contentElement = App.WaitForElement("Issue31510ContentProbe");
		if (titleBarElement is null || contentElement is null)
			throw new AssertionException("Issue31510 probe elements were not found.");

		var titleBarFrame = titleBarElement.GetRect();
		var contentFrame = contentElement.GetRect();
		Assert.That(titleBarFrame.Width, Is.GreaterThan(0));
		Assert.That(titleBarFrame.Height, Is.GreaterThan(0));
		Assert.That(contentFrame.Width, Is.GreaterThan(0));
		Assert.That(contentFrame.Height, Is.GreaterThan(0));

		byte[] closedBytes = App.Screenshot();
		Assert.That(closedBytes, Is.Not.Null.And.Not.Empty);

		App.Tap("Issue31510OpenFlyoutButton");
		var flyoutEvidence = App.WaitForElement("Issue31510FlyoutEvidence");
		var flyoutElement = App.WaitForElement("Issue31510FlyoutProbe");
		if (flyoutEvidence is null || flyoutElement is null)
			throw new AssertionException("Issue31510 flyout header was not visible.");

		var tokenElement = App.WaitForElement("Issue31510PresentationToken");
		var stateElement = App.WaitForElement("Issue31510FlyoutState");
		if (tokenElement is null || stateElement is null)
			throw new AssertionException("Issue31510 presentation evidence was not found.");

		var tokenText = tokenElement.GetText();
		var stateText = stateElement.GetText();
		Assert.That(tokenText, Is.EqualTo("1"), "The presentation callback did not replace its -1 sentinel.");
		Assert.That(stateText, Is.EqualTo(bool.TrueString));

		var flyoutFrame = flyoutElement.GetRect();
		Assert.That(flyoutFrame.Width, Is.GreaterThan(0));
		Assert.That(flyoutFrame.Height, Is.GreaterThan(0));

		byte[] openBytes = App.Screenshot();
		Assert.That(openBytes, Is.Not.Null.And.Not.Empty);

		using var closedImage = new MagickImage(closedBytes);
		using var openImage = new MagickImage(openBytes);
		Assert.That(openImage.Width, Is.EqualTo(closedImage.Width));
		Assert.That(openImage.Height, Is.EqualTo(closedImage.Height));

		double windowLeft = Math.Min(titleBarFrame.Left, contentFrame.Left);
		double windowTop = Math.Min(titleBarFrame.Top, contentFrame.Top);
		double windowRight = Math.Max(titleBarFrame.Right, contentFrame.Right);
		double windowBottom = Math.Max(titleBarFrame.Bottom, contentFrame.Bottom);
		double windowWidth = windowRight - windowLeft;
		double windowHeight = windowBottom - windowTop;
		Assert.That(windowWidth, Is.GreaterThan(0));
		Assert.That(windowHeight, Is.GreaterThan(0));

		double sampleLeft = Math.Max(titleBarFrame.Left + 80, flyoutFrame.Left + 80);
		double sampleRight = Math.Min(titleBarFrame.Right - 20, flyoutFrame.Right - 20);
		double sampleY = titleBarFrame.Top + (titleBarFrame.Height * 0.72);
		Assert.That(sampleRight - sampleLeft, Is.GreaterThan(60), "The measured flyout/title-bar intersection was too small.");

		var closedSamples = new List<Rgb>();
		var openSamples = new List<Rgb>();
		for (int index = 1; index <= 3; index++)
		{
			double x = sampleLeft + ((sampleRight - sampleLeft) * index / 4);
			closedSamples.Add(ReadPixel(closedImage, x, sampleY, windowLeft, windowTop, windowWidth, windowHeight));
			openSamples.Add(ReadPixel(openImage, x, sampleY, windowLeft, windowTop, windowWidth, windowHeight));
		}

		Rgb titleBarMean = Mean(closedSamples);
		Rgb overlapMean = Mean(openSamples);
		double alpha = 0x85 / 255d;
		Rgb backdrop = new(
			(titleBarMean.Red - alpha * 255) / (1 - alpha),
			(titleBarMean.Green - alpha * 255) / (1 - alpha),
			(titleBarMean.Blue - alpha * 255) / (1 - alpha));
		Assert.That(backdrop.Red, Is.InRange(0, 255));
		Assert.That(backdrop.Green, Is.InRange(0, 255));
		Assert.That(backdrop.Blue, Is.InRange(0, 255));

		Rgb expected = new(
			alpha * 255 + (1 - alpha) * backdrop.Red,
			alpha * 255 + (1 - alpha) * backdrop.Green,
			alpha * 255 + (1 - alpha) * backdrop.Blue);
		double opaqueDistance = Distance(expected, new Rgb(255, 255, 255));
		Assert.That(opaqueDistance, Is.GreaterThan(2),
			"The active Mica-relative expected color was not distinguishable from opaque white.");
		double tolerance = Math.Min(8, opaqueDistance / 2);

		Assert.That(Distance(overlapMean, expected), Is.LessThanOrEqualTo(tolerance),
			$"Issue31510 overlap remained opaque: observed {overlapMean}, expected {expected}, tolerance {tolerance}.");
	}

	static Rgb ReadPixel(MagickImage image, double x, double y, double left, double top, double width, double height)
	{
		bool screenshotContainsScreenCoordinates =
			image.Width >= left + width &&
			image.Height >= top + height;
		int pixelX = screenshotContainsScreenCoordinates
			? (int)Math.Round(x)
			: (int)Math.Round((x - left) * image.Width / width);
		int pixelY = screenshotContainsScreenCoordinates
			? (int)Math.Round(y)
			: (int)Math.Round((y - top) * image.Height / height);
		Assert.That(pixelX, Is.InRange(0, (int)image.Width - 1));
		Assert.That(pixelY, Is.InRange(0, (int)image.Height - 1));

		using var pixels = image.GetPixels();
		var color = pixels.GetPixel(pixelX, pixelY).ToColor();
		if (color is null)
			throw new AssertionException($"Unable to read screenshot pixel at ({pixelX}, {pixelY}).");

		return new Rgb(color.R, color.G, color.B);
	}

	static Rgb Mean(IReadOnlyList<Rgb> samples) =>
		new(samples.Average(sample => sample.Red), samples.Average(sample => sample.Green), samples.Average(sample => sample.Blue));

	static double Distance(Rgb first, Rgb second) =>
		Math.Max(Math.Abs(first.Red - second.Red), Math.Max(Math.Abs(first.Green - second.Green), Math.Abs(first.Blue - second.Blue)));

	readonly record struct Rgb(double Red, double Green, double Blue)
	{
		public override string ToString() => $"({Red:F1}, {Green:F1}, {Blue:F1})";
	}
}
#endif

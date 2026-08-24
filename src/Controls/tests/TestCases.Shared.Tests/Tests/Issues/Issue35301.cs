#if WINTEST
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling by default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectedItemDoesNotGainDefaultWindowsSelectionPixels()
	{
		var apple = App.WaitForElement("Apple");
		var banana = App.WaitForElement("Banana");
		var cherry = App.WaitForElement("Cherry");
		var selectionState = App.WaitForElement("SelectionState");
		Assert.That(apple.GetText(), Is.EqualTo("Apple"));
		Assert.That(banana.GetText(), Is.EqualTo("Banana"));
		Assert.That(cherry.GetText(), Is.EqualTo("Cherry"));
		Assert.That(selectionState.GetText(), Is.EqualTo("NONE"));

		var appleRect = apple.GetRect();
		var bananaRect = banana.GetRect();
		var cherryRect = cherry.GetRect();
		Assert.That(appleRect.Bottom, Is.LessThanOrEqualTo(bananaRect.Top));
		Assert.That(bananaRect.Bottom, Is.LessThanOrEqualTo(cherryRect.Top));
		var appleRegion = GetSampleRegion(appleRect);
		var bananaRegion = GetSampleRegion(bananaRect);

		var before = CaptureSettledScreenshots(appleRegion, bananaRegion, out var cleanNoise, out var appleSampledPixels);
		var allowedDistortion = Math.Max(0.001, cleanNoise * 3);

		App.Click("Apple");
		Assert.That(
			App.WaitForTextToBePresentInElement("SelectionState", "Apple", TimeSpan.FromSeconds(10)),
			Is.True,
			"Apple selection did not complete.");
		var selectedState = App.WaitForElement("SelectionState");
		Assert.That(selectedState.GetText(), Is.EqualTo("Apple"));

		var after = CaptureSettledScreenshots(appleRegion, bananaRegion, out var selectedSettleNoise, out _);
		var bananaNoise = MeasurePixelDistortion(before.Second, after.Second, bananaRegion, out _);
		Assert.That(selectedSettleNoise, Is.LessThanOrEqualTo(allowedDistortion),
			$"The selected Apple rendering was not settled: {selectedSettleNoise:F6} distortion.");

		Assert.That(bananaNoise, Is.LessThanOrEqualTo(allowedDistortion),
			$"Unselected Banana changed unexpectedly: {bananaNoise:F6} distortion; clean noise {cleanNoise:F6}.");

		var selectedDistortion = MeasurePixelDistortion(before.Second, after.Second, appleRegion, out _);
		Assert.That(selectedDistortion, Is.LessThanOrEqualTo(allowedDistortion),
			$"Selected CollectionView item gained unexpected default Windows selection pixels: " +
			$"{selectedDistortion:F6} distortion across {appleSampledPixels} sampled pixels; " +
			$"clean noise {cleanNoise:F6}; allowed {allowedDistortion:F6}.");
	}

	static Rectangle GetSampleRegion(Rectangle itemRect)
	{
		const int edgeInset = 2;
		Assert.That(itemRect.Width, Is.GreaterThan(edgeInset * 4));
		Assert.That(itemRect.Height, Is.GreaterThan(edgeInset * 2));
		return new Rectangle(
			itemRect.X + edgeInset,
			itemRect.Y + edgeInset,
			itemRect.Width - (edgeInset * 2),
			itemRect.Height - (edgeInset * 2));
	}

	(byte[] First, byte[] Second) CaptureSettledScreenshots(
		Rectangle firstRegion,
		Rectangle secondRegion,
		out double settledNoise,
		out long firstSampledPixels)
	{
		const int maximumAttempts = 6;
		const double maximumSettledDistortion = 0.0001;
		var first = App.Screenshot();
		var second = first;
		settledNoise = double.MaxValue;
		firstSampledPixels = 0;

		for (var attempt = 0; attempt < maximumAttempts; attempt++)
		{
			second = App.Screenshot();
			var firstNoise = MeasurePixelDistortion(first, second, firstRegion, out firstSampledPixels);
			var secondNoise = MeasurePixelDistortion(first, second, secondRegion, out _);
			settledNoise = Math.Max(firstNoise, secondNoise);
			if (settledNoise <= maximumSettledDistortion)
				return (first, second);

			first = second;
		}

		Assert.Fail($"The rendered rows did not settle after {maximumAttempts} bounded captures: {settledNoise:F6} distortion.");
		return (first, second);
	}

	static double MeasurePixelDistortion(byte[] firstPng, byte[] secondPng, Rectangle region, out long sampledPixels)
	{
		using var first = new MagickImage(firstPng);
		using var second = new MagickImage(secondPng);
		Assert.That(second.Width, Is.EqualTo(first.Width));
		Assert.That(second.Height, Is.EqualTo(first.Height));
		Assert.That(region.Left, Is.GreaterThanOrEqualTo(0));
		Assert.That(region.Top, Is.GreaterThanOrEqualTo(0));
		Assert.That(region.Right, Is.LessThanOrEqualTo((int)first.Width));
		Assert.That(region.Bottom, Is.LessThanOrEqualTo((int)first.Height));

		var geometry = new MagickGeometry(region.X, region.Y, (uint)region.Width, (uint)region.Height);
		first.Crop(geometry);
		second.Crop(geometry);
		sampledPixels = (long)region.Width * region.Height;
		return first.Compare(second, ErrorMetric.RootMeanSquared, Channels.All);
	}
}
#endif

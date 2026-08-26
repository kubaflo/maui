#if WINDOWS
using System.Diagnostics;
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;
using VisualTestUtils;
using VisualTestUtils.MagickNet;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling by default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DefaultSelectionDoesNotAddNativeSelectionChrome()
	{
		var collection = App.WaitForElement("IssueCollectionView");
		var apple = App.WaitForElement("Apple");
		var banana = App.WaitForElement("Banana");

		var appleText = apple.GetText();
		var bananaText = banana.GetText();
		Assert.That(appleText, Is.Not.Null);
		Assert.That(bananaText, Is.Not.Null);
		if (appleText is null || bananaText is null)
			Assert.Fail("CollectionView item text was unavailable.");
		Assert.That(appleText, Is.EqualTo("Apple"));
		Assert.That(bananaText, Is.EqualTo("Banana"));

		var collectionFrame = collection.GetRect();
		var appleFrame = apple.GetRect();
		var bananaFrame = banana.GetRect();
		Assert.That(collectionFrame.Width, Is.GreaterThan(0));
		Assert.That(collectionFrame.Height, Is.GreaterThan(0));
		Assert.That(appleFrame.Width, Is.GreaterThan(0));
		Assert.That(appleFrame.Height, Is.GreaterThan(0));
		Assert.That(bananaFrame.Width, Is.GreaterThan(0));
		Assert.That(bananaFrame.Height, Is.GreaterThan(0));
		Assert.That(collectionFrame.Contains(appleFrame), Is.True);
		Assert.That(collectionFrame.Contains(bananaFrame), Is.True);

		var initialScreenshot = App.Screenshot();
		var imageSize = GetImageSize(initialScreenshot);
		var appleSample = GetTextFreeSample(appleFrame);
		var bananaSample = GetTextFreeSample(bananaFrame);
		AssertSampleIsValid(appleSample, appleFrame, imageSize);
		AssertSampleIsValid(bananaSample, bananaFrame, imageSize);

		var appleBefore = Crop(initialScreenshot, appleSample);
		var bananaBefore = Crop(initialScreenshot, bananaSample);
		Assert.That(GetDifferenceRatio(appleBefore, appleBefore), Is.Zero);
		Assert.That(GetDifferenceRatio(bananaBefore, bananaBefore), Is.Zero);
		Assert.That(
			GetDifferenceRatio(appleBefore, bananaBefore),
			Is.LessThanOrEqualTo(0.001),
			"Initial Apple and Banana sampling regions must contain only the common unselected background.");

		apple.Click();
		Assert.That(
			App.WaitForTextToBePresentInElement("SelectionState", "Selection received: Apple", TimeSpan.FromSeconds(10)),
			Is.True);
		var selectionState = App.WaitForElement("SelectionState").GetText();
		Assert.That(selectionState, Is.Not.Null);
		if (selectionState is null)
			Assert.Fail("SelectionChanged marker text was unavailable.");
		Assert.That(selectionState, Is.EqualTo("Selection received: Apple"));

		var settledScreenshot = WaitForStableRendering(appleSample, bananaSample);
		var bananaAfter = Crop(settledScreenshot, bananaSample);
		var bananaDifferenceRatio = GetDifferenceRatio(bananaBefore, bananaAfter);
		Assert.That(
			bananaDifferenceRatio,
			Is.LessThanOrEqualTo(0.001),
			$"Unselected Banana control changed unexpectedly: {bananaDifferenceRatio:P4} difference.");

		var appleAfter = Crop(settledScreenshot, appleSample);
		var appleDifferenceRatio = GetDifferenceRatio(appleBefore, appleAfter);
		var sampledPixels = appleSample.Width * appleSample.Height;
		Assert.That(
			appleDifferenceRatio,
			Is.Zero,
			$"Selected Apple acquired unexpected default WinUI selection chrome; {appleDifferenceRatio:P4} difference across {sampledPixels} sampled pixels (tolerance 0).");
	}

	byte[] WaitForStableRendering(Rectangle appleSample, Rectangle bananaSample)
	{
		var stopwatch = Stopwatch.StartNew();
		var previous = App.Screenshot();

		while (stopwatch.Elapsed < TimeSpan.FromSeconds(2))
		{
			var current = App.Screenshot();
			var appleIsStable = GetDifferenceRatio(Crop(previous, appleSample), Crop(current, appleSample)) == 0;
			var bananaIsStable = GetDifferenceRatio(Crop(previous, bananaSample), Crop(current, bananaSample)) == 0;
			if (appleIsStable && bananaIsStable)
				return current;

			previous = current;
		}

		Assert.Fail("CollectionView rendering did not settle within the bounded capture interval.");
		return previous;
	}

	static Rectangle GetTextFreeSample(Rectangle itemFrame)
	{
		var inset = Math.Min(8, itemFrame.Height / 4);
		var left = itemFrame.X + (itemFrame.Width * 2 / 3);
		return new Rectangle(
			left,
			itemFrame.Y + inset,
			itemFrame.Right - left - inset,
			itemFrame.Height - (2 * inset));
	}

	static void AssertSampleIsValid(Rectangle sample, Rectangle itemFrame, (int width, int height) imageSize)
	{
		Assert.That(sample.Width, Is.GreaterThanOrEqualTo(20));
		Assert.That(sample.Height, Is.GreaterThan(0));
		Assert.That(sample.X, Is.GreaterThan(itemFrame.X + 40));
		Assert.That(sample.X, Is.GreaterThanOrEqualTo(0));
		Assert.That(sample.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(sample.Right, Is.LessThanOrEqualTo(imageSize.width));
		Assert.That(sample.Bottom, Is.LessThanOrEqualTo(imageSize.height));
	}

	static (int width, int height) GetImageSize(byte[] screenshot)
	{
		var image = new ImageSnapshot(screenshot, ImageSnapshotFormat.PNG);
		return new MagickNetImageEditorFactory().CreateImageEditor(image).GetSize();
	}

	static ImageSnapshot Crop(byte[] screenshot, Rectangle sample)
	{
		var image = new ImageSnapshot(screenshot, ImageSnapshotFormat.PNG);
		var editor = new MagickNetImageEditorFactory().CreateImageEditor(image);
		editor.Crop(sample.X, sample.Y, sample.Width, sample.Height);
		return editor.GetUpdatedImage();
	}

	static double GetDifferenceRatio(ImageSnapshot before, ImageSnapshot after)
	{
		using var beforeImage = new MagickImage(before.Data);
		using var afterImage = new MagickImage(after.Data);
		return beforeImage.Compare(afterImage, ErrorMetric.RootMeanSquared, Channels.Red);
	}
}
#endif

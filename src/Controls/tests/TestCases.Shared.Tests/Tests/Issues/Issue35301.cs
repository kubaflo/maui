#if WINDOWS
using System;
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
	public void SelectedItemDoesNotAcquireRoundedWinUIStyling()
	{
		var ready = App.WaitForTextToBePresentInElement(
			"Issue35301Status",
			"Ready|Generation=-1|Item=<none>",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(ready, Is.True, "The CollectionView did not reach its initial state.");

		var appleQuery = AppiumQuery.ByXPath("//*[@Name='Apple']");
		var appleElement = App.WaitForElement(appleQuery);
		if (appleElement is null)
			throw new AssertionException("The realized Apple item was not available for the pointer click.");

		var itemBounds = appleElement.GetRect();
		Assert.That(itemBounds.Width, Is.GreaterThan(0), "The realized Apple item had no width.");
		Assert.That(itemBounds.Height, Is.GreaterThan(0), "The realized Apple item had no height.");

		var beforeSelection = App.Screenshot();
		App.Tap(appleQuery);

		var selected = App.WaitForTextToBePresentInElement(
			"Issue35301Status",
			"Selected|Generation=0|Item=Apple",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(selected, Is.True, "SelectionChanged did not report Apple after the Appium click.");

		var afterSelection = App.Screenshot();
		using var beforeItem = CropToItem(beforeSelection, itemBounds);
		using var afterItem = CropToItem(afterSelection, itemBounds);
		var difference = beforeItem.Compare(afterItem, ErrorMetric.RootMeanSquared, Channels.All);

		Assert.That(
			difference,
			Is.LessThanOrEqualTo(0.001),
			$"Issue35301 selected item must not acquire WinUI rounded selection styling; rendered pixel difference={difference:0.######}");
	}

	static MagickImage CropToItem(byte[] screenshot, System.Drawing.Rectangle bounds)
	{
		var image = new MagickImage(screenshot);
		var imageWidth = image.Width;
		var imageHeight = image.Height;
		if (bounds.X < 0 ||
			bounds.Y < 0 ||
			bounds.Right > imageWidth ||
			bounds.Bottom > imageHeight)
		{
			image.Dispose();
			throw new AssertionException(
				$"The Apple bounds ({bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}) were outside the screenshot ({imageWidth},{imageHeight}).");
		}

		image.Crop(new MagickGeometry(bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height));
		image.ResetPage();
		return image;
	}
}
#endif

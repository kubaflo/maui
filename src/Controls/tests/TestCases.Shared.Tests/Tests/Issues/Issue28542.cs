#if ANDROID
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28542 : _IssuesUITest
{
	public override string Issue => "CollectionView scrollbar thumb changes size for variable-height items";

	public Issue28542(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ScrollbarThumbKeepsContentBasedHeightForVariableHeightItems()
	{
		App.SetOrientationPortrait();
		var collectionRect = App.WaitForElement("Issue28542Collection").GetRect();

		using (var screenshot = new MagickImage(App.Screenshot()))
			Assert.That(screenshot.Height, Is.GreaterThan(screenshot.Width), "Issue 28542 requires a portrait window.");

		var firstShortItemRect = App.WaitForElement("ShortItem01").GetRect();
		Assert.That(firstShortItemRect.Y, Is.GreaterThanOrEqualTo(collectionRect.Y), "The first short item was not inside the CollectionView viewport.");

		DragUp(collectionRect, 0.6f);
		using var shortItemsScreenshot = new MagickImage(App.Screenshot());
		var shortItemsThumbHeight = MeasureScrollbarThumb(shortItemsScreenshot, collectionRect);
		Assert.That(shortItemsThumbHeight, Is.GreaterThan(0), "The scrollbar thumb was not rendered while short items were visible.");

		DragUp(collectionRect, 0.6f);
		DragUp(collectionRect, 0.6f);
		using var tallItemsScreenshot = new MagickImage(App.Screenshot());

		var scrollState = App.FindElement("Issue28542ScrollState").GetText();
		if (scrollState is null)
			throw new AssertionException("The post-drag Scrolled callback state was null.");
		Assert.That(ParseLastVisibleIndex(scrollState), Is.GreaterThanOrEqualTo(12), "The post-drag Scrolled callback did not report a tall item.");

		var tallItemsThumbHeight = MeasureScrollbarThumb(tallItemsScreenshot, collectionRect);
		Assert.That(tallItemsThumbHeight, Is.GreaterThan(0), "The scrollbar thumb was not rendered while tall items were visible.");

		var tolerance = Math.Max(4, (int)Math.Ceiling(shortItemsScreenshot.Width / 180d));
		if (Math.Abs(tallItemsThumbHeight - shortItemsThumbHeight) > tolerance)
			throw new AssertionException("Issue 28542 scrollbar thumb height changed after scrolling from short items to tall items.");
	}

	void DragUp(System.Drawing.Rectangle rect, float proportion)
	{
		var centerX = rect.X + (rect.Width / 2f);
		var startY = rect.Y + (rect.Height * 0.75f);
		var travel = rect.Height * proportion;
		App.DragCoordinates(centerX, startY, centerX, startY - travel);
	}

	static int MeasureScrollbarThumb(MagickImage screenshot, System.Drawing.Rectangle rect)
	{
		using var pixels = screenshot.GetPixels();
		var longestRun = 0;
		var firstX = Math.Min((int)screenshot.Width - 2, rect.Right - 2);
		var lastX = Math.Max(rect.Left + 25, firstX - 14);
		var firstY = Math.Max(1, rect.Top + 1);
		var lastY = Math.Min((int)screenshot.Height - 2, rect.Bottom - 2);

		for (var x = firstX; x >= lastX; x--)
		{
			var run = 0;
			for (var y = firstY; y <= lastY; y++)
			{
				var edge = pixels.GetPixel(x, y).ToColor();
				var reference = pixels.GetPixel(x - 24, y).ToColor();
				if (edge is null || reference is null)
					throw new AssertionException("The screenshot did not provide pixel colors for the CollectionView scrollbar region.");

				var edgeLuminance = edge.R + edge.G + edge.B;
				var referenceLuminance = reference.R + reference.G + reference.B;
				var difference =
					Math.Abs(edge.R - reference.R) +
					Math.Abs(edge.G - reference.G) +
					Math.Abs(edge.B - reference.B);

				if (difference > 45 && edgeLuminance + 30 < referenceLuminance)
				{
					run++;
					longestRun = Math.Max(longestRun, run);
				}
				else
				{
					run = 0;
				}
			}
		}

		return longestRun < rect.Height / 2 ? longestRun : -1;
	}

	static int ParseLastVisibleIndex(string state)
	{
		const string marker = ";last=";
		var markerIndex = state.IndexOf(marker, StringComparison.Ordinal);
		if (markerIndex < 0 || !int.TryParse(state[(markerIndex + marker.Length)..], out var index))
			throw new AssertionException($"The Scrolled callback state was invalid: {state}");

		return index;
	}
}
#endif

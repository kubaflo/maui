#if WINDOWS
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35617 : _IssuesUITest
{
	public override string Issue => "Horizontal CollectionView delays rendering newly added items";

	public Issue35617(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RapidlyAddedItemsAreRenderedBeforeTheNextClick()
	{
		var collectionView = App.WaitForElement("Issue35617CollectionView");
		Assert.That(collectionView, Is.Not.Null);
		var collectionRect = collectionView.GetRect();
		Assert.That(collectionRect.Width, Is.GreaterThan(0));
		Assert.That(collectionRect.Height, Is.EqualTo(50).Within(10));

		var initialItem = App.WaitForElement("item: 0");
		Assert.That(initialItem, Is.Not.Null);
		var initialItemRect = initialItem.GetRect();
		Assert.That(initialItemRect.Width, Is.GreaterThan(0));
		Assert.That(initialItemRect.Height, Is.GreaterThan(0));
		Assert.That(initialItemRect.X, Is.GreaterThanOrEqualTo(collectionRect.X));
		Assert.That(initialItemRect.Y, Is.GreaterThanOrEqualTo(collectionRect.Y));
		Assert.That(initialItemRect.X + initialItemRect.Width,
			Is.LessThanOrEqualTo(collectionRect.X + collectionRect.Width));
		Assert.That(initialItemRect.Y + initialItemRect.Height,
			Is.LessThanOrEqualTo(collectionRect.Y + collectionRect.Height));

		var initialCallbackCount = App.WaitForElement("Issue35617CallbackCount");
		Assert.That(initialCallbackCount, Is.Not.Null);
		Assert.That(initialCallbackCount.GetText(), Is.EqualTo("-1"));

		var measurements = new List<(int Rendered, int Expected)>();
		for (int cycle = 0; cycle < 3; cycle++)
		{
			App.Tap("Issue35617AddButton");
			App.Tap("Issue35617AddButton");
			App.Tap("Issue35617AddButton");

			var itemCountText = App.FindElement("Issue35617ItemCount").GetText();
			Assert.That(
				int.TryParse(itemCountText, out var expectedItemCount),
				Is.True,
				$"The page did not report a numeric source item count: '{itemCountText}'.");

			int renderedItemCount = 0;
			for (int itemIndex = 0; itemIndex < expectedItemCount; itemIndex++)
			{
				foreach (var element in App.FindElementsByText($"item: {itemIndex}"))
				{
					var itemRect = element.GetRect();
					if (itemRect.Width > 0 &&
						itemRect.Height > 0 &&
						itemRect.X >= collectionRect.X &&
						itemRect.Y >= collectionRect.Y &&
						itemRect.X + itemRect.Width <= collectionRect.X + collectionRect.Width &&
						itemRect.Y + itemRect.Height <= collectionRect.Y + collectionRect.Height)
					{
						renderedItemCount++;
					}
				}
			}

			measurements.Add((renderedItemCount, expectedItemCount));
			App.Tap("Issue35617ResetButton");
		}

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35617CallbackCount", "9"),
			Is.True,
			"All nine Add callbacks should complete.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35617CycleCount", "3"),
			Is.True,
			"All three rapid-add cycles should complete.");

		Assert.That(
			measurements.All(measurement => measurement.Rendered == measurement.Expected),
			Is.True,
			$"Horizontal CollectionView left newly added item unrendered before the next rapid click. Rendered/source counts: {string.Join(", ", measurements)}");
	}
}
#endif

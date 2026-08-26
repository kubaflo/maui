#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27332 : _IssuesUITest
{
	public Issue27332(TestDevice device) : base(device)
	{
	}

	public override string Issue => "CollectionView footer is displayed at the bottom after clearing items";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FooterRemainsAdjacentToHeaderAfterClearingItems()
	{
		App.WaitForElement("HeaderRoot");
		App.WaitForElement("FooterRoot");
		App.WaitForElement("AddButton");
		App.WaitForElement("ClearButton");
		App.WaitForElement("ItemCountLabel");
		App.WaitForElement("LayoutGenerationLabel");

		App.Tap("AddButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ItemCountLabel", "Items: 2"),
			Is.True,
			"The CollectionView source should contain two items after Add.");

		var populatedHeader = App.WaitForElement("HeaderRoot").GetRect();
		var firstItem = App.WaitForElement("Item1Root").GetRect();
		var secondItem = App.WaitForElement("Item2Root").GetRect();
		var populatedFooter = App.WaitForElement("FooterRoot").GetRect();

		AssertNonEmpty(populatedHeader, "header");
		AssertNonEmpty(firstItem, "Item 1");
		AssertNonEmpty(secondItem, "Item 2");
		AssertNonEmpty(populatedFooter, "footer");
		Assert.That(firstItem.Y, Is.GreaterThanOrEqualTo(populatedHeader.Bottom), "Item 1 should be below the header.");
		Assert.That(secondItem.Y, Is.GreaterThanOrEqualTo(firstItem.Bottom), "Item 2 should be below Item 1.");
		Assert.That(populatedFooter.Y, Is.GreaterThanOrEqualTo(secondItem.Bottom), "The footer should be below Item 2.");
		Assert.That(firstItem.Y - populatedHeader.Bottom, Is.EqualTo(0).Within(2), "The populated header and Item 1 should have no layout gap.");
		Assert.That(secondItem.Y - firstItem.Bottom, Is.EqualTo(0).Within(2), "The populated items should have no layout gap.");

		App.Tap("ClearButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ItemCountLabel", "Items: 0"),
			Is.True,
			"The CollectionView source should be empty after Clear.");
		Assert.That(
			App.WaitForTextToBePresentInElement("LayoutGenerationLabel", "Layout generation: 1"),
			Is.True,
			"The native CollectionView should receive a post-clear LayoutUpdated callback.");

		var emptyHeader = App.WaitForElement("HeaderRoot").GetRect();
		var emptyFooter = App.WaitForElement("FooterRoot").GetRect();
		var viewport = App.WaitForElement("CollectionView").GetRect();

		AssertNonEmpty(emptyHeader, "empty header");
		AssertNonEmpty(emptyFooter, "empty footer");
		AssertNonEmpty(viewport, "CollectionView viewport");

		var gap = emptyFooter.Y - emptyHeader.Bottom;
		Assert.That(
			gap,
			Is.EqualTo(0).Within(2),
			$"Issue27332 footer should be adjacent to header after clearing all items. " +
			$"HeaderBottom={emptyHeader.Bottom}, FooterTop={emptyFooter.Y}, Gap={gap}, Tolerance=2, " +
			$"ViewportWidth={viewport.Width}, ViewportHeight={viewport.Height}.");
	}

	static void AssertNonEmpty(System.Drawing.Rectangle rectangle, string elementName)
	{
		Assert.That(rectangle.Width, Is.GreaterThan(0), $"{elementName} should have a positive native width.");
		Assert.That(rectangle.Height, Is.GreaterThan(0), $"{elementName} should have a positive native height.");
	}
}
#endif

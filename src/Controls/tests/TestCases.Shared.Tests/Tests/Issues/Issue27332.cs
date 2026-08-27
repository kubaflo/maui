#if WINDOWS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27332 : _IssuesUITest
{
	public Issue27332(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView footer is displayed at the bottom after items are added and cleared";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FooterRemainsAdjacentToHeaderAfterItemsAreCleared()
	{
		const int geometryTolerance = 2;
		var windowSize = ((AppiumWindowsApp)App).Driver.Manage().Window.Size;
		Assert.Multiple(() =>
		{
			Assert.That(windowSize.Width, Is.GreaterThan(windowSize.Height), "The active test window should preserve the reported landscape geometry.");
			Assert.That(windowSize.Width, Is.GreaterThanOrEqualTo(800), "The active test window should be wide enough to expose the reported layout.");
			Assert.That(windowSize.Height, Is.GreaterThanOrEqualTo(500), "The active test window should be tall enough to expose the reported layout.");
		});

		AssertResultText("Count: -1");
		var collectionRect = App.WaitForElement("Issue27332Collection").GetRect();
		var initialHeaderRect = App.WaitForElement("Issue27332Header").GetRect();
		var initialFooterRect = App.WaitForElement("Issue27332Footer").GetRect();
		AssertValidSectionGeometry(collectionRect, initialHeaderRect, initialFooterRect, geometryTolerance);

		App.Tap("Issue27332Add");
		AssertResultText("Count: 2");
		var populatedCollectionRect = App.WaitForElement("Issue27332Collection").GetRect();
		var item1Rect = App.WaitForElement("Item 1").GetRect();
		var item2Rect = App.WaitForElement("Item 2").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(item1Rect.Width, Is.GreaterThan(0), "Item 1 should be rendered with a positive width.");
			Assert.That(item1Rect.Height, Is.GreaterThan(0), "Item 1 should be rendered with a positive height.");
			Assert.That(item2Rect.Width, Is.GreaterThan(0), "Item 2 should be rendered with a positive width.");
			Assert.That(item2Rect.Height, Is.GreaterThan(0), "Item 2 should be rendered with a positive height.");
			Assert.That(item1Rect.Top, Is.GreaterThanOrEqualTo(populatedCollectionRect.Top), "Item 1 should be rendered inside the CollectionView.");
			Assert.That(item2Rect.Bottom, Is.LessThanOrEqualTo(populatedCollectionRect.Bottom), "Item 2 should be rendered inside the CollectionView.");
			Assert.That(item2Rect.Top, Is.GreaterThanOrEqualTo(item1Rect.Top), "Item 2 should be rendered after Item 1.");
		});

		App.Tap("Issue27332Clear");
		AssertResultText("Count: 0");
		App.WaitForNoElement("Item 1");
		App.WaitForNoElement("Item 2");

		var currentCollectionRect = App.WaitForElement("Issue27332Collection").GetRect();
		var headerRect = App.WaitForElement("Issue27332Header").GetRect();
		var footerRect = App.WaitForElement("Issue27332Footer").GetRect();
		AssertValidSectionGeometry(currentCollectionRect, headerRect, footerRect, geometryTolerance);

		var gap = footerRect.Top - headerRect.Bottom;
		Assert.That(gap, Is.EqualTo(0).Within(geometryTolerance),
			$"Issue27332 footer must be adjacent to header after Add 2 Items and Clear All Items; measured gap={gap}, header={headerRect}, footer={footerRect}, collection={currentCollectionRect}, tolerance={geometryTolerance}, expected=0.");
	}

	void AssertResultText(string expected)
	{
		App.RetryAssert(() =>
		{
			var actual = App.FindElement("Issue27332Result").GetText();
			if (actual is null)
				Assert.Fail("The item-count callback label should expose text.");

			Assert.That(actual, Is.EqualTo(expected));
		});
	}

	static void AssertValidSectionGeometry(Rectangle collectionRect, Rectangle headerRect, Rectangle footerRect, int geometryTolerance)
	{
		Assert.Multiple(() =>
		{
			Assert.That(collectionRect.Width, Is.GreaterThan(0), "The CollectionView should have a positive width.");
			Assert.That(collectionRect.Height, Is.GreaterThan(0), "The CollectionView should have a positive height.");
			Assert.That(headerRect.Width, Is.GreaterThan(0), "The identified header should have a positive width.");
			Assert.That(headerRect.Height, Is.GreaterThan(0), "The identified header should have a positive height.");
			Assert.That(footerRect.Width, Is.GreaterThan(0), "The identified footer should have a positive width.");
			Assert.That(footerRect.Height, Is.GreaterThan(0), "The identified footer should have a positive height.");
			Assert.That(headerRect.Left, Is.GreaterThanOrEqualTo(collectionRect.Left - geometryTolerance), "The header should be inside the CollectionView horizontally.");
			Assert.That(headerRect.Right, Is.LessThanOrEqualTo(collectionRect.Right + geometryTolerance), "The header should be inside the CollectionView horizontally.");
			Assert.That(headerRect.Top, Is.GreaterThanOrEqualTo(collectionRect.Top - geometryTolerance), "The header should be inside the CollectionView vertically.");
			Assert.That(headerRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom + geometryTolerance), "The header should be inside the CollectionView vertically.");
			Assert.That(footerRect.Left, Is.GreaterThanOrEqualTo(collectionRect.Left - geometryTolerance), "The footer should be inside the CollectionView horizontally.");
			Assert.That(footerRect.Right, Is.LessThanOrEqualTo(collectionRect.Right + geometryTolerance), "The footer should be inside the CollectionView horizontally.");
			Assert.That(footerRect.Top, Is.GreaterThanOrEqualTo(collectionRect.Top - geometryTolerance), "The footer should be inside the CollectionView vertically.");
			Assert.That(footerRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom + geometryTolerance), "The footer should be inside the CollectionView vertically.");
		});
	}
}
#endif

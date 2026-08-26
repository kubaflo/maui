#if WINDOWS

using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28006 : _IssuesUITest
{
	public Issue28006(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView scroll position changes when inserting an item";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void InsertingItemAboveViewportKeepsReferenceItemVisible()
	{
		App.WaitForElement("ScrollToMiddle");
		App.WaitForElement("AddItemAbove");
		var collectionRect = App.WaitForElement("ItemsCollection").GetRect();

		App.Tap("ScrollToMiddle");

		bool initialScrollCompleted = App.WaitForTextToBePresentInElement(
			"ScrollToken",
			"Token=0",
			TimeSpan.FromSeconds(10));
		Assert.That(initialScrollCompleted, Is.True, "The initial CollectionView scroll callback did not occur.");

		var initialFirstVisible = App.WaitForElement("FirstVisibleIndex").GetText();
		Assert.That(initialFirstVisible, Is.Not.Null);
		Assert.That(initialFirstVisible, Is.EqualTo("FirstVisible=10;Count=20"));

		var initialReferenceItems = App.FindElements("Item10");
		Assert.That(initialReferenceItems.Count, Is.EqualTo(1), "The reference item was not uniquely rendered before insertion.");
		var initialReferenceRect = initialReferenceItems.Single().GetRect();
		Assert.That(initialReferenceRect.Y, Is.EqualTo(collectionRect.Y).Within(2));

		var initialReferenceImages = App.FindElements("Item10Image");
		Assert.That(initialReferenceImages.Count, Is.EqualTo(1), "The bundled image for the reference item was not rendered.");

		App.Tap("AddItemAbove");

		bool insertionScrollCompleted = App.WaitForTextToBePresentInElement(
			"ScrollToken",
			"Token=1",
			TimeSpan.FromSeconds(10));
		Assert.That(insertionScrollCompleted, Is.True, "The post-insertion CollectionView scroll callback did not occur.");

		var postInsertStatus = App.WaitForElement("FirstVisibleIndex").GetText();
		Assert.That(postInsertStatus, Is.Not.Null);
		var postInsertReferenceItems = App.FindElements("Item10");
		string referenceFrame = postInsertReferenceItems.Count == 1
			? postInsertReferenceItems.Single().GetRect().ToString()
			: "<not visible>";
		string failureDetails =
			$"CollectionView reset to top after insertion: expected firstVisible=11/count=21, actual={postInsertStatus}, " +
			$"visible Item10 count={postInsertReferenceItems.Count}, collectionFrame={collectionRect}, itemFrame={referenceFrame}.";

		Assert.That(postInsertStatus, Is.EqualTo("FirstVisible=11;Count=21"), failureDetails);
		Assert.That(postInsertReferenceItems.Count, Is.EqualTo(1), failureDetails);

		var postInsertReferenceRect = postInsertReferenceItems.Single().GetRect();
		Assert.That(postInsertReferenceRect.Y, Is.EqualTo(collectionRect.Y).Within(2), failureDetails);
	}
}

#endif

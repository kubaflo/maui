#if IOS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue19064 : _IssuesUITest
{
	const string CollectionViewId = "Issue19064CollectionView";
	const string CheckButtonId = "Issue19064CheckButton";
	const string CheckResultId = "Issue19064CheckResult";
	const string FirstImageId = "Issue19064Item0Image";
	const string ScrollStateId = "Issue19064ScrollState";
	const double SizeTolerance = 1;

	public override string Issue => "ItemSizingStrategy gallery displays items inconsistently";

	public Issue19064(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void MeasureFirstItemImageRemainsVisibleAfterScrollingAwayAndBackTwice()
	{
		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The reported scenario should run in portrait.");
		Assert.That(
			App.WaitForElement(ScrollStateId).GetText(),
			Is.EqualTo("START"),
			"The Scrolled callback should begin in the pre-trigger sentinel state.");

		var collectionRect = App.WaitForElement(CollectionViewId).GetRect();
		var initialImageRect = App.WaitForElement(FirstImageId).GetRect();
		AssertImageIsVisible(initialImageRect, collectionRect);
		Assert.That(initialImageRect.Width, Is.EqualTo(100).Within(SizeTolerance), "Item 0 image should initially be 100 points wide.");
		Assert.That(initialImageRect.Height, Is.EqualTo(50).Within(SizeTolerance), "Item 0 image should initially be 50 points high.");

		ScrollAwayAndReturn(windowRect, collectionRect, 1);
		ScrollAwayAndReturn(windowRect, collectionRect, 2);
		App.Click(CheckButtonId);
		App.RetryAssert(() =>
			Assert.That(App.FindElement(CheckResultId).GetText(), Is.EqualTo("RETURNS:2"), "The check action should observe both return cycles."));

		App.RetryAssert(() =>
		{
			var returnedImageRect = App.WaitForElement(FirstImageId).GetRect();
			var returnedCollectionRect = App.FindElement(CollectionViewId).GetRect();
			Assert.That(returnedImageRect.Width, Is.GreaterThan(0), "The item-bound native image should have a positive width.");
			Assert.That(returnedImageRect.Height, Is.GreaterThan(0), "The item-bound native image should have a positive height.");
			var visibleRect = Rectangle.Intersect(returnedImageRect, returnedCollectionRect);
			Assert.That(
				visibleRect.Width > 0 && visibleRect.Height > 0,
				Is.True,
				$"Item 0 image should remain visible after two right-and-left scroll cycles; observed native rect x={returnedImageRect.X}, y={returnedImageRect.Y}, width={returnedImageRect.Width}, height={returnedImageRect.Height}; CollectionView rect x={returnedCollectionRect.X}, y={returnedCollectionRect.Y}, width={returnedCollectionRect.Width}, height={returnedCollectionRect.Height}");
		});
	}

	void ScrollAwayAndReturn(Rectangle windowRect, Rectangle collectionRect, int expectedReturnCount)
	{
		DragHorizontally(windowRect, collectionRect, towardLaterItems: true);
		DragHorizontally(windowRect, collectionRect, towardLaterItems: true);
		App.RetryAssert(() =>
		{
			var state = App.FindElement(ScrollStateId).GetText()!;
			Assert.That(ParseAwayIndex(state), Is.GreaterThanOrEqualTo(2), "The Scrolled callback should report FirstVisibleItemIndex >= 2.");
		});

		DragHorizontally(windowRect, collectionRect, towardLaterItems: false);
		DragHorizontally(windowRect, collectionRect, towardLaterItems: false);
		DragHorizontally(windowRect, collectionRect, towardLaterItems: false);
		App.RetryAssert(() =>
		{
			Assert.That(
				App.FindElement(ScrollStateId).GetText(),
				Is.EqualTo($"RETURNED:0:{expectedReturnCount}"),
				$"The Scrolled callback should report return cycle {expectedReturnCount} at FirstVisibleItemIndex 0.");
		});
	}

	void DragHorizontally(Rectangle windowRect, Rectangle collectionRect, bool towardLaterItems)
	{
		var left = windowRect.X + (int)(windowRect.Width * 0.2);
		var right = windowRect.X + (int)(windowRect.Width * 0.8);
		var y = collectionRect.Y + collectionRect.Height / 2;

		Assert.That(y, Is.InRange(collectionRect.Top, collectionRect.Bottom), "The drag coordinate should be inside the CollectionView.");
		Assert.That(left, Is.InRange(collectionRect.Left, collectionRect.Right), "The left drag coordinate should be inside the CollectionView.");
		Assert.That(right, Is.InRange(collectionRect.Left, collectionRect.Right), "The right drag coordinate should be inside the CollectionView.");

		App.DragCoordinates(
			towardLaterItems ? right : left,
			y,
			towardLaterItems ? left : right,
			y);
	}

	static int ParseAwayIndex(string state)
	{
		const string prefix = "AWAY:";
		Assert.That(state, Does.StartWith(prefix), "The Scrolled callback should report the away state.");
		Assert.That(int.TryParse(state[prefix.Length..], out var index), Is.True, "The away state should contain FirstVisibleItemIndex.");
		return index;
	}

	static void AssertImageIsVisible(Rectangle imageRect, Rectangle collectionRect)
	{
		var visibleRect = Rectangle.Intersect(imageRect, collectionRect);
		Assert.That(visibleRect.Width, Is.GreaterThan(0), "Item 0 image should intersect the CollectionView viewport horizontally.");
		Assert.That(visibleRect.Height, Is.GreaterThan(0), "Item 0 image should intersect the CollectionView viewport vertically.");
	}
}
#endif

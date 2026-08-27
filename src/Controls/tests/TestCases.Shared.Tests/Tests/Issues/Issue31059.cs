#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31059 : _IssuesUITest
{
	public Issue31059(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView changes the centered item when rotating to landscape";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void LastItemRemainsCenteredAfterRotatingToLandscape()
	{
		const string collectionId = "Issue31059Collection";
		const string orientationStateId = "Issue31059OrientationState";
		const string lastItemId = "Image 4";
		const string failureSignature = "Issue31059 expected the center index to remain 4 after portrait-to-landscape rotation.";

		App.SetOrientationPortrait();

		var windowElement = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
		if (windowElement is null)
			throw new AssertionException("Issue31059 requires the native iOS window.");

		var portraitWindow = windowElement.GetRect();
		Assert.That(portraitWindow.Height, Is.GreaterThan(portraitWindow.Width),
			$"Issue31059 requires portrait geometry before the trigger, but the window was {portraitWindow}.");

		var collectionElement = App.WaitForElement(collectionId);
		if (collectionElement is null)
			throw new AssertionException("Issue31059 requires the CollectionView.");

		var collectionRect = collectionElement.GetRect();
		var dragStartX = (float)(portraitWindow.X + (portraitWindow.Width * 0.85));
		var dragEndX = (float)(portraitWindow.X + (portraitWindow.Width * 0.20));
		var dragY = (float)(collectionRect.Y + (collectionRect.Height / 2));

		for (var expectedIndex = 1; expectedIndex <= 4; expectedIndex++)
		{
			App.DragCoordinates(dragStartX, dragY, dragEndX, dragY);
			var capturedIndex = expectedIndex;
			App.RetryAssert(() =>
			{
				var state = ReadOrientationState(orientationStateId);
				Assert.That(state.CenterIndex, Is.EqualTo(capturedIndex),
					$"Issue31059 drag {capturedIndex} should center item {capturedIndex}, but state was generation {state.Generation}, index {state.CenterIndex}.");
			});
		}

		var portraitState = ReadOrientationState(orientationStateId);
		Assert.That(portraitState.CenterIndex, Is.EqualTo(4),
			$"Issue31059 requires item 4 before rotation, but the center index was {portraitState.CenterIndex}.");

		var portraitItemElement = App.WaitForElement(lastItemId);
		if (portraitItemElement is null)
			throw new AssertionException("Issue31059 requires Image 4 to be rendered before rotation.");

		var portraitItem = portraitItemElement.GetRect();
		AssertItemIsVisible(portraitItem, collectionRect, portraitState);

		App.SetOrientationLandscape();

		App.RetryAssert(() =>
		{
			var landscapeWindowElement = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
			if (landscapeWindowElement is null)
				throw new AssertionException("Issue31059 requires the native iOS window after rotation.");

			var landscapeWindow = landscapeWindowElement.GetRect();
			var state = ReadOrientationState(orientationStateId);
			Assert.Multiple(() =>
			{
				Assert.That(landscapeWindow.Width, Is.GreaterThan(landscapeWindow.Height),
					$"Issue31059 expected landscape root-window geometry, but the window was {landscapeWindow}.");
				Assert.That(state.Generation, Is.GreaterThan(portraitState.Generation),
					$"Issue31059 expected SizeChanged generation to advance beyond {portraitState.Generation}, but it was {state.Generation}.");
			});
		});

		var landscapeState = ReadOrientationState(orientationStateId);
		Assert.That(landscapeState.CenterIndex, Is.EqualTo(4),
			$"{failureSignature} Observed index={landscapeState.CenterIndex}.");
	}

	(int Generation, int CenterIndex) ReadOrientationState(string orientationStateId)
	{
		var stateElement = App.WaitForElement(orientationStateId);
		if (stateElement is null)
			throw new AssertionException("Issue31059 requires the orientation state element.");

		var stateText = stateElement.GetText();
		if (stateText is null)
			throw new AssertionException("Issue31059 requires non-null orientation state text.");

		var parts = stateText.Split(':');
		Assert.That(parts, Has.Length.EqualTo(2), $"Issue31059 orientation state was malformed: '{stateText}'.");
		return (int.Parse(parts[0]), int.Parse(parts[1]));
	}

	static void AssertItemIsVisible(
		System.Drawing.Rectangle item,
		System.Drawing.Rectangle viewport,
		(int Generation, int CenterIndex) state)
	{
		var measurements = $"Issue31059 portrait precondition: index={state.CenterIndex}, generation={state.Generation}, item={item}, viewport={viewport}";

		Assert.Multiple(() =>
		{
			Assert.That(item.Width, Is.EqualTo(320).Within(2), measurements);
			Assert.That(item.Height, Is.EqualTo(320).Within(2), measurements);
			Assert.That(item.Right, Is.GreaterThan(viewport.Left), measurements);
			Assert.That(item.Left, Is.LessThan(viewport.Right), measurements);
			Assert.That(item.Bottom, Is.GreaterThan(viewport.Top), measurements);
			Assert.That(item.Top, Is.LessThan(viewport.Bottom), measurements);
			Assert.That(state.CenterIndex, Is.EqualTo(4), measurements);
		});
	}
}
#endif

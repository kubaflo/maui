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

	public override string Issue => "CollectionView center changes after portrait-to-landscape rotation";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RotationPreservesCenteredItem()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue31059CollectionView");

		App.RetryAssert(() =>
		{
			var transitionState = App.FindElement("Issue31059TransitionStateLabel").GetText();
			Assert.That(transitionState, Does.Contain("Orientation:Portrait;Transition:-1;Stable:0;Size:"));
		});

		Assert.That(
			App.FindElement("Issue31059ScenarioLabel").GetText(),
			Is.EqualTo("Items: Item 1, Item 2, Item 3; Size: 300x320; Snap: MandatorySingle Center"));
		Assert.That(
			App.FindElement("Issue31059CurrentItemLabel").GetText(),
			Is.EqualTo("Current item: Item 1"));
		App.WaitForElement("Item 1");

		var collectionRect = App.WaitForElement("Issue31059CollectionView").GetRect();
		var centerY = collectionRect.Top + (collectionRect.Height / 2);
		var dragStartX = collectionRect.Left + (collectionRect.Width * 0.75f);
		var dragEndX = collectionRect.Left + (collectionRect.Width * 0.25f);

		App.DragCoordinates(dragStartX, centerY, dragEndX, centerY);
		App.RetryAssert(() =>
		{
			Assert.That(
				App.FindElement("Issue31059CurrentItemLabel").GetText(),
				Is.EqualTo("Current item: Item 2"));
		});
		App.WaitForElement("Item 2");

		App.DragCoordinates(dragStartX, centerY, dragEndX, centerY);
		App.DragCoordinates(dragStartX, centerY, dragEndX, centerY);
		App.RetryAssert(() =>
		{
			Assert.That(
				App.FindElement("Issue31059CurrentItemLabel").GetText(),
				Is.EqualTo("Current item: Item 3"));
		});
		App.WaitForElement("Item 3");

		var centeredItemBeforeRotation = App.FindElement("Issue31059CurrentItemLabel").GetText();
		App.SetOrientationLandscape();

		App.RetryAssert(() =>
		{
			var transitionState = App.FindElement("Issue31059TransitionStateLabel").GetText();
			Assert.That(transitionState, Does.Contain("Orientation:Landscape;Transition:"));
			Assert.That(transitionState, Does.Contain(";Stable:2;Size:"));
		});

		var centeredItemAfterRotation = App.FindElement("Issue31059CurrentItemLabel").GetText();
		Assert.That(
			centeredItemAfterRotation,
			Is.EqualTo("Current item: Item 3"),
			$"CollectionView center changed after portrait-to-landscape rotation: before={centeredItemBeforeRotation}; after={centeredItemAfterRotation}; expected=Current item: Item 3");
	}
}
#endif

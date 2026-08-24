#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35889 : _IssuesUITest
{
	public Issue35889(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Empty CollectionView has incorrect height on iOS";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void EmptyCollectionViewInAutoGridRowHasZeroNativeHeight()
	{
		App.SetOrientationPortrait();

		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		App.RetryAssert(() =>
		{
			windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
			Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width));
		});

		var lifecycleStatus = App.WaitForElement("LifecycleStatus");
		Assert.That(lifecycleStatus, Is.Not.Null);
		var initialStatus = lifecycleStatus.GetText();
		Assert.That(initialStatus, Is.EqualTo("UNTRIGGERED"));
		App.WaitForElement("ShowScenario");
		App.Tap("ShowScenario");

		App.RetryAssert(() =>
		{
			var loadedStatus = App.WaitForElement("LifecycleStatus");
			Assert.That(loadedStatus, Is.Not.Null);
			Assert.That(loadedStatus.GetText(), Is.EqualTo("LOADED"));
		});

		var collectionElement = App.WaitForElement("EmptyCollectionView");
		var beforeElement = App.WaitForElement("BeforeCollectionLabel");
		var afterElement = App.WaitForElement("AfterCollectionLabel");
		Assert.That(collectionElement, Is.Not.Null);
		Assert.That(beforeElement, Is.Not.Null);
		Assert.That(afterElement, Is.Not.Null);

		Assert.That(beforeElement.GetText(), Is.EqualTo("before collectionview"));
		Assert.That(afterElement.GetText(), Is.EqualTo("after collectionview"));

		var beforeRect = beforeElement.GetRect();
		var afterRect = afterElement.GetRect();
		var collectionRect = collectionElement.GetRect();

		Assert.That(beforeRect.Width, Is.GreaterThan(0));
		Assert.That(beforeRect.Height, Is.GreaterThan(0));
		Assert.That(afterRect.Width, Is.GreaterThan(0));
		Assert.That(afterRect.Height, Is.GreaterThan(0));
		Assert.That(beforeRect.Y, Is.GreaterThanOrEqualTo(windowRect.Y));
		Assert.That(afterRect.Y, Is.GreaterThanOrEqualTo(beforeRect.Y + beforeRect.Height));
		Assert.That(collectionRect.Width, Is.GreaterThan(0));
		Assert.That(collectionRect.X, Is.GreaterThanOrEqualTo(windowRect.X));
		Assert.That(collectionRect.Y, Is.GreaterThanOrEqualTo(windowRect.Y));
		Assert.That(collectionRect.X + collectionRect.Width, Is.LessThanOrEqualTo(windowRect.X + windowRect.Width));
		Assert.That(collectionRect.Y, Is.GreaterThanOrEqualTo(beforeRect.Y + beforeRect.Height));
		Assert.That(afterRect.Y, Is.GreaterThanOrEqualTo(collectionRect.Y));
		Assert.That(collectionRect.Height, Is.EqualTo(0).Within(1),
			$"Empty CollectionView native height was {collectionRect.Height}, expected 0 +/- 1 after the content-replacement layout completed.");
	}
}
#endif

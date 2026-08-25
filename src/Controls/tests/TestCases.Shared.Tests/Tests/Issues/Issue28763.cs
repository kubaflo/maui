#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28763 : _IssuesUITest
{
	public override string Issue => "Multiple notifications for SelectionChanged in a CollectionView when reusing a singleton view model";

	public Issue28763(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectionChangedCommandRunsOnceAfterReturningToDetailPage()
	{
		App.WaitForElement("Task 1");
		App.Tap("Task 1");
		App.WaitForElement("Detail visit 1: Detail items for Task 1; Notifications: 0");

		App.Tap("Item A");
		App.WaitForElement("Command: observed");
		var firstStateElement = App.WaitForElement("DetailState");
		if (firstStateElement is null)
		{
			Assert.Fail("The first detail state was not available.");
			return;
		}

		var firstState = firstStateElement.GetText();
		if (firstState is null)
		{
			Assert.Fail("The first detail state had no text.");
			return;
		}

		Assert.That(firstState, Does.EndWith("Notifications: 1"),
			"The first detail-page selection should invoke SelectionChangedCommand exactly once.");

		App.TapBackArrow();
		App.WaitForElement("Task 2");
		App.Tap("Task 2");
		App.WaitForElement("Detail visit 2: Detail items for Task 2; Notifications: 0");

		App.Tap("Item B");
		App.WaitForElement("Command: observed");
		var secondStateElement = App.WaitForElement("DetailState");
		if (secondStateElement is null)
		{
			Assert.Fail("The second detail state was not available.");
			return;
		}

		var secondState = secondStateElement.GetText();
		if (secondState is null)
		{
			Assert.Fail("The second detail state had no text.");
			return;
		}

		const string notificationMarker = "Notifications: ";
		var markerIndex = secondState.LastIndexOf(notificationMarker, StringComparison.Ordinal);
		Assert.That(markerIndex, Is.GreaterThanOrEqualTo(0), "The second detail state did not report a notification count.");
		var notificationText = secondState[(markerIndex + notificationMarker.Length)..];
		Assert.That(int.TryParse(notificationText, out var actualCount), Is.True,
			"The second detail state did not report a numeric notification count.");
		Assert.That(actualCount, Is.EqualTo(1),
			$"SelectionChangedCommand should run once after the second detail-page selection; observed count was {actualCount}.");
	}
}
#endif

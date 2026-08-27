#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue17877 : _IssuesUITest
{
	public override string Issue => "TabbedPage does not notify when the current tab is reselected";

	public Issue17877(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void ReselectingCurrentBottomTabRaisesManagedNotification()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue17877ReadyStatus",
				"Bottom tabs loaded: 1",
				TimeSpan.FromSeconds(15)),
			Is.True,
			"The pushed bottom TabbedPage must finish loading before the touch sequence starts.");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue17877ReselectionStatus",
				"CurrentPageReselected count: 0"),
			Is.True,
			"The reselection callback sentinel must be initialized before the trigger.");

		App.WaitForElement("Tab 2");
		App.Tap("Tab 2");
		App.WaitForElement("Issue17877TabTwoContent");

		App.WaitForElement("Tab 1");
		App.Tap("Tab 1");
		App.WaitForElement("Issue17877TabOneContent");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue17877PageChangeStatus",
				"CurrentPageChanged count: 2"),
			Is.True,
			"Selecting Tab 2 and returning to Tab 1 must complete both ordinary tab transitions.");

		App.Tap("Tab 1");
		App.WaitForTextToBePresentInElement(
			"Issue17877ReselectionStatus",
			"CurrentPageReselected count: 1",
			TimeSpan.FromSeconds(3));

		var countElement = App.FindElement("Issue17877ReselectionStatus");
		if (countElement is null)
			throw new AssertionException("The managed reselection status element was not found after touching the current tab.");

		var actualCount = countElement.GetText();
		Assert.That(
			actualCount,
			Is.EqualTo("CurrentPageReselected count: 1"),
			$"Issue 17877 reselection notification mismatch: expected one managed reselection callback after touching the current tab, but observed '{actualCount ?? "<null>"}'.");
	}
}
#endif

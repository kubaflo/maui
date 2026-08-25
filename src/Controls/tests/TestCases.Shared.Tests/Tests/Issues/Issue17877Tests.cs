#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue17877Tests : _IssuesUITest
{
	public Issue17877Tests(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "TabbedPage does not notify when the current tab is reselected";

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void ReselectingCurrentBottomTabRaisesManagedNotification()
	{
		App.WaitForElement("Issue17877TabOneContent");

		App.Tap(AppiumQuery.ByAccessibilityId("Tab 2"));
		App.WaitForElement("Issue17877TabTwoContent");

		App.Tap(AppiumQuery.ByAccessibilityId("Tab 1"));
		App.WaitForElement("Issue17877TabOneContent");

		App.Tap("Issue17877Arm");
		App.Tap(AppiumQuery.ByAccessibilityId("Tab 1"));
		App.Tap("Issue17877Check");

		var result = App.WaitForElement("Issue17877Result").GetText();
		Assert.That(
			result,
			Is.EqualTo("Reselection notified at count 1"),
			"CurrentPageReselected did not notify after reselecting current Tab 1");
	}
}
#endif

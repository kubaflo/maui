#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue17877 : _IssuesUITest
{
	public Issue17877(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "TabbedPage does not trigger when reselected current tab";

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void ReselectingCurrentBottomTabRaisesManagedNotification()
	{
		App.WaitForElement("Tab1Content");

		App.Tap("Tab 2");
		App.WaitForElement("Tab2Content");

		App.Tap("Tab 1");
		App.WaitForElement("Tab1Content");
		var normalTransitionCount = App.FindElement("CurrentPageChangedCount").GetText();
		Assert.That(normalTransitionCount, Is.EqualTo("CurrentPageChanged count: 2"));

		App.Tap("ArmReselectionCheck");
		var armedCount = App.WaitForElement("PostTriggerCount").GetText();
		Assert.That(armedCount, Is.EqualTo("Post-trigger count: -1"));

		App.Tap("Tab 1");

		var notificationObserved = App.WaitForTextToBePresentInElement(
			"PostTriggerCount",
			"Post-trigger count: 3",
			TimeSpan.FromSeconds(5));
		var finalCountText = App.WaitForElement("CurrentPageChangedCount").GetText()
			?? throw new InvalidOperationException("CurrentPageChanged count text was unavailable.");
		var finalCount = finalCountText["CurrentPageChanged count: ".Length..];
		Assert.That(notificationObserved, Is.True,
			$"CurrentPageChanged count after same-tab reselection was {finalCount}; expected 3");
		Assert.That(finalCountText, Is.EqualTo("CurrentPageChanged count: 3"));
	}
}
#endif

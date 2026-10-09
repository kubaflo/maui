#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26714 : _IssuesUITest
{
	public Issue26714(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Setting Shell.CurrentItem does not trigger the OnNavigating event";

	[Test]
	[Category(UITestCategories.Shell)]
	public void CurrentItemAssignmentRaisesNavigatingBeforeNavigated()
	{
		var settingsMarker = App.WaitForElement("Issue26714SettingsPageTitle");
		Assert.That(settingsMarker, Is.Not.Null);

		var initialNavigatingCount = App.WaitForElement("Issue26714NavigatingCount").GetText();
		var initialNavigatedCount = App.WaitForElement("Issue26714NavigatedCount").GetText();
		Assert.That(initialNavigatingCount, Is.Not.Null);
		Assert.That(initialNavigatedCount, Is.Not.Null);
		Assert.That(initialNavigatingCount, Is.EqualTo("OnNavigating=-1"));
		Assert.That(initialNavigatedCount, Is.EqualTo("OnNavigated=-1"));

		var trigger = App.WaitForElement("Issue26714SetCurrentItemButton");
		Assert.That(trigger, Is.Not.Null);
		App.Tap("Issue26714SetCurrentItemButton");

		var homeMarker = App.WaitForElement("Issue26714HomePageTitle");
		Assert.That(homeMarker, Is.Not.Null);

		var navigatedObserved = App.WaitForTextToBePresentInElement("Issue26714NavigatedCount", "OnNavigated=1");
		Assert.That(navigatedObserved, Is.True);

		var navigatingText = App.WaitForElement("Issue26714NavigatingCount").GetText();
		var navigatedText = App.WaitForElement("Issue26714NavigatedCount").GetText();
		Assert.That(navigatingText, Is.Not.Null);
		Assert.That(navigatedText, Is.Not.Null);
		var navigatingCount = navigatingText!.Replace("OnNavigating=", string.Empty, StringComparison.Ordinal);
		var navigatedCount = navigatedText!.Replace("OnNavigated=", string.Empty, StringComparison.Ordinal);
		Assert.That(navigatingText, Is.EqualTo("OnNavigating=1"),
			$"Shell.OnNavigating callback count was {navigatingCount}; expected 1 before Shell.OnNavigated count {navigatedCount} after assigning Shell.CurrentItem.");
	}
}
#endif

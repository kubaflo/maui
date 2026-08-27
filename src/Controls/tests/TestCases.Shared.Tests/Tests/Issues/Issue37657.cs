#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37657 : _IssuesUITest
{
	public Issue37657(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Shell back handling is not called on the root page of a Shell tab";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellHandlesBackOnRootPageOfSecondTab()
	{
		Assert.That(App.WaitForElement("CurrentRoot").GetText(), Is.EqualTo("Current root: First"));

		var preBackCount = App.WaitForElement("BackCallbackCount").GetText();
		var preBackToken = App.WaitForElement("LifecycleToken").GetText();
		Assert.That(preBackCount, Is.EqualTo("Back callback count: 0"));
		Assert.That(preBackToken, Is.EqualTo("Awaiting Android Back"));

		App.Tap("Second");
		var secondRootVisible = App.WaitForTextToBePresentInElement(
			"CurrentRoot",
			"Current root: Second",
			TimeSpan.FromSeconds(5));
		Assert.That(secondRootVisible, Is.True, "The second tab root page did not become current.");

		App.Back();

		var callbackCompleted = App.WaitForTextToBePresentInElement(
			"LifecycleToken",
			"Android Back callback completed",
			TimeSpan.FromSeconds(5));
		var postBackToken = App.WaitForElement("LifecycleToken").GetText();
		var callbackCount = App.WaitForElement("BackCallbackCount").GetText();
		Assert.That(
			callbackCompleted,
			Is.True,
			$"Shell.OnBackButtonPressed callback count after Android Back: token was '{postBackToken}' and count was '{callbackCount}'.");

		Assert.That(postBackToken, Is.EqualTo("Android Back callback completed"));
		Assert.That(
			callbackCount,
			Is.EqualTo("Back callback count: 1"),
			$"Shell.OnBackButtonPressed callback count after Android Back was '{callbackCount}'; expected 'Back callback count: 1'.");
	}
}
#endif

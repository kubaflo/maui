#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35002 : _IssuesUITest
{
	const string ActivationStatusId = "ActivationStatusLabel";
	const string ActivationSentinel = "ActivatedControl:None";

	public Issue35002(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "TapGestureRecognizer controls are not selectable with a physical keyboard";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void TapGestureRecognizerCanBeActivatedWithKeyboard()
	{
		App.WaitForElement("KeyboardStartEntry");
		App.WaitForElement("GestureTarget");
		App.WaitForElement("FallbackButton");
		App.WaitForElement("EvaluateButton");
		App.WaitForElement(ActivationStatusId);

		var initialStatus = App.FindElement(ActivationStatusId).GetText();
		Assert.That(initialStatus, Does.Contain(ActivationSentinel));

		App.Tap("KeyboardStartEntry");
		Assert.That(
			App.WaitForTextToBePresentInElement(ActivationStatusId, "FocusedControl:KeyboardStartEntry"),
			Is.True,
			"The entry focus callback did not occur.");

		App.FindElement("KeyboardStartEntry").SendKeys(Keys.Tab + Keys.Enter);

		App.RetryAssert(() =>
		{
			var status = App.FindElement(ActivationStatusId).GetText();
			Assert.That(status, Does.Not.Contain(ActivationSentinel));
		});

		var finalStatus = App.FindElement(ActivationStatusId).GetText();
		if (finalStatus is null)
		{
			Assert.Fail("The activation status label did not expose text.");
			return;
		}

		const string activationPrefix = "ActivatedControl:";
		const string focusPrefix = "FocusedControl:";
		var focusStart = finalStatus.IndexOf(focusPrefix, StringComparison.Ordinal);
		Assert.That(finalStatus, Does.StartWith(activationPrefix));
		Assert.That(focusStart, Is.GreaterThan(activationPrefix.Length));
		var activatedControl = finalStatus[activationPrefix.Length..focusStart].Trim();

		Assert.That(
			activatedControl,
			Is.EqualTo("GestureTarget"),
			$"Issue 35002 keyboard activation expected GestureTarget but observed: {activatedControl}");
	}
}
#endif

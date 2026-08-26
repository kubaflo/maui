#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35002 : _IssuesUITest
{
	public Issue35002(TestDevice device) : base(device) { }

	public override string Issue => "TapGestureRecognizer controls are not selectable with a physical keyboard";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void TapGestureRecognizerCanBeActivatedWithKeyboard()
	{
		var target = App.WaitForElement("TapGestureTarget");
		var targetText = target.GetText();
		Assert.That(targetText, Is.Not.Null);
		Assert.That(targetText, Is.EqualTo("Tap gesture target"));

		var initialActivationText = App.WaitForElement("ActivationEvidence").GetText();
		Assert.That(initialActivationText, Is.Not.Null);
		Assert.That(initialActivationText, Is.EqualTo("Target activations: 0"));

		var initialOutcomeText = App.WaitForElement("KeyboardOutcome").GetText();
		Assert.That(initialOutcomeText, Is.Not.Null);
		Assert.That(initialOutcomeText, Is.EqualTo("Keyboard outcome: pending"));

		App.Tap("FocusStart");
		Assert.That(
			App.WaitForTextToBePresentInElement("FocusStart", "Keyboard focus start: focused", TimeSpan.FromSeconds(5)),
			Is.True,
			"The preceding button did not record keyboard focus.");

		App.WaitForElement("FocusStart").SendKeys(Keys.Tab + Keys.Enter);

		Assert.That(
			App.WaitForTextToBePresentInElement("KeyboardOutcome", "Keyboard result:", TimeSpan.FromSeconds(5)),
			Is.True,
			"Tab and Enter did not produce a target or following-button callback.");

		var activationText = App.WaitForElement("ActivationEvidence").GetText();
		Assert.That(activationText, Is.Not.Null);
		Assert.That(
			activationText,
			Is.EqualTo("Target activations: 1"),
			"TapGestureRecognizer keyboard activation failed: expected one activation after Tab and Enter.");
	}
}
#endif

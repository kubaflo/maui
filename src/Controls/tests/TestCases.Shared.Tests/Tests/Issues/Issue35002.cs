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
	[Category(UITestCategories.Accessibility)]
	public void TapGestureRecognizerLabelCanBeActivatedWithKeyboard()
	{
		var keyboardStartButton = App.WaitForElement("KeyboardStartButton");
		App.WaitForElement("TapTargetLabel");
		App.WaitForElement("KeyboardFallbackButton");

		Assert.That(
			App.WaitForElement("KeyboardBaselineLabel").GetText() ?? throw new InvalidOperationException("Keyboard baseline label text was null."),
			Is.EqualTo("Keyboard baseline activations: 0"));
		Assert.That(
			App.WaitForElement("KeyboardDepartureLabel").GetText() ?? throw new InvalidOperationException("Keyboard departure label text was null."),
			Is.EqualTo("Keyboard start departures: 0"));
		Assert.That(
			App.WaitForElement("TapFocusLabel").GetText() ?? throw new InvalidOperationException("Tap focus label text was null."),
			Is.EqualTo("Tap target keyboard focuses: 0"));
		Assert.That(
			App.WaitForElement("TapActivationLabel").GetText() ?? throw new InvalidOperationException("Tap activation label text was null."),
			Is.EqualTo("Tap target activations: 0"));

		keyboardStartButton.SendKeys(Keys.Enter);
		bool baselineActivated = App.WaitForTextToBePresentInElement(
			"KeyboardBaselineLabel",
			"Keyboard baseline activations: 1",
			timeout: TimeSpan.FromSeconds(5));
		Assert.That(baselineActivated, Is.True, "Enter did not activate the keyboard-start Button.");

		keyboardStartButton.SendKeys(Keys.Tab + Keys.Enter);
		bool keyboardFocusDeparted = App.WaitForTextToBePresentInElement(
			"KeyboardDepartureLabel",
			"Keyboard start departures: 1",
			timeout: TimeSpan.FromSeconds(5));
		Assert.That(keyboardFocusDeparted, Is.True, "Tab did not move keyboard focus away from the keyboard-start Button.");

		App.WaitForElement("TapTargetLabel");
		var tapFocusLabel = App.WaitForElement("TapFocusLabel");
		var tapActivationLabel = App.WaitForElement("TapActivationLabel");
		bool targetActivated = App.WaitForTextToBePresentInElement(
			"TapActivationLabel",
			"Tap target activations: 1",
			timeout: TimeSpan.FromSeconds(5));

		var targetFocusText = tapFocusLabel.GetText() ?? throw new InvalidOperationException("Tap focus label text was null.");
		var targetActivationText = tapActivationLabel.GetText() ?? throw new InvalidOperationException("Tap activation label text was null.");
		Assert.That(
			targetActivated,
			Is.True,
			$"TapGestureRecognizer Label keyboard activation mismatch: focus='{targetFocusText}', activation='{targetActivationText}', expected activation 1.");
	}
}
#endif

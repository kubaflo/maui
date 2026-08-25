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
	public void TapGestureRecognizerCanBeFocusedAndActivatedWithKeyboard()
	{
		const string initialState = "Focused=None; FocusTransitions=-1; ActivationCallbacks=-1; GestureActivations=0; BoundaryActivations=0";
		const string anchorState = "Focused=FocusEntry; FocusTransitions=0; ActivationCallbacks=0; GestureActivations=0; BoundaryActivations=0";
		const string expectedFinalState = "Focused=GestureTarget; FocusTransitions=1; ActivationCallbacks=1; GestureActivations=1; BoundaryActivations=0";

		var focusEntry = App.WaitForElement("FocusEntry");
		App.WaitForElement("GestureTarget");
		App.WaitForElement("BoundaryButton");
		var resultElement = App.WaitForElement("ResultLabel");
		var observedInitialState = resultElement.GetText();
		if (observedInitialState is null)
		{
			Assert.Fail("The direct keyboard event state was unavailable before the trigger.");
			return;
		}

		Assert.That(observedInitialState, Is.EqualTo(initialState));

		App.Tap("FocusEntry");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", anchorState, timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"Tapping FocusEntry should establish the keyboard focus anchor and reset the event counters.");

		focusEntry.SendKeys(Keys.Tab + Keys.Enter);
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "ActivationCallbacks=1", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"Enter should cause exactly one activation callback.");

		var finalState = App.WaitForElement("ResultLabel").GetText();
		if (finalState is null)
		{
			Assert.Fail("The direct keyboard event state was unavailable after the trigger.");
			return;
		}

		Assert.That(
			finalState == expectedFinalState,
			Is.True,
			$"Keyboard Tab+Enter should activate GestureTarget exactly once and not BoundaryButton; observed gesture activations={finalState}; expected final state={expectedFinalState}");
	}
}
#endif

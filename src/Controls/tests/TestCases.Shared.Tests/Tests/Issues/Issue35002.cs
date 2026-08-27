#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35002 : _IssuesUITest
{
	const string GestureTargetId = "GestureTarget";
	const string KeyboardSentinelId = "KeyboardSentinel";
	const string KeyboardStartId = "KeyboardStart";
	const string SetupStatusId = "SetupStatus";
	const string TransitionTraceId = "TransitionTrace";

	public Issue35002(TestDevice device) : base(device)
	{
	}

	public override string Issue => "TapGestureRecognizer controls are not selectable with a physical keyboard";

	[Test]
	[Category(UITestCategories.Accessibility)]
	public void TapGestureRecognizerCanBeActivatedWithKeyboard()
	{
		var gestureTarget = App.WaitForElement(GestureTargetId);
		Assert.That(gestureTarget.GetText(), Is.EqualTo("Gesture target"));
		var keyboardStart = App.WaitForElement(KeyboardStartId);
		App.WaitForElement(KeyboardSentinelId);

		var initialTrace = App.WaitForElement(TransitionTraceId).GetText();
		Assert.That(initialTrace, Is.EqualTo("ACTIVATION: Not triggered"));

		App.Tap(KeyboardStartId);
		Assert.That(
			App.WaitForTextToBePresentInElement(SetupStatusId, "SETUP: Preceding button focused"),
			Is.True,
			"The setup callback did not confirm focus on the preceding button.");

		keyboardStart.SendKeys(Keys.Tab + Keys.Enter);

		Assert.That(
			App.WaitForTextToBePresentInElement(TransitionTraceId, "ACTIVATED:", TimeSpan.FromSeconds(5)),
			Is.True,
			"No activation callback was received after Tab and Enter.");

		var actualActivation = App.WaitForElement(TransitionTraceId).GetText();
		Assert.That(
			actualActivation,
			Is.EqualTo($"ACTIVATED: {GestureTargetId}"),
			$"TapGestureRecognizer target was skipped by keyboard navigation: expected '{GestureTargetId}', observed '{actualActivation}'.");
	}
}
#endif

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
	public void TapGestureRecognizerIsKeyboardNavigableAndSelectable()
	{
		var startEntry = App.WaitForElement("StartEntry");
		App.WaitForElement("GestureTarget");
		App.WaitForElement("TraversalSentinel");
		var initialFocus = App.WaitForElement("FocusDetails").GetText();
		var initialActivation = App.WaitForElement("ResultLabel").GetText();

		Assert.That(initialFocus, Does.Contain("Focused=Unset; FocusCallbacks=0"));
		Assert.That(initialActivation, Does.Contain("Recognizers=1"));
		Assert.That(initialActivation, Does.Contain("TargetTaps=0"));
		Assert.That(initialActivation, Does.Contain("SentinelClicks=0"));
		Assert.That(initialActivation, Does.Contain("TotalActivations=0"));

		App.Tap("StartEntry");
		var entryFocused = App.WaitForTextToBePresentInElement(
			"FocusDetails",
			"Focused=StartEntry",
			timeout: System.TimeSpan.FromSeconds(10));
		Assert.That(entryFocused, Is.True, "StartEntry did not report its Focused callback.");

		startEntry.SendKeys(Keys.Tab + Keys.Enter);

		var focusMoved = App.WaitForTextToBePresentInElement(
			"FocusDetails",
			"FocusCallbacks=2",
			timeout: System.TimeSpan.FromSeconds(10));
		Assert.That(focusMoved, Is.True, "Tab did not produce a post-StartEntry focus callback.");

		var activationOccurred = App.WaitForTextToBePresentInElement(
			"ResultLabel",
			"TotalActivations=1",
			timeout: System.TimeSpan.FromSeconds(10));
		Assert.That(activationOccurred, Is.True, "Enter did not produce an activation callback.");

		var observedFocus = App.WaitForElement("FocusDetails").GetText();
		var observedActivation = App.WaitForElement("ResultLabel").GetText();

		Assert.That(
			observedFocus,
			Does.Contain("Focused=GestureTarget"),
			$"TapGestureRecognizer keyboard focus mismatch: expected GestureTarget, observed '{observedFocus}'.");
		Assert.That(observedActivation, Does.Contain("TargetTaps=1"));
		Assert.That(observedActivation, Does.Contain("SentinelClicks=0"));
	}
}
#endif

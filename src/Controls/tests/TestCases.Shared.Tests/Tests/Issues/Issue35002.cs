#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35002 : _IssuesUITest
{
	public Issue35002(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "TapGestureRecognizer controls are not selectable with a physical keyboard";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void TapGestureRecognizerCanBeFocusedAndActivatedWithKeyboard()
	{
		var focusStartButton = App.WaitForElement("FocusStartButton");
		var gestureLabel = App.WaitForElement("GestureLabel");
		var afterTargetButton = App.WaitForElement("AfterTargetButton");
		var resultLabel = App.WaitForElement("ResultLabel");

		Assert.That(focusStartButton, Is.Not.Null);
		Assert.That(gestureLabel, Is.Not.Null);
		Assert.That(afterTargetButton, Is.Not.Null);
		Assert.That(resultLabel, Is.Not.Null);

		focusStartButton.Click();
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ResultLabel",
				"Initialized; GestureFocus=0; GestureTaps=0; AfterFocus=0; AfterClicks=0",
				TimeSpan.FromSeconds(5)),
			Is.True,
			"The start button callback should initialize all keyboard telemetry.");

		focusStartButton.SendKeys(Keys.Tab + Keys.Enter);
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "Activation observed", TimeSpan.FromSeconds(5)),
			Is.True,
			"Tab and Enter should cause post-trigger focus and activation callbacks.");

		var telemetry = App.FindElement("ResultLabel").GetText()
			?? throw new InvalidOperationException("Keyboard telemetry text was null.");
		Assert.Multiple(() =>
		{
			Assert.That(telemetry, Does.Contain("GestureFocus=1"),
				$"TapGestureRecognizer Label should receive keyboard focus before activation; observed telemetry: {telemetry}");
			Assert.That(telemetry, Does.Contain("GestureTaps=1"),
				$"Enter should invoke the Label TapGestureRecognizer; observed telemetry: {telemetry}");
			Assert.That(telemetry, Does.Contain("AfterFocus=0"),
				$"Tab should not skip to the following Button; observed telemetry: {telemetry}");
			Assert.That(telemetry, Does.Contain("AfterClicks=0"),
				$"Enter should not click the following Button; observed telemetry: {telemetry}");
		});
	}
}
#endif

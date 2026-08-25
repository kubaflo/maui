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
		App.WaitForElement("Issue35002Instructions");
		var keyboardAnchor = App.WaitForElement("Issue35002KeyboardAnchor");
		App.WaitForElement("Issue35002GestureTarget");
		App.WaitForElement("Issue35002AfterTargetButton");
		App.WaitForElement("Issue35002InputStatus");
		var resultLabel = App.WaitForElement("Issue35002ResultStatus");

		var initialState = resultLabel.GetText();
		if (initialState is null)
			Assert.Fail("The initial callback state should be available.");

		Assert.That(initialState, Is.EqualTo("Callback=None; TapCount=0; ButtonClickCount=0"));

		App.Click("Issue35002KeyboardAnchor");
		var hasKeyboardFocus = keyboardAnchor.GetAttribute<string>("HasKeyboardFocus");
		if (hasKeyboardFocus is null)
			Assert.Fail("Windows UI Automation should report the Entry keyboard focus state.");

		Assert.That(string.Equals(hasKeyboardFocus, "True", StringComparison.OrdinalIgnoreCase), Is.True,
			"The Entry should have native Windows keyboard focus before sending keyboard input.");

		keyboardAnchor.SendKeys(Keys.Tab + Keys.Enter);

		App.RetryAssert(() =>
		{
			var callbackState = App.FindElement("Issue35002InputStatus").GetText();
			if (callbackState is null)
				Assert.Fail("The callback state should be available after keyboard input.");

			Assert.That(callbackState, Is.Not.EqualTo("Callback=None"),
				"Tab and Enter should invoke either the gesture target or the following button.");
		}, timeout: TimeSpan.FromSeconds(10));

		var actualState = resultLabel.GetText();
		if (actualState is null)
			Assert.Fail("The final callback counts should be available.");

		Assert.That(actualState, Is.EqualTo("Callback=Tap; TapCount=1; ButtonClickCount=0"),
			$"Keyboard activation skipped the Label TapGestureRecognizer. Actual callback state: {actualState}");
	}
}
#endif

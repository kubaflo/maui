#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35002 : _IssuesUITest
{
	const string StartEntryId = "StartEntry";
	const string TappableLabelId = "TappableLabel";
	const string FollowingButtonId = "FollowingButton";
	const string FocusSequenceLabelId = "FocusSequenceLabel";

	public Issue35002(TestDevice device) : base(device) { }

	public override string Issue => "TapGestureRecognizer controls are not selectable with a physical keyboard";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void TappableLabelReceivesKeyboardFocus()
	{
		var startEntry = App.WaitForElement(StartEntryId);
		if (startEntry is null)
		{
			Assert.Fail("StartEntry should exist before testing keyboard navigation.");
			return;
		}

		App.WaitForElement(TappableLabelId);
		App.WaitForElement(FollowingButtonId);
		App.WaitForTextToBePresentInElement(FocusSequenceLabelId, "None");

		App.Tap(StartEntryId);
		App.WaitForTextToBePresentInElement(FocusSequenceLabelId, "StartEntry");

		startEntry.SendKeys(Keys.Tab);

		var focusSequence = "No post-key focus callback";
		App.RetryAssert(() =>
		{
			var focusSequenceElement = App.WaitForElement(FocusSequenceLabelId);
			if (focusSequenceElement is null)
			{
				Assert.Fail("FocusSequenceLabel should exist after the keyboard focus transition.");
				return;
			}

			var observedFocus = focusSequenceElement.GetText();
			if (observedFocus is null)
			{
				Assert.Fail("FocusSequenceLabel should expose the post-key focused element.");
				return;
			}

			focusSequence = observedFocus;
			Assert.That(focusSequence, Is.Not.EqualTo("StartEntry"),
				"A focus callback should report the post-key focus transition.");
		});

		Assert.That(focusSequence, Is.EqualTo("TappableLabel"),
			$"TappableLabel should receive focus after one Tab key. Actual focused element: {focusSequence}.");
	}
}
#endif

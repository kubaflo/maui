#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26532 : _IssuesUITest
{
	public override string Issue => "Changing BindingContext clears the previous Picker selection";

	public Issue26532(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Picker)]
	public void ChangingBindingContextDoesNotClearPreviousSelection()
	{
		App.WaitForElement("AnswerPicker");
		App.WaitForElement("OriginalSelectionLabel");
		App.WaitForElement("DefectStatusLabel");
		App.WaitForElement("ContextStateLabel");
		App.WaitForElement("NextButton");

		App.Tap("AnswerPicker");
		App.WaitForElement("Answer A");
		App.Tap("Answer A");

		Assert.That(
			App.WaitForTextToBePresentInElement("OriginalSelectionLabel", "Original selection: Answer A"),
			Is.True,
			"The native Picker selection did not update the first question.");

		var statusBeforeRebind = App.FindElement("DefectStatusLabel");
		Assert.That(statusBeforeRebind, Is.Not.Null);
		if (statusBeforeRebind is null)
		{
			Assert.Fail("The defect status label was not found.");
			return;
		}

		Assert.That(statusBeforeRebind.GetText(), Is.EqualTo("No defect observed"));

		App.Tap("NextButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ContextStateLabel", "Second question active"),
			Is.True,
			"The second BindingContext did not become active.");

		var originalSelection = App.FindElement("OriginalSelectionLabel");
		Assert.That(originalSelection, Is.Not.Null);
		if (originalSelection is null)
		{
			Assert.Fail("The original selection label was not found.");
			return;
		}

		var actual = originalSelection.GetText();
		Assert.That(
			actual,
			Is.EqualTo("Original selection: Answer A"),
			$"Issue 26532: changing BindingContext cleared the previous model selection; observed '{actual}', expected 'Answer A'.");
	}
}
#endif

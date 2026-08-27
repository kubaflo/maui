#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26532 : _IssuesUITest
{
	public Issue26532(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Changing BindingContext clears the previously bound Picker selection";

	[Test]
	[Category(UITestCategories.Picker)]
	public void BindingContextChangeDoesNotMutatePreviousPickerSelection()
	{
		const string expectedOriginalSelection = "Original model selection: Answer 2";

		Assert.That(GetRequiredText("QuestionPrompt"), Is.EqualTo("Question 1"));
		App.WaitForElement("Select an answer");
		Assert.That(GetRequiredText("OriginalSelectionLabel"), Is.EqualTo("none"));
		Assert.That(GetRequiredText("PickerItemCountLabel"), Is.EqualTo("Picker item count: 2"));

		App.Tap("AnswerPicker");
		App.WaitForElement("Answer 2");
		App.Tap("Answer 2");

		App.WaitForElement(expectedOriginalSelection);
		var originalSelectionBeforeTrigger = GetRequiredText("OriginalSelectionLabel");
		Assert.That(originalSelectionBeforeTrigger, Is.EqualTo(expectedOriginalSelection));

		App.Tap("NextButton");
		App.WaitForElement("Question 2");
		Assert.That(
			App.WaitForTextToBePresentInElement("PickerItemCountLabel", "Picker item count: 0"),
			Is.True,
			"The Picker did not apply the second model's empty Answers collection.");

		var originalSelectionAfterTrigger = GetRequiredText("OriginalSelectionLabel");
		Assert.That(
			originalSelectionAfterTrigger,
			Is.EqualTo(expectedOriginalSelection),
			$"Issue26532: the BindingContext swap mutated the previously bound SelectedAnswer. Observed '{originalSelectionAfterTrigger}', expected '{expectedOriginalSelection}'.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
		{
			Assert.Fail($"The required element '{automationId}' was not found.");
			return string.Empty;
		}

		var text = element.GetText();
		if (text is null)
		{
			Assert.Fail($"The required element '{automationId}' did not expose text.");
			return string.Empty;
		}

		return text;
	}
}
#endif

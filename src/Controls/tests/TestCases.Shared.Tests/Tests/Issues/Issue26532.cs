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

	public override string Issue => "Changing BindingContext clears the previous Picker selection";

	[Test]
	[Category(UITestCategories.Picker)]
	public void ChangingBindingContextPreservesPreviousSelection()
	{
		var picker = App.WaitForElement("AnswerPicker");
		Assert.That(picker.GetText(), Is.EqualTo("Choose an answer"));
		Assert.That(
			App.WaitForElement("TransitionStatus").GetText(),
			Is.EqualTo("Question index: -1; answer count: -1"));

		App.Tap("AnswerPicker");
		App.WaitForElement("Answer A");
		App.Tap("Answer A");

		Assert.That(
			App.WaitForElement("SelectionStatus").GetText(),
			Is.EqualTo("Selection received: Answer A"));

		var preTriggerSelection = App.WaitForElement("OriginalSelectionStatus").GetText();
		Assert.That(preTriggerSelection, Is.EqualTo("Original selected: Answer A"));

		App.Tap("NextButton");

		Assert.That(
			App.WaitForElement("TransitionStatus").GetText(),
			Is.EqualTo("Question index: 1; answer count: 0"));

		var postTriggerSelection = App.WaitForElement("OriginalSelectionStatus").GetText();
		Assert.That(
			postTriggerSelection,
			Is.EqualTo(preTriggerSelection),
			$"The original QuestionViewModel selection changed after the page BindingContext transition: observed '{postTriggerSelection}', expected '{preTriggerSelection}'.");
	}
}
#endif

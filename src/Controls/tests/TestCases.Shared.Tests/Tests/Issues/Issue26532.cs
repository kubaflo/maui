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

	public override string Issue => "Picker clears the previous BindingContext selection";

	[Test]
	[Category(UITestCategories.Picker)]
	public void OriginalSelectionRemainsAfterBindingContextChanges()
	{
		Assert.That(App.WaitForElement("AnswerPicker"), Is.Not.Null);
		Assert.That(ReadText("TransitionStatus"), Is.EqualTo("BindingContext transition pending"));

		App.Tap("AnswerPicker");

		var firstAnswer = App.WaitForElement(AppiumQuery.ByXPath("//*[@text='First answer']"));
		var secondAnswer = App.WaitForElement(AppiumQuery.ByXPath("//*[@text='Second answer']"));
		Assert.That(firstAnswer, Is.Not.Null, "The first answer should be present in the native Picker choices.");
		Assert.That(secondAnswer, Is.Not.Null, "The second answer should be present in the native Picker choices.");
		secondAnswer?.Tap();

		Assert.That(
			App.WaitForTextToBePresentInElement("AnswerPicker", "Second answer"),
			Is.True,
			"The Picker should display the answer selected through the native Android choice UI.");
		Assert.That(
			App.WaitForTextToBePresentInElement("OriginalSelection", "Original item selected answer: Second answer"),
			Is.True,
			"The first question should receive the user's selection.");
		Assert.That(ReadText("TransitionStatus"), Is.EqualTo("BindingContext transition pending"));

		Assert.That(App.WaitForElement("NextQuestionButton"), Is.Not.Null);
		App.Tap("NextQuestionButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("TransitionStatus", "BindingContext changed to empty question"),
			Is.True,
			"The BindingContext transition should complete.");
		Assert.That(ReadText("ReplacementAnswerCount"), Is.EqualTo("Replacement answer count: 0"));

		_ = App.WaitForTextToBePresentInElement(
			"OriginalSelection",
			"Original item selected answer: Second answer",
			timeout: TimeSpan.FromSeconds(2));
		var observedSelection = ReadText("OriginalSelection");
		Assert.That(
			observedSelection,
			Is.EqualTo("Original item selected answer: Second answer"),
			$"Issue26532 original selection was cleared after BindingContext changed. Observed: '{observedSelection}'; Expected: 'Original item selected answer: Second answer'.");
	}

	string ReadText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		Assert.That(element, Is.Not.Null, $"Element '{automationId}' should exist.");
		return element?.GetText() ?? "<element not found>";
	}
}
#endif

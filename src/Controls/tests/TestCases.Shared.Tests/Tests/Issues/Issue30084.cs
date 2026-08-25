#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30084 : _IssuesUITest
{
	public Issue30084(TestDevice device) : base(device) { }

	public override string Issue => "InputView with TextTransform Uppercase triggers TextChanged twice per character";

	[Test]
	[Category(UITestCategories.Entry)]
	public void UppercaseKeyboardInputRaisesOneTextChangedEvent()
	{
		AssertInitialState("Entry", "UppercaseEntry", "EntryEventTrace");
		AssertInitialState("Editor", "UppercaseEditor", "EditorEventTrace");
		AssertInitialState("SearchBar", "UppercaseSearchBar", "SearchBarEventTrace");

		var entryResult = EnterTextAndGetResult("Entry", "UppercaseEntry", "EntryEventTrace", "d", "D");
		AssertSingleEvent("Entry", "D", entryResult);

		var editorResult = EnterTextAndGetResult("Editor", "UppercaseEditor", "EditorEventTrace", "e", "E");
		AssertSingleEvent("Editor", "E", editorResult);

		var searchBarResult = EnterTextAndGetResult("SearchBar", "UppercaseSearchBar", "SearchBarEventTrace", "f", "F");
		AssertSingleEvent("SearchBar", "F", searchBarResult);
	}

	void AssertInitialState(string controlName, string controlId, string traceId)
	{
		var controlText = App.WaitForElement(controlId).GetText();
		if (controlText is null)
		{
			Assert.Fail($"{controlName} text lookup returned null before keyboard input.");
			return;
		}

		var traceText = App.WaitForElement(traceId).GetText();
		if (traceText is null)
		{
			Assert.Fail($"{controlName} trace lookup returned null before keyboard input.");
			return;
		}

		Assert.That(controlText, Is.Empty, $"{controlName} should be empty before keyboard input.");
		Assert.That(traceText, Is.EqualTo("NO EVENTS"), $"{controlName} raised TextChanged before keyboard input.");
	}

	(string DisplayedText, string Trace, int EventCount) EnterTextAndGetResult(
		string controlName,
		string controlId,
		string traceId,
		string input,
		string expectedText)
	{
		App.Tap(controlId);
		App.EnterText(controlId, input);

		var controlElement = App.WaitForElement(() =>
		{
			var element = App.FindElement(controlId);
			if (element is null)
				return null;

			var text = element.GetText();
			return string.Equals(text, expectedText, StringComparison.Ordinal) ? element : null;
		}, $"Timed out waiting for {controlName} to display transformed text {expectedText}.", TimeSpan.FromSeconds(10));

		var displayedText = controlElement.GetText();
		if (displayedText is null)
		{
			Assert.Fail($"{controlName} text lookup returned null after keyboard input.");
			return (string.Empty, string.Empty, 0);
		}

		var traceElement = App.WaitForElement(() =>
		{
			var element = App.FindElement(traceId);
			if (element is null)
				return null;

			var text = element.GetText();
			return !string.IsNullOrEmpty(text) && text != "NO EVENTS" ? element : null;
		}, $"Timed out waiting for {controlName} TextChanged callback.", TimeSpan.FromSeconds(10));

		var trace = traceElement.GetText();
		if (trace is null)
		{
			Assert.Fail($"{controlName} trace lookup returned null after keyboard input.");
			return (string.Empty, string.Empty, 0);
		}

		var eventCount = trace.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
		return (displayedText, trace, eventCount);
	}

	static void AssertSingleEvent(
		string controlName,
		string expectedText,
		(string DisplayedText, string Trace, int EventCount) result)
	{
		Assert.That(result.DisplayedText, Is.EqualTo(expectedText),
			$"{controlName} displayed {result.DisplayedText}; expected transformed value {expectedText}. Complete trace: {result.Trace}");
		Assert.That(result.EventCount, Is.EqualTo(1),
			$"{controlName} TextChanged event count for one lowercase keyboard character was {result.EventCount}; complete trace: {result.Trace}; expected count 1 and value {expectedText}.");
	}
}
#endif

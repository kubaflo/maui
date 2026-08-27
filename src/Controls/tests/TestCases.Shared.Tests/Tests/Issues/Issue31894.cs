#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31894 : _IssuesUITest
{
	const string EntryText = "Sample entry text";
	const string AffectedEntryId = "AffectedEntry";
	const string FocusStatusId = "FocusStatus";
	const string ResultLabelId = "ResultLabel";

	public Issue31894(TestDevice device) : base(device) { }

	public override string Issue => "Entry selects all text when tapping left of end-aligned text";

	[Test]
	[Category(UITestCategories.Entry)]
	public void TappingLeftOfEndAlignedTextPlacesCaretWithoutSelectingText()
	{
		var affectedEntry = App.WaitForElement(AffectedEntryId);
		var initialFocusState = App.WaitForElement(FocusStatusId).GetText();
		var initialResultState = App.WaitForElement(ResultLabelId).GetText();

		Assert.Multiple(() =>
		{
			Assert.That(affectedEntry.GetText(), Is.EqualTo(EntryText));
			Assert.That(initialFocusState, Is.EqualTo("FocusCount=0"));
			Assert.That(initialResultState, Is.EqualTo("Text=Sample entry text; Alignment=End; IsFocused=False; SelectionLength=0"));
		});

		App.Tap(AffectedEntryId);

		Assert.That(
			App.WaitForTextToBePresentInElement(FocusStatusId, "FocusCount=1", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The Entry Focused callback should run after the pointer tap.");

		var focusedResultState = App.WaitForElement(ResultLabelId).GetText();
		if (focusedResultState is null)
			throw new AssertionException("The Entry selection state should be available after the pointer tap.");

		var selectionLength = GetStateValue(focusedResultState, "SelectionLength");

		Assert.That(
			selectionLength,
			Is.EqualTo(0),
			$"Issue31894 selection mismatch after left-side tap: observed SelectionLength {selectionLength}, expected 0, text length {EntryText.Length}.");
	}

	static int GetStateValue(string state, string key)
	{
		var prefix = $"{key}=";
		var valueStart = state.IndexOf(prefix, StringComparison.Ordinal);
		Assert.That(valueStart, Is.GreaterThanOrEqualTo(0), $"State '{state}' should contain '{prefix}'.");

		valueStart += prefix.Length;
		var valueEnd = state.IndexOf(';', valueStart);
		var value = valueEnd < 0
			? state[valueStart..]
			: state[valueStart..valueEnd];

		return int.Parse(value, CultureInfo.InvariantCulture);
	}
}
#endif

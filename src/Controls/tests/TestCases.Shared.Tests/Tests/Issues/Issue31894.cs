#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31894 : _IssuesUITest
{
	const string EntryId = "Issue31894Entry";
	const string SelectionLengthId = "Issue31894SelectionLength";
	const string OriginalText = "End aligned text";

	public Issue31894(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Entry selects all text when clicking left of end-aligned text";

	[Test]
	[Category(UITestCategories.Entry)]
	public void TappingLeftOfEndAlignedTextPlacesCaretWithoutSelectingText()
	{
		var entry = App.WaitForElement(EntryId);
		var initialText = entry.GetText();
		var entryRect = entry.GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(initialText, Is.EqualTo(OriginalText));
			Assert.That(entryRect.Width, Is.GreaterThan(300), "The Entry must be wide enough to have blank space left of its end-aligned text.");
			Assert.That(App.WaitForElement(SelectionLengthId).GetText(), Is.EqualTo("Selection length: -1"));
		});

		App.Tap(EntryId);

		var actualSelectionLength = App.WaitForElement(SelectionLengthId).GetText();

		Assert.That(
			actualSelectionLength,
			Is.Not.EqualTo("Selection length: -1"),
			"The native TextBox did not report a post-tap selection transition.");
		Assert.That(
			actualSelectionLength,
			Is.EqualTo("Selection length: 0"),
			$"Issue31894: after left-side click, end-aligned Entry reported '{actualSelectionLength}', expected 'Selection length: 0'.");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31894 : _IssuesUITest
{
	public Issue31894(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Entry selects all text when clicking left of end-aligned text";

	[Test]
	[Category(UITestCategories.Entry)]
	public void ClickingLeftOfEndAlignedTextDoesNotSelectAllText()
	{
		const string expectedText = "Selection test text";
		const string sampledPrefix = "Sampled: IsFocused=True; Focused=1; SelectionLength=";

		var entryElement = App.WaitForElement("AlignedEntry");
		var entryText = entryElement.GetText();
		if (entryText is null)
			throw new AssertionException("The Entry text should be available through the native automation element.");

		Assert.That(entryText, Is.EqualTo(expectedText));

		Assert.That(
			App.WaitForTextToBePresentInElement("DiagnosticLabel", "Ready:", TimeSpan.FromSeconds(5)),
			Is.True,
			"The attached Entry should report its initial focus state.");

		var initialDiagnostic = App.WaitForElement("DiagnosticLabel").GetText();
		if (initialDiagnostic is null)
			throw new AssertionException("The initial diagnostic state should be available.");

		Assert.That(initialDiagnostic, Is.EqualTo("Ready: IsFocused=False; Focused=-1; SelectionLength=-1"));

		var entryRect = entryElement.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(entryRect.Width, Is.GreaterThan(0), "The Entry should have a positive native width.");
			Assert.That(entryRect.Height, Is.GreaterThan(0), "The Entry should have a positive native height.");
		});

		var tapX = entryRect.CenterX();
		var tapY = entryRect.CenterY();
		Assert.Multiple(() =>
		{
			Assert.That(tapX, Is.GreaterThan(entryRect.X));
			Assert.That(tapX, Is.LessThan(entryRect.X + entryRect.Width));
			Assert.That(tapY, Is.GreaterThan(entryRect.Y));
			Assert.That(tapY, Is.LessThan(entryRect.Y + entryRect.Height));
		});

		App.Tap("AlignedEntry");

		Assert.That(
			App.WaitForTextToBePresentInElement("DiagnosticLabel", "Focused=1", TimeSpan.FromSeconds(5)),
			Is.True,
			"The element-centered tap should raise exactly one Focused event.");
		Assert.That(
			App.WaitForTextToBePresentInElement("DiagnosticLabel", "Sampled:", TimeSpan.FromSeconds(5)),
			Is.True,
			"The pointer focus should produce a SelectionLength state transition.");

		var sampledDiagnostic = App.WaitForElement("DiagnosticLabel").GetText();
		if (sampledDiagnostic is null)
			throw new AssertionException("The post-focus diagnostic state should be available.");

		Assert.That(sampledDiagnostic, Does.StartWith(sampledPrefix), "The Entry should be focused by exactly one pointer-focus event.");

		var selectionText = sampledDiagnostic[sampledPrefix.Length..];
		Assert.That(
			int.TryParse(selectionText, out var selectionLength),
			Is.True,
			$"The sampled SelectionLength should be numeric, but was '{selectionText}'.");
		Assert.That(
			selectionLength,
			Is.EqualTo(0),
			$"End-aligned Entry selected {selectionLength} characters after left-side pointer focus; expected 0 for text length {entryText.Length}.");
	}
}
#endif

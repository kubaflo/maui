#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27664 : _IssuesUITest
{
	public override string Issue => "Editor does not resize above the iOS keyboard";

	public Issue27664(TestDevice testDevice) : base(testDevice) { }

	[Test]
	[Category(UITestCategories.Editor)]
	public void EditorResizesAboveKeyboardAfterMultilineWrapping()
	{
		const string editorId = "Issue27664Editor";
		const string resultId = "Issue27664Result";
		const string payload = "Line one has enough words to wrap naturally Line two continues with more visible text Line three adds another wrapped row Line four keeps the Editor focused";
		const double tolerance = 2;

		App.SetOrientationPortrait();

		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The iOS window must be in portrait orientation.");

		var editorElement = App.WaitForElement(editorId);
		var initialEditorRect = editorElement.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(initialEditorRect.Width, Is.GreaterThan(0), "The intended Editor must have a nonzero native width.");
			Assert.That(initialEditorRect.Height, Is.GreaterThan(0), "The intended Editor must have a nonzero native height.");
			Assert.That(App.IsKeyboardShown(), Is.False, "The software keyboard must initially be absent.");
		});

		var initialBottomClearance = windowRect.Bottom - initialEditorRect.Bottom;

		App.Tap(editorId);
		Assert.That(App.WaitForKeyboardToShow(), Is.True, "The iOS software keyboard must appear after the Editor is tapped.");
		Assert.That(App.IsKeyboardShown(), Is.True, "The iOS software keyboard must remain visible during text entry.");

		var keyboardRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeKeyboard")).GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(keyboardRect.Width, Is.GreaterThan(0), "The native keyboard must have a nonzero width.");
			Assert.That(keyboardRect.Height, Is.GreaterThan(0), "The native keyboard must have a nonzero height.");
		});

		editorElement.SendKeys(payload);
		Assert.That(
			App.WaitForTextToBePresentInElement(resultId, "Input completed: focused", TimeSpan.FromSeconds(10)),
			Is.True,
			"The Editor TextChanged callback must observe the payload closing token.");
		Assert.That(App.IsKeyboardShown(), Is.True, "The iOS software keyboard must remain visible after text entry.");

		var enteredText = App.WaitForElement(editorId).GetText();
		if (enteredText is null)
			Assert.Fail("The Editor text lookup returned null after text entry.");

		Assert.Multiple(() =>
		{
			Assert.That(enteredText, Does.StartWith("Line one"), "The Editor must contain the payload's opening token.");
			Assert.That(enteredText, Does.EndWith("focused"), "The Editor must contain the payload's closing token.");
		});

		var focusedEditorRect = App.WaitForElement(editorId).GetRect();
		var expectedFocusedHeight = keyboardRect.Top - focusedEditorRect.Top - initialBottomClearance;
		var failureMessage =
			$"Issue27664 Editor remained under the iOS keyboard after focused multiline wrapping. " +
			$"Window={windowRect}, initialEditor={initialEditorRect}, focusedEditor={focusedEditorRect}, " +
			$"keyboard={keyboardRect}, initialClearance={initialBottomClearance}, " +
			$"expectedHeight={expectedFocusedHeight}, tolerance={tolerance}.";

		Assert.Multiple(() =>
		{
			Assert.That(focusedEditorRect.Top, Is.EqualTo(initialEditorRect.Top).Within(tolerance), failureMessage);
			Assert.That(focusedEditorRect.Bottom, Is.LessThanOrEqualTo(keyboardRect.Top + tolerance), failureMessage);
			Assert.That(focusedEditorRect.Height, Is.EqualTo(expectedFocusedHeight).Within(tolerance), failureMessage);
		});
	}
}
#endif

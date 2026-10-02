#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27664 : _IssuesUITest
{
	public Issue27664(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Editor does not resize when the iOS keyboard appears";

	[Test]
	[Category(UITestCategories.Editor)]
	public void EditorResizesAboveSoftwareKeyboard()
	{
		const string inputMarker = "MULTILINE-END";
		const string inputText = "This text is entered into the Editor while the iOS software keyboard is visible. It is intentionally long enough to wrap across multiple lines so the Editor and its current input remain visibly affected by the available page height. The final marker confirms that the complete input reached the intended control. MULTILINE-END";
		const int gridBottomInset = 16;
		const int pointTolerance = 3;

		App.SetOrientationPortrait();
		Assert.That(App.IsKeyboardShown(), Is.False,
			"The software keyboard must be hidden while recording the Editor's initial native frame.");

		var editorElement = App.WaitForElement("IssueEditor");
		var editorFrameBeforeKeyboard = editorElement.GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(editorFrameBeforeKeyboard.Width, Is.GreaterThan(0), "The intended Editor must have a nonzero native width before the keyboard appears.");
			Assert.That(editorFrameBeforeKeyboard.Height, Is.GreaterThan(0), "The intended Editor must have a nonzero native height before the keyboard appears.");
		});

		App.Tap("IssueEditor");

		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5)), Is.True,
			"The iOS software keyboard must be visible before measuring the Editor.");
		Assert.That(App.IsFocused("IssueEditor"), Is.True,
			"The intended Editor must receive focus before text is entered.");

		var keyboardFrame = App.WaitForElement(
			App is AppiumIOSApp iosApp && HelperExtensions.IsIOS26OrHigher(iosApp)
				? "Toolbar"
				: "Done").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(keyboardFrame.Width, Is.GreaterThan(0), "The native iOS keyboard must have a nonzero width.");
			Assert.That(keyboardFrame.Height, Is.GreaterThan(0), "The native iOS keyboard must have a nonzero height.");
			Assert.That(keyboardFrame.Top, Is.LessThan(editorFrameBeforeKeyboard.Bottom),
				"The native iOS keyboard must overlap the Editor's keyboard-hidden frame.");
		});

		App.EnterText("IssueEditor", inputText);
		Assert.That(
			App.WaitForTextToBePresentInElement("IssueEditor", inputMarker, TimeSpan.FromSeconds(10)),
			Is.True,
			"The complete multiline input must reach the intended Editor.");

		App.RetryAssert(() =>
		{
			var editorFrameAfterKeyboard = App.WaitForElement("IssueEditor").GetRect();
			var expectedEditorBottom = keyboardFrame.Top - gridBottomInset;

			Assert.Multiple(() =>
			{
				Assert.That(
					editorFrameAfterKeyboard.Height,
					Is.LessThan(editorFrameBeforeKeyboard.Height - pointTolerance),
					$"Editor did not resize above the iOS keyboard. Before={editorFrameBeforeKeyboard.Height}, After={editorFrameAfterKeyboard.Height}, EditorBottom={editorFrameAfterKeyboard.Bottom}, KeyboardTop={keyboardFrame.Top}, ExpectedBottom={expectedEditorBottom}, Tolerance={pointTolerance}.");
				Assert.That(
					editorFrameAfterKeyboard.Bottom,
					Is.LessThanOrEqualTo(expectedEditorBottom + pointTolerance),
					$"Editor did not resize above the iOS keyboard. Before={editorFrameBeforeKeyboard.Height}, After={editorFrameAfterKeyboard.Height}, EditorBottom={editorFrameAfterKeyboard.Bottom}, KeyboardTop={keyboardFrame.Top}, ExpectedBottom={expectedEditorBottom}, Tolerance={pointTolerance}.");
			});
		});
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26158 : _IssuesUITest
{
	public Issue26158(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Issue 26158: SelectionLength set in Focused callback is reset on iOS";

	[Test]
	[Category(UITestCategories.Entry)]
	public void SelectionLengthSetOnFocusRemainsApplied()
	{
		const string entryText = "Microsoft Maui Entry";
		const string initialResult = "FocusCallbackRan=False;IsFocused=False;Text=Microsoft Maui Entry;ManagedSelectionLength=-1";

		var entry = App.WaitForElement("SelectionEntry");
		Assert.That(entry.GetText(), Is.EqualTo(entryText));
		Assert.That(App.WaitForElement("SelectionResult").GetText(), Is.EqualTo(initialResult));

		App.Tap("SelectionEntry");
		Assert.That(
			App.WaitForTextToBePresentInElement("SelectionResult", "FocusCallbackRan=True", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The Entry focus callback should update the result.");
		var result = App.FindElement("SelectionResult").GetText();
		App.DismissKeyboard();

		Assert.That(result, Does.Contain("FocusCallbackRan=True"));
		Assert.That(result, Does.Contain("IsFocused=True"));
		Assert.That(result, Does.Contain($"Text={entryText}"));
		Assert.That(result, Does.Contain("ManagedSelectionLength=3"), "SelectionLength should remain 3 after focus.");
	}
}
#endif

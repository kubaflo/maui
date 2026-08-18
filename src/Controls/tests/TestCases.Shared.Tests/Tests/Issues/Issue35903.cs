#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35903 : _IssuesUITest
{
	public Issue35903(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Editor control does not show all the text after increasing its height on Windows";

	[Test]
	[Category(UITestCategories.Editor)]
	public void EditorNativeTextViewportExpandsWithWindow()
	{
		App.WaitForElement("IssueEditor");
		App.WaitForElement("ShrinkButton");
		App.WaitForElement("ResultLabel");
		App.Tap("ShrinkButton");
		App.WaitForElement(
			() =>
			{
				var element = App.FindElement("ResultLabel");
				return element.GetText() == "SHRUNK:" ? element : null;
			},
			timeoutMessage: "The Editor did not complete its smaller scrollable layout.");
		App.Tap("ExpandButton");

		var resultElement = App.WaitForElement(
			() =>
			{
				var element = App.FindElement("ResultLabel");
				var text = element.GetText();
				return text is not null && (
					text.StartsWith("PASS:", StringComparison.Ordinal) ||
					text.StartsWith("FAIL:", StringComparison.Ordinal) ||
					text.StartsWith("SETUP:", StringComparison.Ordinal))
					? element
					: null;
			},
			timeoutMessage: "The Editor shrink-expand transition did not complete.",
			timeout: TimeSpan.FromSeconds(15));
		var result = resultElement.GetText();
		Assert.That(result, Does.Not.StartWith("SETUP:"), "Unexpected setup state: " + result);
		Assert.That(
			result,
			Does.StartWith("PASS:"),
			"Editor native text viewport did not expand to fill its enlarged client area; " + result);
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31039 : _IssuesUITest
{
	public Issue31039(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Entry gains focus when an InputTransparent Entry is clicked inside a ScrollView";

	[Test]
	[Category(UITestCategories.Focus)]
	public void InputTransparentEntryDoesNotFocusFirstEntry()
	{
		App.WaitForElement("FirstEntry");
		App.WaitForElement("TransparentEntry");
		App.WaitForElement("LastEntry");
		App.WaitForElement("FirstEntryFocusedCount");

		var initialFocusedCountText = App.WaitForElement("FirstEntryFocusedCount").GetText();
		Assert.That(initialFocusedCountText, Is.EqualTo("FirstEntry Focused Count: 0"),
			"FirstEntry should not receive focus before the reported tap.");

		App.Tap("TransparentEntry");

		var focusedCountText = App.WaitForElement("FirstEntryFocusedCount").GetText();
		if (focusedCountText is null)
		{
			Assert.Fail("FirstEntry focus count was not exposed by the native automation tree.");
		}

		Assert.That(focusedCountText, Is.EqualTo("FirstEntry Focused Count: 0"),
			"Issue31039: FirstEntry received focus after tapping InputTransparent Entry; expected no focus.");
	}
}
#endif

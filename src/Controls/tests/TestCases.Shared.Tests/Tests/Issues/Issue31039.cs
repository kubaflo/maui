#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31039 : _IssuesUITest
{
	public override string Issue => "Entry gains focus when an InputTransparent Entry is clicked inside a ScrollView";

	public Issue31039(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Entry)]
	public void InputTransparentEntryClickDoesNotFocusFirstEntry()
	{
		App.WaitForElement("FirstEntry");
		App.WaitForElement("TransparentEntry");
		App.WaitForElement("ThirdEntry");

		Assert.That(
			App.WaitForTextToBePresentInElement("ProbeLabel", "InitialFirstEntryFocused=0"),
			Is.True,
			"Issue31039: FirstEntry should be unfocused after the page is attached");
		Assert.That(
			App.WaitForTextToBePresentInElement("ProbeLabel", "FocusEventCount=0"),
			Is.True,
			"Issue31039: FirstEntry should be unfocused before clicking InputTransparent Entry");

		App.Click("TransparentEntry");

		Assert.That(
			App.WaitForTextToBePresentInElement("ProbeLabel", "TapSequence=1"),
			Is.True,
			"Issue31039: The enclosing layout tap callback should complete");

		var probeText = App.FindElement("ProbeLabel").GetText();
		if (probeText is null)
		{
			Assert.Fail("Issue31039: The post-click focus probe should contain text");
			return;
		}

		Assert.That(
			probeText,
			Does.Contain("PostClickFirstEntryFocused=0").And.Contain("FocusEventCount=0"),
			$"Issue31039: FirstEntry should remain unfocused after clicking InputTransparent Entry; measured probe={probeText}");
	}
}
#endif

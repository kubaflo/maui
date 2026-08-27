#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33314 : _IssuesUITest
{
	public Issue33314(TestDevice device) : base(device) { }

	public override string Issue => "Editor caret renders as a dot after clearing text and hiding adjacent content";

	[Test]
	[Category(UITestCategories.Editor)]
	public void CaretRemainsFullHeightAfterShiftClearsText()
	{
		var editor = App.WaitForElement("IssueEditor");
		if (editor is null)
		{
			Assert.Fail("The Issue33314 Editor was not found.");
			return;
		}

		App.Tap("IssueEditor");
		Assert.That(
			App.WaitForTextToBePresentInElement("BaselineCaretStatus", "MEASURED:", timeout: TimeSpan.FromSeconds(30)),
			Is.True,
			"The clean focused Editor caret was not measured.");

		var baselineStatus = App.WaitForElement("BaselineCaretStatus");
		if (baselineStatus is null)
		{
			Assert.Fail("The baseline caret status was not found.");
			return;
		}
		Assert.That(baselineStatus.GetText(), Does.Contain("Pass=True"), "The clean focused Editor must render a full-height caret.");

		editor.SendKeys("Sample text");
		var cancelView = App.WaitForElement("CancelView");
		if (cancelView is null)
		{
			Assert.Fail("The cancel ContentView did not become visible after text entry.");
			return;
		}

		editor.SendKeys(Keys.Shift);
		Assert.That(
			App.WaitForTextToBePresentInElement("TransitionStatus", "Shift=1;", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The standalone Shift key did not reach the native Editor.");

		var transitionStatus = App.WaitForElement("TransitionStatus");
		if (transitionStatus is null)
		{
			Assert.Fail("The post-Shift transition status was not found.");
			return;
		}
		Assert.That(transitionStatus.GetText(), Does.Contain("TextEmpty=True"));
		Assert.That(transitionStatus.GetText(), Does.Contain("Selection=0"));
		Assert.That(transitionStatus.GetText(), Does.Contain("Focused=True"));
		Assert.That(transitionStatus.GetText(), Does.Contain("CancelVisible=False"));
		Assert.That(transitionStatus.GetText(), Does.Not.Contain("EmptySequence=-1"));

		Assert.That(
			App.WaitForTextToBePresentInElement("PostTriggerCaretStatus", "MEASURED:", timeout: TimeSpan.FromSeconds(30)),
			Is.True,
			"The post-trigger Editor caret was not measured.");

		var postTriggerStatus = App.WaitForElement("PostTriggerCaretStatus");
		if (postTriggerStatus is null)
		{
			Assert.Fail("The post-trigger caret status was not found.");
			return;
		}

		Assert.That(
			postTriggerStatus.GetText(),
			Does.Contain("Pass=True"),
			$"Issue33314 caret height after Shift clear: {postTriggerStatus.GetText()}; Baseline={baselineStatus.GetText()}; Transition={transitionStatus.GetText()}");
	}
}
#endif

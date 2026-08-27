#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26872 : _IssuesUITest
{
	public Issue26872(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Rectangle RealParent is garbage collected after closing modal pages";

	[Test]
	[Category(UITestCategories.Shape)]
	public void RectangleParentsRemainAvailableAfterModalPagesClose()
	{
		App.WaitForElement("Issue26872OpenPopup");
		AssertText("Issue26872Parent1", "Not inspected");
		AssertText("Issue26872Parent2", "Not inspected");

		OpenVerifyAndClosePopup(1);
		OpenVerifyAndClosePopup(2);

		AssertText("Issue26872Parent1", "Not inspected");
		AssertText("Issue26872Parent2", "Not inspected");

		App.Tap("Issue26872InspectParents");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue26872InspectionStatus", "Inspection complete"),
			Is.True,
			"Parent inspection should complete before its results are asserted.");

		AssertText("Issue26872Attachment1", "Attached");
		AssertText("Issue26872Attachment2", "Attached");
		AssertParentAvailable(1);
		AssertParentAvailable(2);
	}

	void OpenVerifyAndClosePopup(int cycle)
	{
		App.Tap("Issue26872OpenPopup");
		App.WaitForElement("Issue26872ClosePopup");
		App.Tap("Issue26872ClosePopup");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue26872CycleStatus", $"Popup cycle {cycle} of 2 closed."),
			Is.True,
			$"Popup cycle {cycle} should close before the next action.");
		AssertText($"Issue26872Attachment{cycle}", "Attached");
	}

	void AssertParentAvailable(int cycle)
	{
		var parentState = App.WaitForElement($"Issue26872Parent{cycle}").GetText();
		Assert.That(parentState, Is.Not.Null);
		Assert.That(parentState, Is.EqualTo("Available"),
			$"Rectangle {cycle} parent should remain available after modal cycle {cycle}; observed state: {parentState}");
	}

	void AssertText(string automationId, string expected)
	{
		var actual = App.WaitForElement(automationId).GetText();
		Assert.That(actual, Is.Not.Null);
		Assert.That(actual, Is.EqualTo(expected));
	}
}
#endif

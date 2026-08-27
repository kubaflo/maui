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

	public override string Issue => "Rectangle RealParent is garbage collected after closing popups";

	[Test]
	[Category(UITestCategories.Layout)]
	public void RectangleParentsRemainAvailableAfterClosingPopups()
	{
		var initialResult = App.WaitForElement("ResultStatus");
		Assert.That(initialResult, Is.Not.Null);
		var initialText = initialResult.GetText();
		if (initialText is null)
			Assert.Fail("ResultStatus did not expose its initial text.");
		Assert.That(initialText, Is.EqualTo("Unchecked"));

		App.Tap("OpenPopupButton");
		Assert.That(App.WaitForElement("PopupSurface1"), Is.Not.Null);
		Assert.That(
			App.WaitForTextToBePresentInElement("PopupStatus", "Popup 1 ready", TimeSpan.FromSeconds(10)),
			Is.True);
		Assert.That(App.WaitForElement("ClosePopupButton"), Is.Not.Null);
		App.Tap("ClosePopupButton");
		App.WaitForNoElement("PopupSurface1");
		Assert.That(
			App.WaitForTextToBePresentInElement("CycleStatus", "Completed 1 of 2", TimeSpan.FromSeconds(10)),
			Is.True);
		var firstParentStatus = App.WaitForElement("InitialParentStatus");
		Assert.That(firstParentStatus, Is.Not.Null);
		var firstParentText = firstParentStatus.GetText();
		if (firstParentText is null)
			Assert.Fail("InitialParentStatus did not expose cycle 1 text.");
		Assert.That(firstParentText, Is.EqualTo("PopupSurface1"));

		App.Tap("OpenPopupButton");
		Assert.That(App.WaitForElement("PopupSurface2"), Is.Not.Null);
		Assert.That(
			App.WaitForTextToBePresentInElement("PopupStatus", "Popup 2 ready", TimeSpan.FromSeconds(10)),
			Is.True);
		Assert.That(App.WaitForElement("ClosePopupButton"), Is.Not.Null);
		App.Tap("ClosePopupButton");
		App.WaitForNoElement("PopupSurface2");
		Assert.That(
			App.WaitForTextToBePresentInElement("CycleStatus", "Completed 2 of 2", TimeSpan.FromSeconds(10)),
			Is.True);
		var secondParentStatus = App.WaitForElement("InitialParentStatus");
		Assert.That(secondParentStatus, Is.Not.Null);
		var secondParentText = secondParentStatus.GetText();
		if (secondParentText is null)
			Assert.Fail("InitialParentStatus did not expose cycle 2 text.");
		Assert.That(secondParentText, Is.EqualTo("PopupSurface1,PopupSurface2"));

		App.Tap("CheckParentButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("CheckStatus", "Checked", TimeSpan.FromSeconds(10)),
			Is.True);

		var result = App.WaitForElement("ResultStatus");
		Assert.That(result, Is.Not.Null);
		var actualParents = result.GetText();
		if (actualParents is null)
			Assert.Fail("ResultStatus did not expose the retained parent result.");
		const string expectedParents = "PopupSurface1,PopupSurface2";
		Assert.That(
			actualParents,
			Is.EqualTo(expectedParents),
			$"Retained Rectangle parents after two popup close cycles were '{actualParents}', expected '{expectedParents}'.");
	}
}
#endif

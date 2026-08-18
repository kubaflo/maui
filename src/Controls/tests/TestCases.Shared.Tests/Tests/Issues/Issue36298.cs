using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36298 : _IssuesUITest
{
	public Issue36298(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "ContentPresenter throws when retained RefreshView content is reattached";

	[Test]
	[Category(UITestCategories.RefreshView)]
	public void RetainedRefreshViewContentCanBeReattached()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("ReattachmentStatus", "View 1 attached", TimeSpan.FromSeconds(5)),
			Is.True,
			"View 1 did not reach its initial Loaded state.");

		App.Tap("SwitchToViewTwoButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ReattachmentStatus", "View 2 attached", TimeSpan.FromSeconds(5)),
			Is.True,
			"View 2 did not reach its Loaded state.");

		App.Tap("SwitchBackToViewOneButton");
		_ = App.WaitForTextToBePresentInElement("ReattachmentStatus", "View 1 reattached", TimeSpan.FromSeconds(5)) ||
			App.WaitForTextToBePresentInElement("ReattachmentStatus", "ArgumentException", TimeSpan.FromSeconds(5));

		var status = App.FindElement("ReattachmentStatus").GetText();
		Assert.That(
			status,
			Is.EqualTo("View 1 reattached"),
			$"View 1 reattachment status was '{status}', expected 'View 1 reattached'.");
	}
}

using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36652 : _IssuesUITest
{
	const string ExpectedFailureMessage = "Application should remain open after creating Border > SwipeView > Editor.";

	public override string Issue => "Border containing SwipeView causes a native crash on Windows";

	public Issue36652(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void BorderContainingSwipeViewAndEditorShouldRemainOpen()
	{
		Assert.That(
			App.WaitForElement("ReadyStatus").GetText(),
			Is.EqualTo("Ready to create the reported hierarchy."));

		Assert.DoesNotThrow(
			() =>
			{
				App.Tap("ReproduceButton");
				App.WaitForElement("ReportedHierarchyCompleted", timeout: TimeSpan.FromSeconds(20));
			},
			ExpectedFailureMessage);
	}
}

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30317 : _IssuesUITest
{
	public override string Issue => "Narrator reads the custom title bar pane";

	public Issue30317(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Accessibility)]
	public void DefaultTitleBarDoesNotExposeCustomPaneToAccessibility()
	{
		var titleBarAttached = false;
		var attachedState = App.WaitForElement("Issue30317AttachedState");
		titleBarAttached = attachedState.GetText() == "TitleBar attached";
		Assert.That(titleBarAttached, Is.True, "The TitleBar attachment transition did not complete.");

		App.WaitForElement(AppiumQuery.ByXPath("//*[@Name='App Window']"));

		var observedPaneCount = -1;
		observedPaneCount = App.FindElements(
			AppiumQuery.ByXPath("//*[@Name='AppWindow Custom Title Bar']")).Count;

		Assert.That(observedPaneCount, Is.Not.EqualTo(-1));
		Assert.That(
			observedPaneCount,
			Is.EqualTo(0),
			$"Issue30317 custom title-bar pane remained in the Windows accessibility tree; observed {observedPaneCount}, expected 0.");
	}
}
#endif

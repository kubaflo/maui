#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35760 : _IssuesUITest
{
	public override string Issue => "[Android] Shell toolbar title does not update after switching tabs while action mode is open";

	public Issue35760(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void ToolbarTitleUpdatesWhenNavigatingWithActionModeOpen()
	{
		var toolbarTitleQuery = AppiumQuery.ByXPath(
			"//android.widget.HorizontalScrollView/../android.view.ViewGroup/android.widget.TextView");

		App.WaitForElement("Item1");
		App.WaitForElement("NavigateButton");
		Assert.That(App.WaitForElement("NavigationState").GetText(), Is.EqualTo("-1"));
		Assert.That(App.WaitForElement(toolbarTitleQuery).GetText(), Is.EqualTo("Page 1"));

		App.LongPress("Item1");
		App.WaitForElement(AppiumQuery.ByXPath("//*[@text='View']"));
		App.Tap("NavigateButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("NavigationState", "2"),
			Is.True,
			"Page 2 navigation did not complete.");
		App.WaitForElement("PageTwoContent");
		App.WaitForElement(AppiumQuery.ByXPath("//*[@text='FIRST TAB']"));

		Assert.That(
			() => App.FindElement(toolbarTitleQuery).GetText(),
			Is.EqualTo("Page 2").After(3000, 100),
			"Shell toolbar title was 'Page 1' after Page 2 navigation; expected 'Page 2'.");
	}
}
#endif

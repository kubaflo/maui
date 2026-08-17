#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624Tests : _IssuesUITest
{
	public Issue35624Tests(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "SearchHandler CharacterSpacing property is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToNativeText()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue35624Ready");

		var screenSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(screenSize.Height, Is.GreaterThan(screenSize.Width),
			"The SearchHandler scenario should run in portrait orientation.");

		Assert.That(App.WaitForElement("Issue35624Result").GetText(), Is.EqualTo("PENDING"));

		var searchField = App.GetShellSearchHandler();
		Assert.That(searchField.IsEnabled(), Is.True, "The native iOS search field should be enabled.");
		searchField.Tap();
		searchField.SendKeys("MAUI");

		var queryObserved = App.WaitForTextToBePresentInElement(
			"Issue35624Result",
			"Query=MAUI; Callback=True");
		Assert.That(queryObserved, Is.True, "SearchHandler Query should change to MAUI after keyboard input.");

		var nativeTextObserved = App.WaitForTextToBePresentInElement(
			"Issue35624Result",
			"NativeText=MAUI");
		Assert.That(nativeTextObserved, Is.True,
			"The attached native iOS search field should contain the entered MAUI text.");

		var nativeStatus = App.FindElement("Issue35624Result").GetText();
		Assert.That(
			nativeStatus,
			Is.EqualTo("Query=MAUI; Callback=True; NativeText=MAUI; Kern=8; FullRange=True"),
			"SearchHandler native text should apply CharacterSpacing 8");
	}
}
#endif

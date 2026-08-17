#if IOS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33037NavigationPage : _IssuesUITest
{
	const string Title = "Large Title Test";
	const string TitleAssertion = "The standard navigation title should remain visible after scrolling.";

	public override string Issue => "iOS large navigation title disappears after scrolling";

	public Issue33037NavigationPage(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Navigation)]
	public void LargeTitleTransitionsToVisibleStandardTitleAfterScrolling()
	{
		var platformVersionText = ((AppiumApp)App).Driver.Capabilities.GetCapability("platformVersion")?.ToString()
			?? throw new InvalidOperationException("The Appium session did not report a platform version.");
		Assert.That(Version.Parse(platformVersionText), Is.GreaterThanOrEqualTo(new Version(26, 0)));

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(ScreenOrientation.Portrait));

		App.WaitForElement("PageTitle");
		Assert.That(App.WaitForElement("ScrollStatus").GetText(), Is.EqualTo("Scroll offset below 100"));

		var titleQuery = AppiumQuery.ByXPath($"//XCUIElementTypeStaticText[@label='{Title}']");
		var navigationBarQuery = AppiumQuery.ByXPath("//XCUIElementTypeNavigationBar");
		var initialTitle = App.WaitForElement(titleQuery);
		var initialNavigationBar = App.WaitForElement(navigationBarQuery);

		Assert.That(initialTitle.GetText(), Is.EqualTo(Title));
		Assert.That(initialTitle.IsDisplayed(), Is.True);
		Assert.That(initialNavigationBar.GetRect().Height, Is.GreaterThan(44), "The native navigation bar should initially display its large-title region.");

		App.ScrollDown("TestScrollView", ScrollStrategy.Gesture);
		App.ScrollDown("TestScrollView", ScrollStrategy.Gesture);

		Assert.That(
			() => App.FindElement("ScrollStatus").GetText(),
			Is.EqualTo("Scroll offset reached 100").After(5000, 100),
			"The ScrollView should report a vertical offset of at least 100 after two upward swipes.");

		Assert.That(
			() => App.FindElements(titleQuery).Count,
			Is.GreaterThan(0).After(3000, 100),
			TitleAssertion);

		var standardTitle = App.FindElement(titleQuery);
		var navigationBarRect = App.FindElement(navigationBarQuery).GetRect();
		var titleRect = standardTitle.GetRect();

		Assert.That(standardTitle.GetText(), Is.EqualTo(Title), TitleAssertion);
		Assert.That(standardTitle.IsDisplayed(), Is.True, TitleAssertion);
		Assert.That(navigationBarRect.Height, Is.LessThanOrEqualTo(44), TitleAssertion);
		Assert.That(titleRect.Y, Is.GreaterThanOrEqualTo(navigationBarRect.Y), TitleAssertion);
		Assert.That(titleRect.Y + titleRect.Height, Is.LessThanOrEqualTo(navigationBarRect.Y + navigationBarRect.Height), TitleAssertion);
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34780 : _IssuesUITest
{
	public Issue34780(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "iOS 26 TabBar has opaque background";

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void DefaultContentExtendsUnderFloatingTabBar()
	{
		var appiumApp = (AppiumApp)App;
		string platformVersion = appiumApp.Driver.Capabilities.GetCapability("platformVersion")?.ToString() ?? string.Empty;
		Assert.That(Version.Parse(platformVersion), Is.GreaterThanOrEqualTo(new Version(26, 0)),
			"Issue34780 requires iOS 26 or newer.");

		App.SetOrientationPortrait();
		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "Issue34780 requires portrait orientation.");

		var correctTab = App.WaitForElement("Correct");
		var incorrectTab = App.WaitForElement("Incorrect");
		Assert.That(correctTab.IsSelected(), Is.True, "Correct must be the initially selected native tab.");
		AssertContent("Correct");
		Assert.That(App.WaitForElement("CorrectStyle").GetText(), Is.EqualTo("MarginBottom=-100;PaddingBottom=100"));

		var tabBarRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeTabBar")).GetRect();
		var correctTabRect = correctTab.GetRect();
		var incorrectTabRect = incorrectTab.GetRect();
		Assert.That(correctTabRect.Top, Is.GreaterThanOrEqualTo(tabBarRect.Top),
			"Correct must be a native item inside the tab bar.");
		Assert.That(incorrectTabRect.Top, Is.GreaterThanOrEqualTo(tabBarRect.Top),
			"Incorrect must be a native item inside the tab bar.");
		var correctRect = App.WaitForElement("CorrectGrid").GetRect();
		Assert.That(correctRect.Left, Is.EqualTo(windowRect.Left).Within(2), "Correct content must begin at the native window edge.");
		Assert.That(correctRect.Right, Is.EqualTo(windowRect.Right).Within(2), "Correct content must span the native window width.");
		Assert.That(correctRect.Bottom, Is.GreaterThan(tabBarRect.Top),
			"Correct content must establish the behind-tab-bar reference layout.");

		App.Tap("Incorrect");

		var incorrectHeading = App.WaitForElement("IncorrectHeading");
		var transition = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeStaticText[@name='Issue34780Transition' and @label='1']"));
		Assert.That(incorrectHeading, Is.Not.Null, "Incorrect page must become visible after the native tab tap.");
		Assert.That(transition, Is.Not.Null, "CurrentPageChanged must complete after selecting Incorrect.");
		Assert.That(correctTab.IsSelected(), Is.False, "Correct must no longer be the selected native tab.");
		Assert.That(incorrectTab.IsSelected(), Is.True, "Incorrect must be the selected native tab.");

		AssertContent("Incorrect");
		var contentRect = App.WaitForElement("IncorrectScrollView").GetRect();
		var refreshedTabBarRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeTabBar")).GetRect();
		double overlap = contentRect.Bottom - refreshedTabBarRect.Top;
		const double tolerance = 2;

		Assert.That(incorrectTab.GetRect().Top, Is.GreaterThanOrEqualTo(refreshedTabBarRect.Top),
			"Incorrect must remain a native item inside the tab bar after selection.");
		Assert.That(contentRect.Left, Is.EqualTo(windowRect.Left).Within(tolerance),
			"Incorrect content must begin at the native window edge.");
		Assert.That(contentRect.Right, Is.EqualTo(windowRect.Right).Within(tolerance),
			"Incorrect content must span the native window width.");
		Assert.That(contentRect.Bottom, Is.GreaterThan(refreshedTabBarRect.Top + tolerance),
			$"Issue34780 default content must extend beneath the iOS 26 tab bar; contentBottom={contentRect.Bottom:F1}, " +
			$"tabTop={refreshedTabBarRect.Top:F1}, overlap={overlap:F1}, " +
			$"window={windowRect.X:F1},{windowRect.Y:F1},{windowRect.Width:F1},{windowRect.Height:F1}, tolerance={tolerance:F1}");
	}

	void AssertContent(string prefix)
	{
		var scrollRect = App.WaitForElement($"{prefix}ScrollView").GetRect();
		var headingRect = App.WaitForElement($"{prefix}Heading").GetRect();
		Assert.That(headingRect.Top, Is.GreaterThanOrEqualTo(scrollRect.Top),
			$"{prefix} heading must be located inside the ScrollView.");

		string[] colors = ["Red", "Gold", "Green", "Blue", "Purple", "Orange"];
		double previousBottom = headingRect.Bottom;
		foreach (string color in colors)
		{
			var boxRect = App.WaitForElement($"{prefix}{color}Box").GetRect();
			Assert.That(boxRect.Height, Is.EqualTo(120).Within(1),
				$"{prefix} {color} box must retain its issue-derived 120-point height.");
			Assert.That(boxRect.Top, Is.EqualTo(previousBottom).Within(1),
				$"{prefix} {color} box must retain its recorded location and order.");
			previousBottom = boxRect.Bottom;
		}

		var footerRect = App.WaitForElement($"{prefix}Footer").GetRect();
		Assert.That(footerRect.Top, Is.EqualTo(previousBottom).Within(1),
			$"{prefix} footer must follow the six colored boxes.");
	}
}
#endif

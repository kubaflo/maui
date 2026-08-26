#if WINDOWS
using System.Drawing;
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30317 : _IssuesUITest
{
	public override string Issue => "Narrator reads App Window custom titlebar pane";

	public Issue30317(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Accessibility)]
	public void CaptionButtonsDoNotExposeCustomTitleBarPaneAnnouncement()
	{
		const string failureSignature = "Unexpected accessibility name on Windows custom title-bar pane:";

		App.WaitForElement("MainPageReady");
		Assert.That(App, Is.InstanceOf<AppiumApp>());
		var driver = ((AppiumApp)App).Driver;

		driver.Manage().Window.Size = new Size(1280, 720);
		Assert.That(driver.Manage().Window.Size, Is.EqualTo(new Size(1280, 720)));
		Assert.That(driver.FindElements(By.XPath("//*[@Name='TitleBar accessibility']")),
			Has.Count.GreaterThan(0), "The MAUI TitleBar was not attached to the native window.");

		var captionPanes = driver.FindElements(By.XPath("//*[@ClassName='ReunionWindowingCaptionControls']"));
		Assert.That(captionPanes, Has.Count.EqualTo(1),
			"The native AppWindow caption-button pane was not present.");

		var accessibilityName = captionPanes[0].GetAttribute("Name");
		Assert.That(accessibilityName, Is.Not.Null,
			"The native AppWindow caption-button pane did not expose a readable UI Automation Name property.");
		Assert.That(accessibilityName, Is.Empty,
			$"{failureSignature} name={accessibilityName}");
	}
}
#endif

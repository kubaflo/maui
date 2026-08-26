#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30812 : _IssuesUITest
{
	public Issue30812(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Resize exposes an unnecessary More options button";

	[Test]
	[Category(UITestCategories.ToolbarItem)]
	public void ResizeDoesNotExposeMoreOptionsButton()
	{
		var appiumApp = (AppiumApp)App;
		appiumApp.Driver.Manage().Window.Size = new System.Drawing.Size(1280, 720);

		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Width, Is.EqualTo(1280), "The test window width must match the reported geometry.");
		Assert.That(windowSize.Height, Is.EqualTo(720), "The test window height must match the reported geometry.");

		App.WaitForElement("DashboardTitle");
		Assert.That(App.WaitForElement("ResizeStatus").GetText(), Is.EqualTo("Resize not applied"));

		var moreOptionsQuery = AppiumQuery.ByXPath("//*[@Name='More options']");
		Assert.That(App.FindElements(moreOptionsQuery), Is.Empty, "More options must not exist before Resize.");

		App.Tap("ApplyResizeButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ResizeStatus",
				"Resize applied at 200 percent",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The 200 percent Resize transition did not complete.");

		appiumApp.Driver.ExecuteScript("windows: keys", new Dictionary<string, object>
		{
			["actions"] = new[]
			{
				new Dictionary<string, object> { ["virtualKeyCode"] = 0x09, ["down"] = false }
			}
		});

		var moreOptionsCount = App.FindElements(moreOptionsQuery).Count;
		Assert.That(
			moreOptionsCount,
			Is.Zero,
			$"Unnecessary More options button remained after Resize. Observed count: {moreOptionsCount}; expected count: 0.");
	}
}
#endif

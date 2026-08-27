#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30812 : _IssuesUITest
{
	public Issue30812(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Unnecessary More options button appears after applying resize";

	[Test]
	[Category(UITestCategories.Accessibility)]
	public void HiddenMoreOptionsButtonRemainsOutsideKeyboardFocusOrderAfterResize()
	{
		var dashboard = App.WaitForElement("DashboardContainer");
		var widthBeforeResize = dashboard.GetRect().Width;
		Assert.That(widthBeforeResize, Is.GreaterThan(320), "The dashboard must begin wider than the resize target.");
		App.WaitForNoElement("MoreOptionsButton", timeout: TimeSpan.FromSeconds(2));

		var applyResizeButton = App.WaitForElement("ApplyResizeButton");
		applyResizeButton.Tap();
		var resizedDashboard = App.WaitForElement(
			() =>
			{
				var candidate = App.FindElement("DashboardContainer");
				if (candidate is null)
				{
					return null;
				}

				return Math.Abs(candidate.GetRect().Width - 320) <= 2 ? candidate : null;
			},
			timeoutMessage: "The dashboard did not reach the requested 320-unit width.",
			timeout: TimeSpan.FromSeconds(10));

		var widthAfterResize = resizedDashboard.GetRect().Width;
		Assert.That(widthAfterResize, Is.Not.EqualTo(widthBeforeResize), "The dashboard width did not change.");
		Assert.That(widthAfterResize, Is.EqualTo(320).Within(2), "The dashboard did not reach the requested width.");

		applyResizeButton.SendKeys(string.Concat(
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab,
			Keys.Tab));

		Assert.That(
			App.FindElements("MoreOptionsButton"),
			Is.Empty,
			$"More options appeared in accessibility tree after resize; before={widthBeforeResize}, after={widthAfterResize}");
	}
}
#endif

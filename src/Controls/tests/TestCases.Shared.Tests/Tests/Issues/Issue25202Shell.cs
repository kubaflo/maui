#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue25202Shell : _IssuesUITest
{
	public Issue25202Shell(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Custom Shell TitleView geometry changes after registered-route navigation";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RegisteredRouteNavigationPreservesCustomTitleViewGeometry()
	{
		const double geometryTolerance = 2;

		App.SetOrientationPortrait();

		var settingsContent = App.WaitForElement("Issue25202SettingsContent").GetRect();
		Assert.That(settingsContent.Height, Is.GreaterThan(settingsContent.Width),
			"The test must run in portrait orientation.");

		var settingsVisible = App.WaitForTextToBePresentInElement("Issue25202SettingsTitle", "Settings");
		var initialToolbarTitleVisible = App.WaitForTextToBePresentInElement("Issue25202ToolbarTitle", "Settings");
		var languageVisible = App.WaitForTextToBePresentInElement("Issue25202LanguagePicker", "English");
		var navigationVisible = App.WaitForTextToBePresentInElement("Issue25202NavigateButton", "Navigate to login");
		Assert.That(settingsVisible, Is.True, "The Settings page title must be visible.");
		Assert.That(initialToolbarTitleVisible, Is.True, "The custom TitleView must initially show the Settings title.");
		Assert.That(languageVisible, Is.True, "The English Picker selection must be visible.");
		Assert.That(navigationVisible, Is.True, "The registered-route navigation button must be visible.");

		var density = App.GetDisplayDensity();
		var initialToolbar = App.WaitForElementTillPageNavigationSettled("Issue25202RoundedToolbar").GetRect();
		var initialToolbarContent = App.WaitForElement("Issue25202ToolbarContent").GetRect();
		Assert.That(initialToolbar.Width, Is.GreaterThan(0), "The custom TitleView must have positive width.");
		Assert.That(initialToolbar.Height, Is.GreaterThan(0), "The custom TitleView must have positive height.");
		Assert.That(initialToolbarContent.X - initialToolbar.X, Is.EqualTo(20 * density).Within(geometryTolerance),
			"The custom TitleView must preserve its 20-DIP left padding.");
		Assert.That(initialToolbar.X + initialToolbar.Width - initialToolbarContent.X - initialToolbarContent.Width,
			Is.EqualTo(20 * density).Within(geometryTolerance),
			"The custom TitleView must preserve its 20-DIP right padding.");

		App.Tap("Issue25202NavigateButton");

		var postTriggerObservations = -1;
		var routeVisible = App.WaitForTextToBePresentInElement("Issue25202LoginContent", "Login route");
		var loginTitleVisible = App.WaitForTextToBePresentInElement("Issue25202ToolbarTitle", "Log in");
		if (routeVisible && loginTitleVisible)
			postTriggerObservations = 2;

		Assert.That(postTriggerObservations, Is.EqualTo(2),
			"Registered-route content and the reused TitleView title must both update after navigation.");

		var routedToolbar = App.WaitForElementTillPageNavigationSettled("Issue25202RoundedToolbar").GetRect();
		Assert.That(routedToolbar.Width, Is.GreaterThan(0), "The routed custom TitleView must still have positive width.");
		Assert.That(routedToolbar.Height, Is.GreaterThan(0), "The routed custom TitleView must still have positive height.");

		var geometryUnchanged =
			Math.Abs(routedToolbar.X - initialToolbar.X) <= geometryTolerance &&
			Math.Abs(routedToolbar.Y - initialToolbar.Y) <= geometryTolerance &&
			Math.Abs(routedToolbar.Width - initialToolbar.Width) <= geometryTolerance &&
			Math.Abs(routedToolbar.Height - initialToolbar.Height) <= geometryTolerance;

		Assert.That(geometryUnchanged, Is.True,
			$"Custom Shell TitleView geometry changed after registered-route navigation. " +
			$"Initial={initialToolbar}; Routed={routedToolbar}; Tolerance={geometryTolerance}px.");
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28893Tests : _IssuesUITest
{
	public Issue28893Tests(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] CarouselView with Bindable Gradient Border crash app";

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void GradientBorderRefreshKeepsApplicationRunning()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("IssueHeader");
		App.WaitForElement("AffectedCarousel");
		App.WaitForElement("RefreshButton");
		Assert.That(App.FindElements("First"), Is.Empty);
		Assert.That(App.FindElements("Fourth"), Is.Empty);

		if (App is not AppiumApp appiumApp)
			throw new AssertionException("The iOS test requires the Appium driver.");

		var bundleCapability = appiumApp.Driver.Capabilities.GetCapability("bundleId");
		if (bundleCapability is null)
			throw new AssertionException("The iOS Appium session did not provide a bundleId capability.");

		var bundleId = bundleCapability.ToString();
		if (string.IsNullOrEmpty(bundleId))
			throw new AssertionException("The iOS Appium bundleId capability was empty.");

		Assert.That(QueryAppState(appiumApp, bundleId), Is.EqualTo(4),
			"The issue page must be foregrounded before refreshing.");

		App.Tap("RefreshButton");

		var observedAppState = -1;
		observedAppState = QueryAppState(appiumApp, bundleId);
		Assert.That(observedAppState, Is.Not.EqualTo(-1),
			"The post-refresh iOS app-state observation must be recorded.");

		if (observedAppState != 4)
		{
			App.LaunchApp();
			App.WaitForElement("GoToTestButton");
		}

		Assert.That(observedAppState, Is.EqualTo(4),
			$"CarouselView gradient Border refresh must keep the iOS app running; observed app state={observedAppState}");

		App.WaitForElement("First", timeout: TimeSpan.FromSeconds(10));
		App.WaitForElement("Fourth", timeout: TimeSpan.FromSeconds(10));
	}

	static int QueryAppState(AppiumApp appiumApp, string bundleId)
	{
		var state = appiumApp.Driver.ExecuteScript(
			"mobile: queryAppState",
			new Dictionary<string, object> { ["bundleId"] = bundleId });

		if (state is null)
			throw new AssertionException("The iOS app-state query returned no state.");

		return Convert.ToInt32(state);
	}
}
#endif

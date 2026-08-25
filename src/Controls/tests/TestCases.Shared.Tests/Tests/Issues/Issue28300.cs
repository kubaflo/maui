using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if IOS
public class Issue28300 : _IssuesUITest
{
	public override string Issue => "Custom busy indicator remains visible after loading completes";

	public Issue28300(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Animation)]
	public void CustomBusyIndicatorHidesAfterStopCallback()
	{
		if (App is not AppiumIOSApp)
		{
			Assert.Fail("Issue28300 requires the iOS Appium runner.");
			return;
		}

		App.SetOrientationPortrait();
		var iosApp = (AppiumIOSApp)App;
		var windowSize = iosApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The iOS device should be in portrait orientation.");

		var navigationStatus = App.WaitForElement("NavigationStatus").GetText();
		Assert.That(navigationStatus, Is.Not.Null);
		Assert.That(navigationStatus, Is.EqualTo("NavigationPending"));

		App.Tap("StartWizardButton");
		App.WaitForElement("WizardPageLoadedLabel");

		AssertStatus("IndicatorStatus", "IndicatorAttachedAndVisible=True");
		AssertStatus("AnimationStatus", "AnimationStarted=True");
		AssertStatus("StopStatus", "StopRequested=True");

		App.RetryAssert(() =>
		{
			var displayedCount = 0;
			foreach (var indicator in App.FindElements("CustomBusyIndicator"))
			{
				if (indicator.IsDisplayed())
					displayedCount++;
			}

			Assert.That(
				displayedCount,
				Is.Zero,
				$"Custom busy indicator remained visible after stop callback: observed {displayedCount} displayed native elements; expected 0 within 2 seconds.");
		}, timeout: TimeSpan.FromSeconds(2));
	}

	void AssertStatus(string automationId, string expectedText)
	{
		App.RetryAssert(() =>
		{
			var actualText = App.FindElement(automationId).GetText();
			Assert.That(actualText, Is.Not.Null);
			Assert.That(actualText, Is.EqualTo(expectedText));
		});
	}
}
#endif

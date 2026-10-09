#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28893 : _IssuesUITest
{
	public Issue28893(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] CarouselView with bindable gradient Border crashes app";

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void UpdatingAttachedCarouselWithBoundGradientBorderKeepsAppRunning()
	{
		App.SetOrientationPortrait();

		var rootRect = App.WaitForElement("Issue28893Root").GetRect();
		Assert.That(rootRect.Height, Is.GreaterThan(rootRect.Width), "The issue requires a portrait window.");
		Assert.That(App.FindElements("GradientCarousel"), Is.Not.Empty);
		Assert.That(App.FindElements("UpdateItemsButton"), Is.Not.Empty);
		Assert.That(App.FindElementsByText("Red to orange"), Is.Empty, "The CarouselView must initially have no items.");
		Assert.That(App.AppState, Is.EqualTo(ApplicationState.Running));

		App.Tap("UpdateItemsButton");

		var outcome = "Pending";
		var appState = ApplicationState.Unknown;
		var renderedItemCount = 0;

		for (var attempt = 0; attempt < 20; attempt++)
		{
			appState = App.AppState;
			if (appState == ApplicationState.NotRunning)
			{
				outcome = "Crash";
				break;
			}

			if (appState != ApplicationState.Running)
				continue;

			renderedItemCount = App.FindElementsByText("Red to orange").Count;

			if (renderedItemCount > 0)
			{
				outcome = "Rendered";
				break;
			}
		}

		try
		{
			Assert.That(appState, Is.Not.EqualTo(ApplicationState.Unknown), "The post-trigger application state must be observable.");
			Assert.That(outcome, Is.Not.EqualTo("Pending"), "The update must produce a post-trigger render or process-state transition.");
			Assert.That(appState, Is.EqualTo(ApplicationState.Running),
				$"Bound-gradient CarouselView update must keep the iOS app running while realizing the first assigned item. Outcome={outcome}; AppState={appState}; RenderedItemCount={renderedItemCount}");
			Assert.That(renderedItemCount, Is.GreaterThan(0), "The first assigned bound-gradient item must be realized.");
		}
		finally
		{
			if (appState == ApplicationState.NotRunning)
				App.LaunchApp();
		}
	}
}
#endif

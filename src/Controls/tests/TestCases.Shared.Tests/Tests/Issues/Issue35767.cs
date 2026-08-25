#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35767 : _IssuesUITest
{
	public Issue35767(TestDevice device) : base(device)
	{
	}

	public override string Issue => "SearchHandler.ShowsResults does not work correctly";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RuntimeShowsResultsFalseHidesSearchResults()
	{
		var searchBox = App.WaitForElement("TextBox");
		if (searchBox is null)
		{
			Assert.Fail("The native Shell search TextBox was not found.");
			return;
		}

		searchBox.Tap();
		App.EnterText("TextBox", "alpha");

		var alphaResultQuery = AppiumQuery.ByXPath("//*[@Name='alpha result']");
		var alphaResult = App.WaitForElement(alphaResultQuery);
		if (alphaResult is null)
		{
			Assert.Fail("The reference alpha result was not displayed.");
			return;
		}

		alphaResult.Tap();
		App.WaitForNoElement(alphaResultQuery);

		App.Tap("Issue35767DisableResults");

		var transitionStatus = App.WaitForElement("Issue35767TransitionStatus");
		if (transitionStatus is null)
		{
			Assert.Fail("The ShowsResults transition status was not found.");
			return;
		}

		App.RetryAssert(() =>
		{
			Assert.That(transitionStatus.GetText(), Is.EqualTo("Count=1; ShowsResults=False"));
		});

		var transitionText = transitionStatus.GetText();
		Assert.That(transitionText, Does.Contain("Count=1"), "The ShowsResults property-changed callback did not occur.");
		Assert.That(transitionText, Does.Contain("ShowsResults=False"), "ShowsResults did not change to false.");

		searchBox.Tap();
		App.ClearText("TextBox");

		var betaResultQuery = AppiumQuery.ByXPath("//*[@Name='beta result']");
		Assert.That(App.FindElements(betaResultQuery), Is.Empty, "The beta result existed before entering its query.");

		App.EnterText("TextBox", "beta");

		var observedBetaResultCount = 0;
		var settleUntil = DateTime.UtcNow + TimeSpan.FromSeconds(2);
		do
		{
			observedBetaResultCount = Math.Max(observedBetaResultCount, App.FindElements(betaResultQuery).Count);
		}
		while (DateTime.UtcNow < settleUntil);

		Assert.That(observedBetaResultCount, Is.EqualTo(0),
			$"SearchHandler results remained visible after ShowsResults=False; observed {observedBetaResultCount} native beta result elements, expected 0.");
	}
}
#endif

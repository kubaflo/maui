#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26144 : _IssuesUITest
{
	const string ContentId = "Issue26144DashboardContent";

	public Issue26144(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Nested Shell content disappears after navigating away and back";

	[Test]
	[Category(UITestCategories.Shell)]
	public void NestedShellContentRendersAfterSecondNavigation()
	{
		WaitForText("Issue26144MainRouteStatus", "Main:0");

		App.Tap("Issue26144OpenDashboardButton");
		WaitForText("Issue26144DashboardRouteStatus", "Dashboard:1");

		var firstTokenElement = App.WaitForElement("Issue26144DashboardInstanceToken");
		var firstToken = firstTokenElement.GetText();
		Assert.That(firstToken, Is.Not.Null.And.Not.EqualTo("not-created").And.Not.Empty, "The first Dashboard Shell instance token should be present");

		App.WaitForElement(ContentId);
		var firstContentElements = App.FindElements(ContentId);
		Assert.That(firstContentElements, Has.Count.EqualTo(1), "The first Dashboard visit should contain exactly one primary content label");
		var firstContent = firstContentElements.Single();
		var firstRect = firstContent.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(firstContent.GetText(), Is.EqualTo("Dashboard content visible"));
			Assert.That(firstContent.IsDisplayed(), Is.True, "The first Dashboard content label should be displayed");
			Assert.That(firstRect.Width, Is.GreaterThan(0), "The first Dashboard content label should have positive width");
			Assert.That(firstRect.Height, Is.GreaterThan(0), "The first Dashboard content label should have positive height");
		});

		App.Tap("Issue26144ReturnToMainButton");
		WaitForText("Issue26144MainRouteStatus", "Main:1");

		App.Tap("Issue26144OpenDashboardButton");
		WaitForText("Issue26144DashboardRouteStatus", "Dashboard:2");

		var secondTokenElement = App.WaitForElement("Issue26144DashboardInstanceToken");
		Assert.That(secondTokenElement.GetText(), Is.EqualTo(firstToken), "The nested Dashboard Shell instance should be reused");

		try
		{
			App.WaitForElement(ContentId);
		}
		catch (TimeoutException)
		{
			// The final assertion below reports the native rendering state.
		}

		var contentElements = App.FindElements(ContentId);
		bool isDisplayed = contentElements.Count == 1 && contentElements.Single().IsDisplayed();
		var contentRect = contentElements.Count == 1 ? contentElements.Single().GetRect() : default;

		Assert.That(
			contentElements.Count == 1 && isDisplayed && contentRect.Width > 0 && contentRect.Height > 0,
			Is.True,
			$"Dashboard content was not natively displayed after the second Shell navigation; count={contentElements.Count}, displayed={isDisplayed}, width={contentRect.Width}, height={contentRect.Height}; expected count=1, displayed=True, width>0, height>0");
	}

	void WaitForText(string automationId, string expectedText)
	{
		App.WaitForElement(
			() => App.FindElements(automationId).FirstOrDefault(element => element.GetText() == expectedText),
			$"Timed out waiting for {automationId} to report {expectedText}");
	}
}
#endif

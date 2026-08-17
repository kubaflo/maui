#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33037Repro : _IssuesUITest
{
	public Issue33037Repro(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "iOS Large Title display disappears";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void CompactTitleRemainsVisibleAfterScrolling()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires a portrait viewport.");

		var status = App.WaitForElement("ResultStatus");
		Assert.That(status.GetText(), Is.EqualTo("NO BUG: waiting for the scroll trigger"));

		App.WaitForElement("TestScrollView");
		var titleQuery = AppiumQuery.ByAccessibilityId("Large Title Test");
		var initialTitle = App.WaitForElement(titleQuery);
		Assert.That(initialTitle.IsDisplayed(), Is.True, "The large navigation title must be visible before scrolling.");
		Assert.That(initialTitle.GetRect().Height, Is.GreaterThan(0), "The large navigation title must have visible native geometry before scrolling.");

		App.ScrollDown("TestScrollView");
		App.ScrollDown("TestScrollView");

		Assert.That(
			App.WaitForTextToBePresentInElement("ResultStatus", "SCROLLED PAST 80", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The ScrollView.Scrolled callback must prove that ScrollY crossed 80 points.");
		Assert.That(App.FindElement("ResultStatus").GetText(), Is.EqualTo("SCROLLED PAST 80"));

		const string failureMessage = "The compact navigation title must remain visibly rendered after scrolling.";
		App.RetryAssert(() =>
		{
			var compactTitles = App.FindElements(titleQuery);
			Assert.That(compactTitles.Count, Is.EqualTo(1), failureMessage);

			var compactTitle = compactTitles.Single();
			var compactTitleRect = compactTitle.GetRect();
			var scrollViewRect = App.FindElement("TestScrollView").GetRect();
			var isVisibleInCompactRegion =
				compactTitle.IsDisplayed() &&
				compactTitleRect.Width > 0 &&
				compactTitleRect.Height > 0 &&
				compactTitleRect.X >= 0 &&
				compactTitleRect.Y >= 0 &&
				compactTitleRect.Right <= windowSize.Width &&
				compactTitleRect.Bottom <= scrollViewRect.Y;

			Assert.That(isVisibleInCompactRegion, Is.True, failureMessage);
		}, timeout: TimeSpan.FromSeconds(5));
	}
}
#endif

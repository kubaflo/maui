#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26144 : _IssuesUITest
{
	const string BackToMainId = "Issue26144BackToMain";
	const string ContentId = "Issue26144DashboardContent";
	const string OpenDashboardId = "Issue26144OpenDashboard";
	const string VisitCountId = "Issue26144DashboardVisitCount";

	public Issue26144(TestDevice device) : base(device) { }

	public override string Issue => "Shell TabBar content does not render after navigating away and back";

	[Test]
	[Category(UITestCategories.Shell)]
	public void DashboardContentRemainsVisibleAfterSecondVisit()
	{
		var observedVisitCount = -1;

		App.WaitForElement(OpenDashboardId);
		App.Tap(OpenDashboardId);
		observedVisitCount = AssertVisitCount(1, observedVisitCount);

		App.RetryAssert(() =>
		{
			var firstVisitMarkers = App.FindElements(ContentId);
			Assert.That(firstVisitMarkers.Count, Is.EqualTo(1), "The first dashboard visit should create exactly one content marker.");
			var firstVisitMarker = firstVisitMarkers.Single();
			var firstVisitFrame = firstVisitMarker.GetRect();
			Assert.That(firstVisitMarker.IsDisplayed(), Is.True, "Dashboard content should be displayed on the first visit.");
			Assert.That(firstVisitFrame.Width, Is.GreaterThan(0), "Dashboard content should have a nonzero width on the first visit.");
			Assert.That(firstVisitFrame.Height, Is.GreaterThan(0), "Dashboard content should have a nonzero height on the first visit.");
		});

		App.Tap(BackToMainId);
		App.WaitForElement(OpenDashboardId);
		App.Tap(OpenDashboardId);
		observedVisitCount = AssertVisitCount(2, observedVisitCount);

		App.RetryAssert(() =>
		{
			var markers = App.FindElements(ContentId);
			var isDisplayed = false;
			var width = -1;
			var height = -1;

			if (markers.Count == 1)
			{
				var marker = markers.Single();
				var frame = marker.GetRect();
				isDisplayed = marker.IsDisplayed();
				width = frame.Width;
				height = frame.Height;
			}

			Assert.That(
				markers.Count == 1 && isDisplayed && width > 0 && height > 0,
				Is.True,
				$"Dashboard content was not visibly rendered after second Shell visit. Count={markers.Count}, Displayed={isDisplayed}, Width={width}, Height={height}.");
		});
	}

	int AssertVisitCount(int expectedCount, int observedVisitCount)
	{
		App.RetryAssert(() =>
		{
			var countText = App.FindElement(VisitCountId).GetText();
			observedVisitCount = countText == $"Dashboard visits: {expectedCount}" ? expectedCount : -1;
			Assert.That(observedVisitCount, Is.EqualTo(expectedCount), $"Dashboard navigation count should transition to {expectedCount}.");
		});

		return observedVisitCount;
	}
}
#endif

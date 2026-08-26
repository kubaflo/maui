#if WINDOWS
using System.Text.RegularExpressions;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30949 : _IssuesUITest
{
	const string EventStatusId = "Issue30949EventStatus";
	const string SwipeTargetId = "Issue30949SwipeTarget";

	public Issue30949(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "[Windows] SwipeView gesture events are not raised";

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void SwipeViewRaisesGestureEventsWhenLeftItemsAreRevealed()
	{
		var swipeTarget = App.WaitForElement(SwipeTargetId);
		App.WaitForElement(EventStatusId);

		var initialCounts = GetEventCounts();
		Assert.Multiple(() =>
		{
			Assert.That(initialCounts.Started, Is.Zero, "SwipeStarted count should begin at zero.");
			Assert.That(initialCounts.Changing, Is.Zero, "SwipeChanging count should begin at zero.");
			Assert.That(initialCounts.Ended, Is.Zero, "SwipeEnded count should begin at zero.");
		});

		var appiumApp = App as AppiumApp;
		Assert.That(appiumApp, Is.Not.Null, "The test requires an active Appium window.");
		if (appiumApp is null)
			throw new InvalidOperationException("The test requires an active Appium window.");

		var windowWidth = appiumApp.Driver.Manage().Window.Size.Width;
		Assert.That(windowWidth, Is.GreaterThan(0), "The active Appium window should have a measurable width.");

		var targetRect = swipeTarget.GetRect();
		var initialTargetX = targetRect.X;
		var startX = targetRect.X + targetRect.Width / 2;
		var centerY = targetRect.Y + targetRect.Height / 2;
		App.DragCoordinates(startX, centerY, startX + windowWidth * 0.25f, centerY);

		App.RetryAssert(() =>
		{
			var revealedTargetX = App.WaitForElement(SwipeTargetId).GetRect().X;
			Assert.That(revealedTargetX, Is.GreaterThan(initialTargetX),
				$"The SwipeView content remained at x={revealedTargetX}; it should move right from x={initialTargetX} when the LeftItems are revealed.");
		});

		App.RetryAssert(() =>
		{
			var startedCount = GetEventCounts().Started;
			Assert.That(startedCount, Is.GreaterThan(0),
				$"SwipeStarted event count was {startedCount} after the SwipeView revealed its LeftItems; expected greater than 0.");
		});
		App.RetryAssert(() =>
		{
			var changingCount = GetEventCounts().Changing;
			Assert.That(changingCount, Is.GreaterThan(0),
				$"SwipeChanging event count was {changingCount} after the SwipeView revealed its LeftItems; expected greater than 0.");
		});
		App.RetryAssert(() =>
		{
			var endedCount = GetEventCounts().Ended;
			Assert.That(endedCount, Is.GreaterThan(0),
				$"SwipeEnded event count was {endedCount} after the SwipeView revealed its LeftItems; expected greater than 0.");
		});
	}

	(int Started, int Changing, int Ended) GetEventCounts()
	{
		var statusText = App.WaitForElement(EventStatusId).GetText();
		if (statusText is null)
			throw new InvalidOperationException("The SwipeView event status label did not expose text.");

		var match = Regex.Match(statusText, @"^Events: started=(\d+), changing=(\d+), ended=(\d+)$");
		Assert.That(match.Success, Is.True, $"Could not parse SwipeView event counts from '{statusText}'.");

		return (
			int.Parse(match.Groups[1].Value),
			int.Parse(match.Groups[2].Value),
			int.Parse(match.Groups[3].Value));
	}
}
#endif

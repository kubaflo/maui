#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30949 : _IssuesUITest
{
	public Issue30949(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "SwipeView swipe events do not fire on Windows";

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void SwipeEventsFireAfterPointerDrag()
	{
		if (App is not AppiumWindowsApp windowsApp)
		{
			throw new InvalidOperationException($"Invalid App type for this test: {App}. Expected {nameof(AppiumWindowsApp)}.");
		}

		var rootRect = App.WaitForElement("Issue30949Root").GetRect();
		var contentRect = App.WaitForElement("Issue30949SwipeContent").GetRect();
		var initialCounts = App.WaitForElement("Issue30949EventCounts").GetText()
			?? throw new InvalidOperationException("The event count label did not expose text.");

		Assert.That(initialCounts, Is.EqualTo("Started: 0; Changing: 0; Ended: 0"));

		var startX = contentRect.X + (contentRect.Width / 2);
		var startY = contentRect.Y + (contentRect.Height / 2);
		var segmentLength = rootRect.Width * 18 / 100;

		Assert.Multiple(() =>
		{
			Assert.That(startX, Is.GreaterThanOrEqualTo(contentRect.X));
			Assert.That(startX, Is.LessThanOrEqualTo(contentRect.X + contentRect.Width));
			Assert.That(startY, Is.GreaterThanOrEqualTo(contentRect.Y));
			Assert.That(startY, Is.LessThanOrEqualTo(contentRect.Y + contentRect.Height));
		});

		var pointer = new AppiumPointerInputDevice(PointerKind.Touch);
		var drag = new ActionSequence(pointer, 0);
		drag.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
		drag.AddAction(pointer.CreatePointerDown(PointerButton.TouchContact));
		drag.AddAction(pointer.CreatePointerMove(
			CoordinateOrigin.Viewport, startX + segmentLength, startY, TimeSpan.FromMilliseconds(250)));
		drag.AddAction(pointer.CreatePointerMove(
			CoordinateOrigin.Viewport, startX + (segmentLength * 2), startY, TimeSpan.FromMilliseconds(250)));
		windowsApp.Driver.PerformActions([drag]);

		var draggedContentX = App.WaitForElement("Issue30949SwipeContent").GetRect().X;

		var release = new ActionSequence(pointer, 0);
		release.AddAction(pointer.CreatePointerUp(PointerButton.TouchContact));
		windowsApp.Driver.PerformActions([release]);

		Assert.That(draggedContentX, Is.GreaterThan(contentRect.X + 5),
			$"The Windows SwipeView must move its content while the rightward drag is held before event counts are evaluated. Initial X={contentRect.X}, dragged X={draggedContentX}.");

		_ = App.WaitForTextToBePresentInElement(
			"Issue30949EventCounts", "Started: 1", timeout: TimeSpan.FromSeconds(2));
		var countText = App.WaitForElement("Issue30949EventCounts").GetText()
			?? throw new InvalidOperationException("The event count label did not expose text after the drag.");
		var countParts = countText.Split([' ', ':', ';'], StringSplitOptions.RemoveEmptyEntries);

		Assert.That(countParts, Has.Length.EqualTo(6),
			$"Unexpected event count format: {countText}");

		var startedCount = int.Parse(countParts[1]);
		var changingCount = int.Parse(countParts[3]);
		var endedCount = int.Parse(countParts[5]);

		Assert.Multiple(() =>
		{
			Assert.That(startedCount, Is.GreaterThan(0),
				$"SwipeStarted count after a processed rightward drag was {startedCount}; expected greater than 0. Counts: Started={startedCount}, Changing={changingCount}, Ended={endedCount}.");
			Assert.That(changingCount, Is.GreaterThan(0),
				$"SwipeChanging count after a processed rightward drag was {changingCount}; expected greater than 0. Counts: Started={startedCount}, Changing={changingCount}, Ended={endedCount}.");
			Assert.That(endedCount, Is.GreaterThan(0),
				$"SwipeEnded count after a processed rightward drag was {endedCount}; expected greater than 0. Counts: Started={startedCount}, Changing={changingCount}, Ended={endedCount}.");
		});
	}
}
#endif

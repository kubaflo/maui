#if ANDROID
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37089 : _IssuesUITest
{
	public Issue37089(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "SwipeView stops tracking swipe when pointer leaves item bounds during active gesture";

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void SwipeContinuesTrackingOutsideItemBounds()
	{
		var androidApp = App as AppiumAndroidApp
			?? throw new InvalidOperationException("The Android Appium driver is required to perform the uninterrupted touch sequence.");
		var row = App.WaitForElement("Swipe this row");
		var rowRect = row.GetRect();
		Assert.That(rowRect.Width, Is.GreaterThan(0), "The realized SwipeView row must have a native width.");
		Assert.That(rowRect.Height, Is.GreaterThan(0), "The realized SwipeView row must have a native height.");
		Assert.That(
			App.FindElement("SwipeTelemetry").GetText(),
			Is.EqualTo("started=false|ended=false|count=-1"));

		var centerX = rowRect.CenterX();
		var centerY = rowRect.CenterY();
		var firstLeftX = centerX - rowRect.Width / 5;
		var secondLeftX = centerX - rowRect.Width * 2 / 5;
		var belowY = rowRect.Bottom + Math.Max(24, rowRect.Height / 4);

		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var leaveRowSequence = new ActionSequence(touchDevice, 0);
		leaveRowSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, centerX, centerY, TimeSpan.Zero));
		leaveRowSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		leaveRowSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, firstLeftX, centerY, TimeSpan.FromMilliseconds(300)));
		leaveRowSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, firstLeftX, belowY, TimeSpan.FromMilliseconds(300)));
		androidApp.Driver.PerformActions([leaveRowSequence]);

		var countBeforeOutsideMove = ReadChangeCount();
		Assert.That(countBeforeOutsideMove, Is.GreaterThan(0), "SwipeChanging must occur before the pointer leaves the row.");

		var moveOutsideSequence = new ActionSequence(touchDevice, 0);
		moveOutsideSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, secondLeftX, belowY, TimeSpan.FromMilliseconds(300)));
		androidApp.Driver.PerformActions([moveOutsideSequence]);
		var countAfterOutsideMove = ReadChangeCount();

		var finishSequence = new ActionSequence(touchDevice, 0);
		finishSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, secondLeftX, centerY, TimeSpan.FromMilliseconds(300)));
		finishSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([finishSequence]);

		Assert.That(
			App.WaitForTextToBePresentInElement("SwipeTelemetry", "ended=true", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"SwipeEnded telemetry was not published.");

		Assert.That(
			countAfterOutsideMove,
			Is.GreaterThan(countBeforeOutsideMove),
			$"Swipe tracking stopped outside item bounds: callback count remained {countAfterOutsideMove} while the held pointer moved horizontally outside the row.");
	}

	int ReadChangeCount()
	{
		var telemetry = App.FindElement("SwipeTelemetry").GetText()
			?? throw new InvalidOperationException("Swipe telemetry must be available during the gesture.");
		var values = telemetry.Split('|')
			.Select(part => part.Split('=', 2))
			.ToDictionary(part => part[0], part => part[1]);

		Assert.That(values["started"], Is.EqualTo("true"), "SwipeStarted must occur on the realized row.");
		Assert.That(values["ended"], Is.EqualTo("false"), "The pointer must remain held while tracking is measured.");
		return int.Parse(values["count"], System.Globalization.CultureInfo.InvariantCulture);
	}
}
#endif

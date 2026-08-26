#if ANDROID
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	const string EntryId = "Issue27665Entry";
	const string ImageId = "Issue27665Image";
	const string ScrollViewId = "Issue27665ScrollView";
	const string TelemetryId = "Issue27665Telemetry";

	public override string Issue => "Flickering when hiding and showing elements from ScrollView.Scrolled on Android";

	public Issue27665(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderRemainsHiddenDuringSustainedDownwardDrag()
	{
		App.SetOrientationPortrait();

		if (App is not AppiumAndroidApp androidApp)
		{
			Assert.Fail("Issue27665 requires the Android Appium runner.");
			return;
		}

		var driver = androidApp.Driver;
		if (driver is null)
		{
			Assert.Fail("Issue27665 requires an active Appium driver.");
			return;
		}

		var windowSize = driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The device must be in portrait orientation.");

		var entry = App.WaitForElement(EntryId);
		if (entry is null)
		{
			Assert.Fail("The header Entry was not rendered.");
			return;
		}

		var image = App.WaitForElement(ImageId);
		if (image is null)
		{
			Assert.Fail("The header Image was not rendered.");
			return;
		}

		Assert.That(entry.GetRect().Height, Is.GreaterThan(0), "The header Entry must initially be visible.");
		Assert.That(image.GetRect().Height, Is.GreaterThan(0), "The dotnet_bot.png Image must initially be visible.");

		var initialTelemetry = App.WaitForElement(TelemetryId);
		if (initialTelemetry is null)
		{
			Assert.Fail("The scroll telemetry was not rendered.");
			return;
		}

		Assert.That(initialTelemetry.GetText(), Does.Contain("Events=-1"), "A Scrolled callback occurred before the gesture.");

		var scrollView = App.WaitForElement(ScrollViewId);
		if (scrollView is null)
		{
			Assert.Fail("The ScrollView was not rendered.");
			return;
		}

		var scrollRect = scrollView.GetRect();
		var x = scrollRect.CenterX();
		var segment = windowSize.Height * 12 / 100;
		var startY = scrollRect.Y + (scrollRect.Height * 70 / 100);
		var endY = startY - (segment * 4);

		Assert.Multiple(() =>
		{
			Assert.That(x, Is.InRange(scrollRect.Left, scrollRect.Right), "Gesture X must be inside the ScrollView.");
			Assert.That(startY, Is.InRange(scrollRect.Top, scrollRect.Bottom), "Gesture start must be inside the ScrollView.");
			Assert.That(endY, Is.InRange(scrollRect.Top, scrollRect.Bottom), "Gesture end must be inside the ScrollView.");
		});

		PerformSustainedDrag(androidApp, x, startY, segment);

		var callbackObserved = App.WaitForTextToBePresentInElement(
			TelemetryId,
			"CallbackObserved=True",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(callbackObserved, Is.True, "The sustained drag did not raise ScrollView.Scrolled.");

		var positiveScrollObserved = App.WaitForTextToBePresentInElement(
			TelemetryId,
			"MaxPositive=True",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(positiveScrollObserved, Is.True, "The sustained drag never moved the ScrollView downward.");

		var telemetryElement = App.FindElement(TelemetryId);
		if (telemetryElement is null)
		{
			Assert.Fail("The post-gesture scroll telemetry was not available.");
			return;
		}

		var telemetry = telemetryElement.GetText();
		if (telemetry is null)
		{
			Assert.Fail("The post-gesture scroll telemetry was empty.");
			return;
		}

		var events = ReadMetric(telemetry, "Events");
		var hiddenTransitions = ReadMetric(telemetry, "Hidden");
		var shownTransitions = ReadMetric(telemetry, "Shown");
		var finalScrollY = ReadMetric(telemetry, "ScrollY");
		var maximumScrollY = ReadMetric(telemetry, "MaxScrollY");
		Assert.That(events, Is.GreaterThan(-1), "The scroll telemetry did not leave its pre-gesture sentinel.");
		Assert.That(hiddenTransitions, Is.GreaterThan(0), "The Scrolled callback did not hide the header.");

		var visibleEntries = App.FindElements(EntryId).Count;
		Assert.That(
			visibleEntries == 0 && shownTransitions == 0 && finalScrollY > 0.01,
			Is.True,
			$"Header entry reappeared during sustained downward drag: visible={visibleEntries}, hidden={hiddenTransitions}, shown={shownTransitions}, finalScrollY={finalScrollY:F2}, maxScrollY={maximumScrollY:F2}; expected visible=0, shown=0, finalScrollY>0.01.");
	}

	static void PerformSustainedDrag(AppiumAndroidApp app, int x, int startY, int segment)
	{
		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));

		for (var index = 1; index <= 4; index++)
		{
			dragSequence.AddAction(touchDevice.CreatePointerMove(
				CoordinateOrigin.Viewport,
				x,
				startY - (segment * index),
				TimeSpan.FromMilliseconds(250)));
		}

		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		app.Driver.PerformActions([dragSequence]);
	}

	static double ReadMetric(string telemetry, string name)
	{
		var prefix = $"{name}=";
		var token = telemetry.Split(';').Single(part => part.StartsWith(prefix, StringComparison.Ordinal));
		return double.Parse(token[prefix.Length..], CultureInfo.InvariantCulture);
	}
}
#endif

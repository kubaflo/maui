#if ANDROID
using System.Diagnostics;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	public Issue27665(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Flickering when hiding and showing elements from ScrollView.Scrolled on Android";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderRemainsNativelyHiddenDuringContinuousDrag()
	{
		if (App is not AppiumAndroidApp androidApp)
			throw new InvalidOperationException($"Invalid app type: expected {nameof(AppiumAndroidApp)}, received {App.GetType().Name}.");

		var entry = App.WaitForElement("HeaderEntry");
		var image = App.WaitForElement("HeaderImage");
		var scrollArea = App.WaitForElement("ScrollArea");
		var firstListItem = App.WaitForElement("FirstListItem");
		App.WaitForElement(AppiumQuery.ByXPath("//android.widget.TextView[@text=\"Element's list\"]"));
		var scrollRect = scrollArea.GetRect();
		var firstListItemRect = firstListItem.GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(entry.GetRect().Height, Is.GreaterThan(0), "The reported Entry was not rendered.");
			Assert.That(image.GetRect().Height, Is.GreaterThan(0), "The dotnet_bot.png Image was not rendered.");
			Assert.That(scrollRect.Height, Is.GreaterThan(0), "The reported ScrollView was not rendered.");
			Assert.That(firstListItem.GetText(), Is.EqualTo("Elemento 1"), "The first reported list item was not rendered.");
			Assert.That(firstListItemRect.Y, Is.GreaterThanOrEqualTo(scrollRect.Y), "The first list item started outside the ScrollView.");
			Assert.That(firstListItemRect.Bottom, Is.LessThanOrEqualTo(scrollRect.Bottom), "The first list item ended outside the ScrollView.");
		});

		var baseline = WaitForTelemetry(values =>
			Get(values, "attached") == 1 &&
			Get(values, "asset") == 1 &&
			Get(values, "frames") >= 3);

		Assert.Multiple(() =>
		{
			Assert.That(Get(baseline, "y"), Is.Zero, "The ScrollView did not start at its top offset.");
			Assert.That(Get(baseline, "callbacks"), Is.Zero, "Scrolled fired before the gesture.");
			Assert.That(Get(baseline, "callbackY"), Is.EqualTo(-1), "Post-trigger callback state was not initialized to its sentinel.");
			Assert.That(Get(baseline, "touchSlop"), Is.GreaterThan(0), "Android did not report a positive touch-slop threshold.");
			Assert.That(Get(baseline, "entryHidden"), Is.Zero, "The Entry was not continuously visible before input.");
			Assert.That(Get(baseline, "imageHidden"), Is.Zero, "The Image was not continuously visible before input.");
			Assert.That(Get(baseline, "entryReappear"), Is.Zero);
			Assert.That(Get(baseline, "imageReappear"), Is.Zero);
		});

		int baselineCallbacks = Get(baseline, "callbacks");
		var rootSize = androidApp.Driver.Manage().Window.Size;
		int x = scrollRect.CenterX();
		int startY = Math.Min(scrollRect.Bottom - 30, rootSize.Height * 80 / 100);
		int endY = Math.Max(scrollRect.Y + 30, rootSize.Height * 15 / 100);
		int segment = (startY - endY) / 3;
		Assert.That(segment, Is.GreaterThan(Get(baseline, "touchSlop")), "The ScrollView was too short for three touch-slop-exceeding drag segments.");

		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY - segment, TimeSpan.FromMilliseconds(320)));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(140)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY - (segment * 2), TimeSpan.FromMilliseconds(320)));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(140)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY - (segment * 3), TimeSpan.FromMilliseconds(320)));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(140)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([dragSequence]);

		var observed = WaitForTelemetry(values =>
			Get(values, "callbacks") > baselineCallbacks &&
			Get(values, "callbackY") >= 0 &&
			Get(values, "postFrames") >= 5 &&
			Get(values, "entryHidden") > 0 &&
			Get(values, "imageHidden") > 0);

		Assert.Multiple(() =>
		{
			Assert.That(Get(observed, "callbacks"), Is.GreaterThan(baselineCallbacks), "No post-trigger Scrolled callback was observed.");
			Assert.That(Get(observed, "callbackY"), Is.GreaterThanOrEqualTo(0), "The post-trigger callback did not report a scroll offset.");
			Assert.That(Get(observed, "postFrames"), Is.GreaterThanOrEqualTo(5), "Native frame telemetry did not advance after the trigger.");
			Assert.That(Get(observed, "entryHidden"), Is.GreaterThan(0), "The Entry never became natively hidden.");
			Assert.That(Get(observed, "imageHidden"), Is.GreaterThan(0), "The Image never became natively hidden.");
		});

		int entryReappearances = Get(observed, "entryReappear");
		int imageReappearances = Get(observed, "imageReappear");
		Assert.That(
			entryReappearances + imageReappearances,
			Is.Zero,
			$"Header native visibility flickered during one continuous drag: Entry transitions={entryReappearances}, Image transitions={imageReappearances}; expected both to remain zero.");
	}

	Dictionary<string, int> WaitForTelemetry(Func<Dictionary<string, int>, bool> predicate)
	{
		var timeout = Stopwatch.StartNew();
		Dictionary<string, int> values = [];

		while (timeout.Elapsed < TimeSpan.FromSeconds(10))
		{
			values = ReadTelemetry();
			if (predicate(values))
				return values;
		}

		Assert.Fail($"Native visibility telemetry did not reach the required state. Last telemetry: {Format(values)}");
		return values;
	}

	Dictionary<string, int> ReadTelemetry()
	{
		var telemetryElement = App.FindElement("ScrollArea");
		if (telemetryElement is null)
			throw new InvalidOperationException("The ScrollView telemetry target could not be found.");

		var telemetry = telemetryElement.GetAttribute<string>("content-desc");
		if (telemetry is null)
			throw new InvalidOperationException("The native visibility telemetry was null.");

		var values = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (string pair in telemetry.Split(';'))
		{
			string[] parts = pair.Split('=', 2);
			if (parts.Length != 2 || !int.TryParse(parts[1], out int value))
				throw new InvalidOperationException($"Invalid native visibility telemetry: {telemetry}");

			values.Add(parts[0], value);
		}

		return values;
	}

	static int Get(Dictionary<string, int> values, string key)
	{
		if (!values.TryGetValue(key, out int value))
			throw new InvalidOperationException($"Native visibility telemetry did not contain '{key}'.");

		return value;
	}

	static string Format(Dictionary<string, int> values) =>
		string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value}"));
}
#endif

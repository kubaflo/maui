#if ANDROID
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;
using PointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	const string ScrollView = "Issue27665ScrollView";

	public Issue27665(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Flickering when hiding or showing elements in the ScrollView Scrolled event";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderRemainsHiddenDuringHeldUpwardScroll()
	{
		if (App is not AppiumAndroidApp androidApp)
			throw new InvalidOperationException($"Invalid App Type For this Test: {App} Expected AppiumAndroidApp.");

		App.SetOrientationPortrait();

		var windowSize = androidApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Width, Is.LessThan(windowSize.Height), "The recorded scenario requires portrait orientation.");

		var entry = App.WaitForElement("Issue27665Entry");
		var image = App.WaitForElement("Issue27665Image");
		var scrollView = App.WaitForElement(ScrollView);
		var row = App.WaitForElement("Issue27665Row3");

		Assert.That(entry.GetRect().Height, Is.GreaterThan(0), "The native Entry must be visible before the gesture.");
		Assert.That(image.GetRect().Width, Is.GreaterThan(0), "The bundled image must have a rendered native width.");
		Assert.That(image.GetRect().Height, Is.GreaterThan(0), "The bundled image must have a rendered native height.");
		Assert.That(scrollView.GetRect().Height, Is.GreaterThan(0), "The native ScrollView must be attached and rendered.");
		Assert.That(row.GetRect().Height, Is.GreaterThan(0), "Elemento 3 must be rendered before starting the gesture.");
		WaitForTelemetry(scrollView, "Ready=True", "The native controls and bundled image did not become ready.");
		WaitForTelemetry(scrollView, "CallbackObserved=False", "Scroll telemetry changed before the recorded gesture.");

		PerformHeldUpwardDrag(androidApp, row.GetRect(), scrollView.GetRect(), windowSize.Height);

		WaitForTelemetry(scrollView, "CallbackObserved=True", "No post-sentinel ScrollView.Scrolled callback was observed.");
		WaitForTelemetry(scrollView, "DownObserved=True", "The native pointer-down was not observed.");
		WaitForTelemetry(scrollView, "MoveObserved=True", "The native pointer moves were not observed.");
		WaitForTelemetry(scrollView, "UpObserved=True", "The native pointer-up was not observed.");
		WaitForTelemetry(scrollView, "PositiveScroll=True", "The gesture did not produce a positive ScrollY.");
		WaitForTelemetry(scrollView, "GoneObserved=True", "The header controls did not become natively Gone.");

		var telemetry = scrollView.GetAttribute<string>("content-desc");
		if (telemetry is null)
			throw new AssertionException("The native ScrollView did not expose instrumentation telemetry.");

		bool returnedVisible = ReadTelemetryBoolean(telemetry, "ReturnedVisible");
		Assert.That(
			returnedVisible,
			Is.False,
			"Header became natively visible again during the held upward scroll.");
	}

	void WaitForTelemetry(IUIElement scrollView, string expected, string timeoutMessage)
	{
		App.WaitForElement(
			() =>
			{
				var telemetry = scrollView.GetAttribute<string>("content-desc");
				return telemetry is not null && telemetry.Contains(expected, StringComparison.Ordinal)
					? scrollView
					: null;
			},
			timeoutMessage,
			timeout: TimeSpan.FromSeconds(10));
	}

	static bool ReadTelemetryBoolean(string telemetry, string key)
	{
		string prefix = $"{key}=";
		int start = telemetry.IndexOf(prefix, StringComparison.Ordinal);
		if (start < 0)
			throw new AssertionException($"The native ScrollView telemetry did not contain {key}.");

		start += prefix.Length;
		int end = telemetry.IndexOf(';', start);
		if (end < 0)
			end = telemetry.Length;

		if (!bool.TryParse(telemetry.AsSpan(start, end - start), out bool value))
			throw new AssertionException($"The native ScrollView telemetry did not contain a boolean {key} value.");

		return value;
	}

	static void PerformHeldUpwardDrag(
		AppiumAndroidApp androidApp,
		System.Drawing.Rectangle rowRect,
		System.Drawing.Rectangle scrollRect,
		int windowHeight)
	{
		int x = rowRect.CenterX();
		int startY = rowRect.CenterY();
		int minimumY = Math.Max(1, scrollRect.Y + 1);
		int segment = windowHeight * 18 / 100;
		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);

		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(250)));

		for (int i = 1; i <= 4; i++)
		{
			int targetY = Math.Max(minimumY, startY - (segment * i));
			dragSequence.AddAction(touchDevice.CreatePointerMove(
				CoordinateOrigin.Viewport,
				x,
				targetY,
				TimeSpan.FromMilliseconds(320)));
			dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(140)));
		}

		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([dragSequence]);
	}
}
#endif

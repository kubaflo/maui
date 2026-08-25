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
	public override string Issue => "Flickering when hiding or showing elements from ScrollView.Scrolled";

	public Issue27665(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void SustainedUpwardDragDoesNotReverseScrollOffsetWhenHeaderVisibilityChanges()
	{
		if (App is not AppiumAndroidApp androidApp)
			throw new InvalidOperationException($"Invalid app type for this Android test: {App}.");

		App.SetOrientationPortrait();

		var headerEntry = App.WaitForElement("HeaderEntry");
		var headerImage = App.WaitForElement("HeaderImage");
		var scrollView = App.WaitForElement("Issue27665ScrollView");
		var dragTarget = App.WaitForElement("Elemento7");
		var windowSize = androidApp.Driver.Manage().Window.Size;
		var scrollViewRect = scrollView.GetRect();
		var dragTargetRect = dragTarget.GetRect();
		var visibleTargetRect = System.Drawing.Rectangle.Intersect(scrollViewRect, dragTargetRect);

		Assert.Multiple(() =>
		{
			Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The reported scenario should run in portrait.");
			Assert.That(headerEntry.GetRect().Height, Is.GreaterThan(0), "The green header Entry should be attached and measured.");
			Assert.That(headerImage.GetRect().Width, Is.GreaterThan(0), "The bundled header image should be attached and measured.");
			Assert.That(headerImage.GetRect().Height, Is.GreaterThan(0), "The bundled header image should have rendered native height.");
			Assert.That(scrollViewRect.Height, Is.GreaterThan(0), "The ScrollView should be attached and measured.");
			Assert.That(dragTarget.GetText(), Is.EqualTo("Elemento 7"), "The intended drag target should exist.");
			Assert.That(visibleTargetRect.Width, Is.GreaterThan(0), "Elemento 7 should be horizontally inside the ScrollView.");
			Assert.That(visibleTargetRect.Height, Is.EqualTo(dragTargetRect.Height), "Elemento 7 should be fully visible at the recorded drag location.");
		});

		PerformSustainedUpwardDrag(androidApp, dragTargetRect);

		var calibration = ReadMeasurements();
		Assert.Multiple(() =>
		{
			Assert.That(calibration.MutationEnabled, Is.Zero);
			Assert.That(calibration.Samples, Is.Not.Empty, "The calibration drag should raise Scrolled after the samples were cleared.");
			Assert.That(calibration.Samples.Any(value => value > 0), Is.True, "The calibration drag should prove that the native surface is scrollable.");
			Assert.That(FindFirstReturnToTopIndex(calibration.Samples), Is.EqualTo(-1), "The offset should not return to the top during the calibration drag.");
		});

		App.Tap("ScrollMeasurements");
		App.RetryAssert(() =>
		{
			var reset = ReadMeasurements();
			Assert.Multiple(() =>
			{
				Assert.That(reset.MutationEnabled, Is.EqualTo(1));
				Assert.That(reset.ResetY, Is.EqualTo(0).Within(0.01), "The calibration hierarchy should return to the top before enabling header mutation.");
				Assert.That(reset.Samples, Is.Empty);
			});
		});

		PerformSustainedUpwardDrag(androidApp, App.WaitForElement("Elemento7").GetRect());

		App.RetryAssert(() =>
		{
			Assert.That(ReadMeasurements().Samples.Any(value => value > 0), Is.True, "The recorded drag should reach a positive ScrollY.");
		});
		App.RetryAssert(() =>
		{
			Assert.Multiple(() =>
			{
				Assert.That(App.FindElements("HeaderEntry"), Is.Empty, "The native Entry should no longer be visible.");
				Assert.That(App.FindElements("HeaderImage"), Is.Empty, "The native Image should no longer be visible.");
			});
		});
		var recorded = ReadMeasurements();
		var returnToTopIndex = FindFirstReturnToTopIndex(recorded.Samples);
		Assert.That(
			returnToTopIndex,
			Is.EqualTo(-1),
			"The header became visible again during one sustained upward drag after scrolling had started.");
	}

	void PerformSustainedUpwardDrag(AppiumAndroidApp androidApp, System.Drawing.Rectangle target)
	{
		var windowHeight = androidApp.Driver.Manage().Window.Size.Height;
		var segmentDistance = (int)Math.Round(windowHeight * 0.18, MidpointRounding.AwayFromZero);
		var x = target.CenterX();
		var y = target.CenterY();
		var touchDevice = new PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);

		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, y, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(250)));
		for (var segment = 1; segment <= 3; segment++)
		{
			y = Math.Clamp(y - segmentDistance, 1, windowHeight - 2);
			dragSequence.AddAction(touchDevice.CreatePointerMove(
				CoordinateOrigin.Viewport,
				x,
				y,
				TimeSpan.FromMilliseconds(320)));
			dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(140)));
		}
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([dragSequence]);
	}

	Measurements ReadMeasurements()
	{
		var measurementsElement = App.FindElement("ScrollMeasurements");
		Assert.That(measurementsElement, Is.Not.Null, "The scroll measurement element should exist.");
		if (measurementsElement is null)
			throw new InvalidOperationException("The scroll measurement element was not found.");

		var text = measurementsElement.GetText();
		Assert.That(text, Is.Not.Null.And.Not.Empty, "Scroll measurements should contain sampled values.");
		if (string.IsNullOrEmpty(text))
			throw new InvalidOperationException("Scroll measurements were empty.");

		var values = text.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(part => part.Split('=', 2))
			.ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

		return new Measurements(
			int.Parse(values["MutationEnabled"], CultureInfo.InvariantCulture),
			double.Parse(values["ResetY"], CultureInfo.InvariantCulture),
			values["Samples"].Length == 0
				? []
				: values["Samples"].Split(',').Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray());
	}

	static int FindFirstReturnToTopIndex(IReadOnlyList<double> samples)
	{
		var hasScrolled = false;
		for (var index = 0; index < samples.Count; index++)
		{
			if (hasScrolled && samples[index] <= 0)
				return index;

			hasScrolled |= samples[index] > 0;
		}

		return -1;
	}

	readonly record struct Measurements(
		int MutationEnabled,
		double ResetY,
		IReadOnlyList<double> Samples);
}
#endif

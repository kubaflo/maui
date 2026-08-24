#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34560 : _IssuesUITest
{
	public Issue34560(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Switch iOS Liquid glass rendering issue";

	[Test]
	[Category(UITestCategories.Switch)]
	public void DefaultSwitchMatchesNativeUISwitchAfterToggle()
	{
		App.SetOrientationPortrait();

		var initialStatus = App.WaitForElement("MeasurementLabel").GetText();
		if (initialStatus?.StartsWith("UNSUPPORTED:", StringComparison.Ordinal) == true)
			return;

		App.WaitForTextToBePresentInElement("MeasurementLabel", "OFF off=", TimeSpan.FromSeconds(10));
		var offStatus = App.FindElement("MeasurementLabel").GetText() ?? string.Empty;
		var offMismatch = ParseMetric(offStatus, "off=");
		var tolerance = ParseMetric(offStatus, "tolerance=");
		Assert.That(offMismatch, Is.LessThanOrEqualTo(tolerance),
			$"Off-state MAUI Switch must validate the native-reference pixel oracle. Measurement: {offStatus}");
		Assert.That(offStatus, Does.Contain("generation=-1"), "Callback generation must retain its sentinel before the tap.");

		var windowWidth = ParseMetric(offStatus, "windowWidth=");
		var windowHeight = ParseMetric(offStatus, "windowHeight=");
		var frameX = ParseMetric(offStatus, "frameX=");
		var frameY = ParseMetric(offStatus, "frameY=");
		var frameWidth = ParseMetric(offStatus, "frameWidth=");
		var frameHeight = ParseMetric(offStatus, "frameHeight=");
		var pixelCount = ParseMetric(offStatus, "pixels=");
		Assert.That(windowWidth, Is.LessThan(windowHeight), "The rendering comparison must run in portrait orientation.");

		var switchRect = App.WaitForElement("IssueSwitch").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(switchRect.Width, Is.GreaterThan(0), "The intended Switch must have a visible width.");
			Assert.That(switchRect.Height, Is.GreaterThan(0), "The intended Switch must have a visible height.");
			Assert.That(frameX, Is.GreaterThanOrEqualTo(0), "The compared Switch frame must start inside the active window.");
			Assert.That(frameY, Is.GreaterThanOrEqualTo(0), "The compared Switch frame must start inside the active window.");
			Assert.That(frameX + frameWidth, Is.LessThanOrEqualTo(windowWidth), "Every compared Switch pixel must be inside the active window width.");
			Assert.That(frameY + frameHeight, Is.LessThanOrEqualTo(windowHeight), "Every compared Switch pixel must be inside the active window height.");
			Assert.That(pixelCount, Is.GreaterThan(0), "The native rendering comparison must contain pixels.");
		});

		App.Tap("IssueSwitch");
		App.WaitForTextToBePresentInElement("MeasurementLabel", "ON off=", TimeSpan.FromSeconds(10));
		var onStatus = App.FindElement("MeasurementLabel").GetText() ?? string.Empty;

		Assert.Multiple(() =>
		{
			Assert.That(onStatus, Does.Contain("generation=0"), "A new Toggled callback must advance from the sentinel after the tap.");
			Assert.That(onStatus, Does.Contain("toggled=true"), "The Appium tap must change IsToggled to true.");
			Assert.That(ParseMetric(onStatus, "frameX="), Is.EqualTo(ParseMetric(offStatus, "frameX=")).Within(0.5), "The same target frame must be measured after the tap.");
			Assert.That(ParseMetric(onStatus, "frameY="), Is.EqualTo(ParseMetric(offStatus, "frameY=")).Within(0.5), "The same target frame must be measured after the tap.");
			Assert.That(ParseMetric(onStatus, "frameWidth="), Is.EqualTo(ParseMetric(offStatus, "frameWidth=")).Within(0.5), "The target width must remain unchanged after the tap.");
			Assert.That(ParseMetric(onStatus, "frameHeight="), Is.EqualTo(ParseMetric(offStatus, "frameHeight=")).Within(0.5), "The target height must remain unchanged after the tap.");
			Assert.That(ParseMetric(onStatus, "pixels="), Is.EqualTo(pixelCount), "The same target pixel region must be measured after the tap.");
		});

		var onMismatch = ParseMetric(onStatus, "on=");
		tolerance = ParseMetric(onStatus, "tolerance=");
		Assert.That(onMismatch, Is.LessThanOrEqualTo(tolerance),
			$"MAUI on-state Switch rendering differs from native UISwitch reference: {onStatus}");
	}

	static double ParseMetric(string measurement, string name)
	{
		var start = measurement.IndexOf(name, StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Measurement '{name}' was not reported: {measurement}");
		start += name.Length;
		var end = measurement.IndexOf(' ', start);
		if (end < 0)
			end = measurement.Length;

		return double.Parse(measurement[start..end], CultureInfo.InvariantCulture);
	}
}
#endif

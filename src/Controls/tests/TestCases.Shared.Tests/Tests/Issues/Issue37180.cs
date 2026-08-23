#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37180 : _IssuesUITest
{
	public Issue37180(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Label Background does not reset to the transparent default when set to null";

	[Test]
	[Category(UITestCategories.Label)]
	public void ClearingBackgroundRestoresNativeTransparentDefault()
	{
		App.WaitForElement("BackgroundLabel");
		App.WaitForElement("MeasurementLabel");
		App.WaitForTextToBePresentInElement("MeasurementLabel", "Transition=-1;Instruction=0.000;Initial=0.000");

		var initialMeasurement = App.FindElement("MeasurementLabel").GetText()
			?? throw new InvalidOperationException("MeasurementLabel text was null after the initial state was reported.");
		Assert.That(ReadValue(initialMeasurement, "Instruction"), Is.EqualTo(0).Within(0.001),
			"The untouched instruction Label should prove that the native platform default is transparent.");
		Assert.That(ReadValue(initialMeasurement, "Initial"), Is.EqualTo(0).Within(0.001),
			"The affected Label should initially use the same transparent native platform default.");

		App.Tap("SetRedButton");
		App.WaitForTextToBePresentInElement("MeasurementLabel", "Transition=1");

		var redMeasurement = App.FindElement("MeasurementLabel").GetText()
			?? throw new InvalidOperationException("MeasurementLabel text was null after the red transition was reported.");
		Assert.That(ReadValue(redMeasurement, "Transition"), Is.EqualTo(1),
			"The red Button Clicked callback should complete before its result is evaluated.");
		Assert.That(ReadValue(redMeasurement, "Red"), Is.EqualTo(1).Within(0.001),
			"Setting the public Label.Background to red should make its native background opaque.");
		Assert.That(App.FindElement("BackgroundLabel").GetText(), Is.EqualTo("Label Background Test"),
			"The affected Label should remain visible with the reported identity before its Background is cleared.");

		App.Tap("SetNullButton");
		App.WaitForTextToBePresentInElement("MeasurementLabel", "Transition=2");

		var nullMeasurement = App.FindElement("MeasurementLabel").GetText()
			?? throw new InvalidOperationException("MeasurementLabel text was null after the null transition was reported.");
		Assert.That(ReadValue(nullMeasurement, "Transition"), Is.EqualTo(2),
			"The null Button Clicked callback should complete before its result is evaluated.");
		Assert.That(ReadTextValue(nullMeasurement, "ManagedNull"), Is.EqualTo("True"),
			"The public Label.Background should be null after the recorded tap.");

		var nullAlpha = ReadValue(nullMeasurement, "Null");
		Assert.That(nullAlpha, Is.EqualTo(0).Within(0.001),
			$"Label native background remained opaque after Background was cleared; observed alpha {nullAlpha.ToString("F3", CultureInfo.InvariantCulture)}, observed RGBA {ReadTextValue(nullMeasurement, "NullRGBA")}, expected alpha 0.");
	}

	static double ReadValue(string measurement, string name) =>
		double.Parse(ReadTextValue(measurement, name), CultureInfo.InvariantCulture);

	static string ReadTextValue(string measurement, string name)
	{
		var prefix = name + "=";
		foreach (var value in measurement.Split(';'))
		{
			if (value.StartsWith(prefix, StringComparison.Ordinal))
				return value[prefix.Length..];
		}

		Assert.Fail($"Measurement '{name}' was missing from '{measurement}'.");
		return string.Empty;
	}
}
#endif

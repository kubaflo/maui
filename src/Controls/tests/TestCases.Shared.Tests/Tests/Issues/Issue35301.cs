#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling on default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DefaultSelectedItemShouldNotUseWinUISelectionChrome()
	{
		App.WaitForElement("Apple");
		Assert.That(
			App.WaitForTextToBePresentInElement("InitialMeasurementLabel", "Identity=", TimeSpan.FromSeconds(10)),
			Is.True,
			"The first realized native item was not measured before selection.");

		var initialMeasurement = ParseMeasurement(GetRequiredText("InitialMeasurementLabel"));
		Assert.That(GetValue(initialMeasurement, "Identity"), Is.EqualTo("Apple"));
		Assert.That(ParseNumber(initialMeasurement, "Width"), Is.GreaterThan(0));
		Assert.That(ParseNumber(initialMeasurement, "Height"), Is.GreaterThan(0));
		Assert.That(GetValue(initialMeasurement, "Selected"), Is.EqualTo("False"));
		double initialRadius = ParseNumber(initialMeasurement, "Radius");
		Assert.That(GetValue(initialMeasurement, "Indicator"), Is.EqualTo("False"));
		Assert.That(GetValue(initialMeasurement, "Index"), Is.EqualTo("-1"));
		Assert.That(GetValue(initialMeasurement, "Sequence"), Is.EqualTo("0"));
		string initialTheme = GetValue(initialMeasurement, "Theme");

		App.Tap("Apple");
		Assert.That(
			App.WaitForTextToBePresentInElement("SelectionIndexLabel", "0", TimeSpan.FromSeconds(10)),
			Is.True,
			"SelectionChanged did not report Apple at index 0.");

		App.Tap("CheckStyleButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionSequenceLabel", "1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The native inspection did not run after selection.");
		Assert.That(
			App.WaitForTextToBePresentInElement("CurrentMeasurementLabel", "Selected=True", TimeSpan.FromSeconds(10)),
			Is.True,
			"The native Apple ListViewItem did not become selected.");

		var selectedMeasurement = ParseMeasurement(GetRequiredText("CurrentMeasurementLabel"));
		Assert.That(GetValue(selectedMeasurement, "Identity"), Is.EqualTo("Apple"));
		Assert.That(ParseNumber(selectedMeasurement, "Width"), Is.GreaterThan(0));
		Assert.That(ParseNumber(selectedMeasurement, "Height"), Is.GreaterThan(0));
		Assert.That(GetValue(selectedMeasurement, "Selected"), Is.EqualTo("True"));
		Assert.That(GetValue(selectedMeasurement, "Index"), Is.EqualTo("0"));
		Assert.That(GetValue(selectedMeasurement, "Sequence"), Is.EqualTo("1"));
		Assert.That(GetValue(selectedMeasurement, "Theme"), Is.EqualTo(initialTheme));

		double selectedRadius = ParseNumber(selectedMeasurement, "Radius");
		string selectedIndicator = GetValue(selectedMeasurement, "Indicator");
		string selectedBackground = GetValue(selectedMeasurement, "SelectedBackground");
		string failureDetails = $"Default CollectionView selected Apple rendered unexpected WinUI selection chrome: initial radius={initialRadius:0.###}, selected radius={selectedRadius:0.###}, indicator={selectedIndicator}, selected background={selectedBackground}, selected=True, index=0, sequence=1, expected unchanged radius, indicator=False, and selected background=False.";
		Assert.That(selectedRadius, Is.EqualTo(initialRadius).Within(0.01), failureDetails);
		Assert.That(selectedIndicator, Is.EqualTo("False"), failureDetails);
		Assert.That(selectedBackground, Is.EqualTo("False"), failureDetails);
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
		{
			Assert.Fail($"Element '{automationId}' was not found.");
			return string.Empty;
		}

		var text = element.GetText();
		if (text is null)
		{
			Assert.Fail($"Element '{automationId}' did not provide text.");
			return string.Empty;
		}

		return text;
	}

	static Dictionary<string, string> ParseMeasurement(string measurement)
	{
		var values = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string component in measurement.Split(';'))
		{
			string[] pair = component.Split('=', 2);
			Assert.That(pair, Has.Length.EqualTo(2), $"Invalid native measurement component: '{component}'.");
			values.Add(pair[0], pair[1]);
		}

		return values;
	}

	static string GetValue(IReadOnlyDictionary<string, string> measurement, string key)
	{
		Assert.That(measurement.ContainsKey(key), Is.True, $"Native measurement did not contain '{key}'.");
		return measurement[key];
	}

	static double ParseNumber(IReadOnlyDictionary<string, string> measurement, string key) =>
		double.Parse(GetValue(measurement, key), NumberStyles.Float, CultureInfo.InvariantCulture);
}
#endif

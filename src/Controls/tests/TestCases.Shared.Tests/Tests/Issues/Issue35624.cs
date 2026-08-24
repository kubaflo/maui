#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	const double ExpectedCharacterSpacing = 20;

	public Issue35624(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "SearchHandler CharacterSpacing property is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToEnteredText()
	{
		App.WaitForElement("Issue35624Reference");

		var appWindow = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
		if (appWindow is null)
			throw new AssertionException("The iOS application window was not found.");

		var windowBounds = appWindow.GetRect();
		Assert.That(windowBounds.Height, Is.GreaterThan(windowBounds.Width),
			"The Issue35624 scenario must run in portrait orientation.");

		var searchHandler = App.GetShellSearchHandler();
		var initialMeasurement = GetRequiredText("Issue35624Measurement");
		Assert.That(initialMeasurement, Is.EqualTo("Measurement: pending"),
			"Native measurement must not contain a passing value before text entry.");

		searchHandler.Tap();
		searchHandler.SendKeys("SPACING");

		var queryObserved = App.WaitForTextToBePresentInElement(
			"Issue35624Query", "Query: SPACING", timeout: TimeSpan.FromSeconds(5));
		Assert.That(queryObserved, Is.True, "SearchHandler.Query did not change to SPACING after text entry.");
		Assert.That(GetRequiredText("Issue35624Query"), Is.EqualTo("Query: SPACING"),
			"SearchHandler.Query must exactly match the entered text.");

		var measurementCompleted = App.WaitForTextToBePresentInElement(
			"Issue35624Measurement", "Measurement: complete", timeout: TimeSpan.FromSeconds(5));
		Assert.That(measurementCompleted, Is.True,
			$"Native measurement did not complete. Current state: {GetRequiredText("Issue35624Measurement")}");

		var measurement = GetRequiredText("Issue35624Measurement");
		var searchIdentity = ReadMeasurementValue(measurement, "Search native id: ");
		Assert.That(searchIdentity, Is.Not.EqualTo("0"),
			"The attached SearchHandler UITextField must have a native identity.");
		Assert.That(ReadMeasurementValue(measurement, "Search attributed text: "), Is.EqualTo("SPACING"),
			"The measured UITextField must contain the text entered through Appium.");
		Assert.That(ReadMeasurementValue(measurement, "Reference attributed text: "), Is.EqualTo("SPACING"),
			"The native Label oracle must contain the expected reference text.");

		var referenceKerning = ReadKerning(measurement, "Reference kerning: ");
		Assert.That(referenceKerning, Is.EqualTo(ExpectedCharacterSpacing).Within(0.01),
			"The Label native kerning did not match its configured CharacterSpacing.");

		var searchKerning = ReadKerning(measurement, "Search kerning: ");
		Assert.That(searchKerning, Is.EqualTo(ExpectedCharacterSpacing).Within(0.01),
			"SearchHandler native kerning did not match its configured CharacterSpacing.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.FindElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}

	static double ReadKerning(string measurement, string prefix)
	{
		return double.Parse(ReadMeasurementValue(measurement, prefix), CultureInfo.InvariantCulture);
	}

	static string ReadMeasurementValue(string measurement, string prefix)
	{
		foreach (var part in measurement.Split('|'))
		{
			if (part.StartsWith(prefix, StringComparison.Ordinal))
				return part[prefix.Length..];
		}

		throw new AssertionException($"Native measurement did not contain '{prefix}'. Actual: {measurement}");
	}
}
#endif

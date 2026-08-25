#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	public Issue35624(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "SearchHandler CharacterSpacing is not applied";

	[Test]
	[Category(UITestCategories.SearchBar)]
	public void SearchHandlerAppliesCharacterSpacingToQueryText()
	{
		var measurementElement = App.WaitForElement("Issue35624Measurement");
		Assert.That(
			measurementElement.GetText(),
			Is.EqualTo("waiting"),
			"The native character-spacing measurement should not run before text input.");

		var searchHandler = App.GetShellSearchHandler();
		searchHandler.Tap();
		searchHandler.SendKeys("MAUI TEST");

		Assert.That(
			searchHandler.GetText(),
			Does.Contain("MAUI TEST"),
			"The native Shell search field should contain the text entered through Appium.");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624InputStatus",
				"Input received: MAUI TEST",
				timeout: TimeSpan.FromSeconds(15)),
			Is.True,
			"The SearchHandler Query callback did not observe the entered text.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35624Measurement",
				"Reference:",
				timeout: TimeSpan.FromSeconds(15)),
			Is.True,
			"The native character-spacing measurement did not complete after text input.");

		var measurement = measurementElement.GetText();
		if (measurement is null)
		{
			Assert.Fail("The native character-spacing measurement element did not expose text.");
			return;
		}

		var referenceKerning = ReadMeasurement(measurement, "Reference");
		Assert.That(
			referenceKerning,
			Is.EqualTo(10).Within(0.01),
			$"The reference Label did not expose the arranged native kerning: {referenceKerning}");

		var searchHandlerKerning = ReadMeasurement(measurement, "SearchHandler");
		Assert.That(
			searchHandlerKerning,
			Is.EqualTo(10).Within(0.01),
			$"SearchHandler CharacterSpacing was not applied: requested 10; measured {searchHandlerKerning}");
	}

	static double ReadMeasurement(string measurement, string name)
	{
		var prefix = $"{name}: ";
		foreach (var part in measurement.Split(';'))
		{
			var valueText = part.Trim();
			if (valueText.StartsWith(prefix, StringComparison.Ordinal) &&
				double.TryParse(
					valueText[prefix.Length..],
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out var value))
			{
				return value;
			}
		}

		Assert.Fail($"Native {name} kerning was unavailable: {measurement}");
		return double.NaN;
	}
}
#endif

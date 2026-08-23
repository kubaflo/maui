#if IOS
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	public override string Issue => "SearchHandler CharacterSpacing property is not applied";

	public Issue35624(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToEnteredText()
	{
		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(ScreenOrientation.Portrait));

		App.WaitForElement("Issue35624PageStatus");
		App.WaitForElement("Issue35624Reference");
		Assert.That(App.WaitForTextToBePresentInElement("Issue35624ResultStatus", "reference=10"), Is.True);

		var initialMeasurements = App.FindElement("Issue35624ResultStatus").GetText() ?? string.Empty;
		Assert.That(ParseMeasurement(initialMeasurements, "generation"), Is.EqualTo(-1));
		Assert.That(ParseMeasurement(initialMeasurements, "search"), Is.EqualTo(-1));
		var referenceKerning = ParseMeasurement(initialMeasurements, "reference");
		Assert.That(referenceKerning, Is.EqualTo(10).Within(0.01));

		var searchFieldQuery = AppiumQuery.ByXPath("//XCUIElementTypeSearchField");
		var searchField = App.WaitForElement(searchFieldQuery);
		searchField.Click();
		App.EnterText(searchFieldQuery, "SPACING");

		Assert.That(App.WaitForTextToBePresentInElement("Issue35624QueryStatus", "query=SPACING"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue35624ResultStatus", "generation=1"), Is.True);
		Assert.That(App.WaitForElement(searchFieldQuery).GetText(), Is.EqualTo("SPACING"));

		var searchKerning = ParseMeasurement(App.FindElement("Issue35624ResultStatus").GetText() ?? string.Empty, "search");
		Assert.That(searchKerning, Is.EqualTo(10).Within(0.01),
			$"SearchHandler native kerning did not equal 10 after entering SPACING; search={searchKerning.ToString(CultureInfo.InvariantCulture)}, expected=10, reference={referenceKerning.ToString(CultureInfo.InvariantCulture)}");
	}

	static double ParseMeasurement(string measurement, string name)
	{
		var prefix = $"{name}=";
		var start = measurement.IndexOf(prefix, StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0));
		start += prefix.Length;
		var end = measurement.IndexOf(';', start);
		var value = end < 0 ? measurement[start..] : measurement[start..end];
		return double.Parse(value, CultureInfo.InvariantCulture);
	}
}
#endif

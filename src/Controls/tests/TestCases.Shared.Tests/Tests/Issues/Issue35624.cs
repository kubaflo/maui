#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35624 : _IssuesUITest
{
	const double Tolerance = 0.01;

	public Issue35624(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "SearchHandler CharacterSpacing is not applied";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerAppliesCharacterSpacingToEnteredText()
	{
		Assert.That(
			ReadText("Issue35624InitialState"),
			Is.EqualTo("Query=<empty>; Callback=-1"));

		var configuredSpacing = ReadMeasurement("Issue35624ConfiguredSpacing", "ConfiguredSpacing: ");
		var defaultKerning = ReadMeasurement("Issue35624DefaultKerning", "DefaultKerning: ");
		var referenceKerning = ReadMeasurement("Issue35624ReferenceKerning", "ReferenceKerning: ");

		Assert.Multiple(() =>
		{
			Assert.That(defaultKerning, Is.EqualTo(0).Within(Tolerance));
			Assert.That(referenceKerning, Is.EqualTo(configuredSpacing).Within(Tolerance));
			Assert.That(Math.Abs(referenceKerning - defaultKerning), Is.GreaterThan(Tolerance));
		});

		var searchField = App.GetShellSearchHandler();
		if (searchField is null)
			throw new AssertionException("The native Shell search field was not found.");

		searchField.Tap();
		searchField.SendKeys("SPACING");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35624Query", "Query: SPACING", TimeSpan.FromSeconds(10)),
			Is.True,
			"SearchHandler.Query did not receive the entered text.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35624Callback", "Callback: 1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The post-input native inspection callback did not run.");
		Assert.That(
			ReadText("Issue35624InitialState"),
			Is.EqualTo("SearchField=True; Text=SPACING; Callback=1"),
			"The post-input inspection did not find the entered text in the native Shell search field.");

		var actualKerning = ReadMeasurement("Issue35624SearchKerning", "SearchKerning: ");
		Assert.That(
			actualKerning,
			Is.EqualTo(configuredSpacing).Within(Tolerance),
			$"SearchHandler rendered SPACING with native kerning {actualKerning}, configured {configuredSpacing}, reference {referenceKerning}, tolerance {Tolerance}.");
	}

	double ReadMeasurement(string automationId, string prefix)
	{
		var text = ReadText(automationId);
		if (!text.StartsWith(prefix, StringComparison.Ordinal))
			throw new AssertionException($"Measurement '{automationId}' did not start with '{prefix}'. Actual: '{text}'.");
		if (!double.TryParse(text[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
			throw new AssertionException($"Measurement '{automationId}' was not numeric. Actual: '{text}'.");

		return value;
	}

	string ReadText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' had no text.");

		return text;
	}
}
#endif

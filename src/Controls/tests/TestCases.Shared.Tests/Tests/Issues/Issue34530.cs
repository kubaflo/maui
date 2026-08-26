#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34530 : _IssuesUITest
{
	public override string Issue => "TextToSpeech.Default.GetLocalesAsync does not return Lithuanian on iOS";

	public Issue34530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Picker)]
	public void TextToSpeechLocalesContainLithuanianAfterPickerFocus()
	{
		var initialSummaryElement = App.WaitForElement("LocaleSummary");
		var initialSummary = initialSummaryElement.GetText();
		Assert.That(initialSummary, Is.EqualTo("Returned locales will appear here."));

		App.WaitForElement("LocalePicker");
		App.Tap("LocalePicker");

		var summaryElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("LocaleSummary");
			if (element is null)
				return null;

			var text = element.GetText();
			return text is not null && text.StartsWith("Returned locales (", StringComparison.Ordinal) ? element : null;
		}, "Timed out waiting for TextToSpeech.Default.GetLocalesAsync to complete", TimeSpan.FromSeconds(30));

		var summary = summaryElement.GetText();
		if (summary is null)
			throw new AssertionException("The rendered locale summary had no text.");

		var summaryLines = summary.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		Assert.That(summaryLines, Is.Not.Empty);
		var countPrefix = "Returned locales (";
		var countSuffix = "):";
		Assert.That(summaryLines[0], Does.StartWith(countPrefix).And.EndWith(countSuffix));
		var countText = summaryLines[0][countPrefix.Length..^countSuffix.Length];
		Assert.That(
			int.TryParse(countText, out var completedCount),
			Is.True,
			$"The returned locale count was invalid: {summaryLines[0]}");
		Assert.That(completedCount, Is.GreaterThanOrEqualTo(0), "The locale query did not publish a completed collection.");

		var localeDescriptions = summaryLines.Skip(1).ToArray();
		Assert.That(localeDescriptions, Has.Length.EqualTo(completedCount));

		var languages = localeDescriptions
			.Select(description => description.Split(" - ", 2, StringSplitOptions.None)[0])
			.ToArray();
		var hasLithuanian = languages.Any(language =>
			string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
			language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));

		Assert.That(
			hasLithuanian,
			Is.True,
			$"Lithuanian locale was absent from TextToSpeech.Default.GetLocalesAsync after Picker focus. Returned languages: {string.Join(", ", languages)}");
	}
}
#endif

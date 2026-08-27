#if WINDOWS
using System.Globalization;
using System.Text;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30090 : _IssuesUITest
{
	public Issue30090(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "DatePicker does not update its format when the culture changes at runtime";

	[Test]
	[Category(UITestCategories.DatePicker)]
	public void DatePickerUpdatesFormatAfterRuntimeCultureChange()
	{
		var testDate = new DateTime(2026, 12, 24);
		var englishCulture = new CultureInfo("en-US");
		var frenchCulture = new CultureInfo("fr-FR");
		var expectedInitialText = testDate.ToString("d", englishCulture);
		var expectedUpdatedText = testDate.ToString("d", frenchCulture);

		var initialCultureLabel = App.WaitForElement("InitialCultureLabel");
		if (initialCultureLabel is null)
		{
			Assert.Fail("The initial culture marker was not found.");
			return;
		}

		Assert.That(initialCultureLabel.GetText(), Is.EqualTo("Initial culture: en-US"));

		var initialDatePicker = App.WaitForElement("IssueDatePicker");
		if (initialDatePicker is null)
		{
			Assert.Fail("The DatePicker was not found before the culture change.");
			return;
		}

		var initialRawText = initialDatePicker.GetText();
		if (initialRawText is null)
		{
			Assert.Fail("The DatePicker did not expose rendered text before the culture change.");
			return;
		}

		var initialText = RemoveFormattingCharacters(initialRawText);
		Assert.That(initialText, Is.EqualTo(expectedInitialText),
			$"The DatePicker baseline was '{initialRawText}', expected '{expectedInitialText}'.");

		App.Tap("ChangeCultureButton");
		App.WaitForElement("Current culture: fr-FR");

		var cultureStatusLabel = App.WaitForElement("CultureStatusLabel");
		if (cultureStatusLabel is null)
		{
			Assert.Fail("The post-click culture marker was not found.");
			return;
		}

		Assert.That(cultureStatusLabel.GetText(), Is.EqualTo("Current culture: fr-FR"));

		var updatedDatePicker = App.WaitForElement("IssueDatePicker");
		if (updatedDatePicker is null)
		{
			Assert.Fail("The DatePicker was not found after the culture change.");
			return;
		}

		var updatedRawText = updatedDatePicker.GetText();
		if (updatedRawText is null)
		{
			Assert.Fail("The DatePicker did not expose rendered text after the culture change.");
			return;
		}

		var updatedText = RemoveFormattingCharacters(updatedRawText);
		Assert.That(updatedText, Is.Not.EqualTo(initialText),
			$"DatePicker rendered text did not update after runtime culture change. Initial and updated text were both '{updatedRawText}'; expected '{expectedUpdatedText}'.");
		Assert.That(updatedText, Is.EqualTo(expectedUpdatedText),
			$"DatePicker rendered text did not update after runtime culture change. Measured '{updatedRawText}', expected '{expectedUpdatedText}'.");
	}

	static string RemoveFormattingCharacters(string value)
	{
		var normalized = new StringBuilder(value.Length);

		foreach (var character in value)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.Format)
				normalized.Append(character);
		}

		return normalized.ToString();
	}
}
#endif

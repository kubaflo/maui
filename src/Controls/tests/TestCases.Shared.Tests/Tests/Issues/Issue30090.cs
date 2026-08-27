#if WINDOWS
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30090 : _IssuesUITest
{
	public Issue30090(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "DatePicker does not update its format when culture changes at runtime";

	[Test]
	[Category(UITestCategories.DatePicker)]
	public void DatePickerUpdatesAfterRuntimeCultureChange()
	{
		var testDate = new DateTime(2025, 12, 24);
		var initialCulture = new CultureInfo("en-US");
		var targetCulture = new CultureInfo("fr-FR");
		var initialDate = NormalizeDateText(testDate.ToString("d", initialCulture));
		var expectedDate = NormalizeDateText(testDate.ToString("d", targetCulture));

		var cultureStatusElement = App.WaitForElement("CultureStatusLabel");
		if (cultureStatusElement is null)
			throw new AssertionException("The culture status label was not found.");

		Assert.That(
			GetRequiredText(cultureStatusElement, "culture status label"),
			Is.EqualTo("Current=en-US; UI=en-US; Default=en-US; DefaultUI=en-US"));

		var managedFormatElement = App.WaitForElement("ManagedFormatLabel");
		if (managedFormatElement is null)
			throw new AssertionException("The managed format label was not found.");

		Assert.That(
			GetRequiredText(managedFormatElement, "managed format label"),
			Is.EqualTo($"Managed en-US format: {initialDate}"));

		var datePickerElement = App.WaitForElement("AffectedDatePicker");
		if (datePickerElement is null)
			throw new AssertionException("The affected DatePicker was not found.");

		Assert.That(
			NormalizeDateText(GetRequiredText(datePickerElement, "affected DatePicker")),
			Is.EqualTo(initialDate));

		var changeCultureButton = App.WaitForElement("ChangeCultureButton");
		if (changeCultureButton is null)
			throw new AssertionException("The change-culture button was not found.");

		changeCultureButton.Click();

		var managedFormatUpdated = App.WaitForTextToBePresentInElement(
			"ManagedFormatLabel",
			$"Managed fr-FR format: {expectedDate}",
			TimeSpan.FromSeconds(5));
		Assert.That(managedFormatUpdated, Is.True, "The culture-change callback did not update managed formatting.");

		var cultureStatusUpdated = App.WaitForTextToBePresentInElement(
			"CultureStatusLabel",
			"Current=fr-FR; UI=fr-FR; Default=fr-FR; DefaultUI=fr-FR",
			TimeSpan.FromSeconds(5));
		Assert.That(cultureStatusUpdated, Is.True, "The culture-change callback did not update all arranged cultures.");

		App.WaitForTextToBePresentInElement(
			"AffectedDatePicker",
			expectedDate,
			TimeSpan.FromSeconds(5));

		var updatedDatePickerElement = App.FindElement("AffectedDatePicker");
		var renderedDate = NormalizeDateText(GetRequiredText(updatedDatePickerElement, "affected DatePicker"));
		Assert.That(
			renderedDate,
			Is.EqualTo(expectedDate),
			$"DatePicker displayed stale date after culture changed at runtime. Expected '{expectedDate}' but found '{renderedDate}'.");
	}

	static string GetRequiredText(IUIElement element, string description)
	{
		if (!element.TryGetText(out var text))
			throw new AssertionException($"The {description} did not expose text.");

		return text;
	}

	static string NormalizeDateText(string value) =>
		string.Concat(value.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.Format)).Trim();
}
#endif

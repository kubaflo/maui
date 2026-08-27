#if WINDOWS
using System.Globalization;
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

	public override string Issue => "DatePicker does not update its format when the culture changes at runtime";

	[Test]
	[Category(UITestCategories.DatePicker)]
	public void DatePickerUpdatesDefaultFormatAfterRuntimeCultureChange()
	{
		var selectedDate = new DateTime(2025, 12, 31);
		var initialCulture = CultureInfo.GetCultureInfo("en-US");
		var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");

		Assert.That(initialCulture.DateTimeFormat.Calendar, Is.TypeOf<GregorianCalendar>());
		Assert.That(frenchCulture.DateTimeFormat.Calendar, Is.TypeOf<GregorianCalendar>());

		var expectedInitialDigits = DigitsOnly(selectedDate.ToString("d", initialCulture));
		var expectedFrenchDigits = DigitsOnly(selectedDate.ToString("d", frenchCulture));

		Assert.That(App.WaitForTextToBePresentInElement("ActiveCultureLabel", "Active culture: en-US"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("CultureCallbackMarkerLabel", "Culture callback marker: -1"), Is.True);

		var datePicker = App.WaitForElement("DatePickerControl");
		var datePickerRect = datePicker.GetRect();
		Assert.That(datePickerRect.Width, Is.GreaterThan(0));
		Assert.That(datePickerRect.Height, Is.GreaterThan(0));

		var initialDigits = DigitsOnly(RequireText(datePicker, "DatePickerControl"));
		Assert.That(initialDigits, Is.EqualTo(expectedInitialDigits),
			$"DatePicker initial rendered digits were {initialDigits}; expected {expectedInitialDigits}.");

		App.Tap("ChangeCultureButton");

		Assert.That(App.WaitForTextToBePresentInElement("CultureCallbackMarkerLabel", "Culture callback marker: 1"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("ActiveCultureLabel", "Active culture: fr-FR"), Is.True);

		App.RetryAssert(() =>
		{
			var renderedDigits = DigitsOnly(RequireText(App.WaitForElement("DatePickerControl"), "DatePickerControl"));
			Assert.That(renderedDigits, Is.EqualTo(expectedFrenchDigits),
				$"DatePicker rendered digits after fr-FR culture change were {renderedDigits}; expected {expectedFrenchDigits}.");
		}, timeout: TimeSpan.FromSeconds(5));
	}

	static string RequireText(IUIElement element, string automationId)
	{
		var text = element.GetText();
		return text is null
			? throw new AssertionException($"Element '{automationId}' did not expose text.")
			: text;
	}

	static string DigitsOnly(string value) => string.Concat(value.Where(char.IsDigit));
}
#endif

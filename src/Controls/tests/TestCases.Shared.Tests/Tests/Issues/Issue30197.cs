#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30197 : _IssuesUITest
{
	public Issue30197(TestDevice device) : base(device)
	{
	}

	public override string Issue => "TimePicker does not update its format when culture changes at runtime";

	[Test]
	[Category(UITestCategories.TimePicker)]
	public void TimePickerImmediatelyUsesNewCultureFormat()
	{
		App.WaitForElement("CultureTimePicker");

		Assert.That(GetRequiredText("CultureLabel"), Is.EqualTo("Current culture: en-US"));
		Assert.That(GetRequiredText("LoadedStatusLabel"), Is.EqualTo("12HourClock"),
			"The loaded WinUI TimePicker must report the arranged en-US 12-hour clock identifier.");
		Assert.That(GetRequiredText("TransitionStatusLabel"), Is.EqualTo("not-started"));

		var expectedText = GetRequiredText("ExpectedValueLabel");
		var initialText = NormalizeNativeTimePickerText(GetRequiredText("FlyoutButton"));

		Assert.That(initialText, Does.Not.Contain(expectedText),
			"The initial en-US display must differ from the expected fr-FR display.");

		App.Tap("ChangeCultureButton");

		App.RetryAssert(() =>
		{
			Assert.That(GetRequiredText("CultureLabel"), Is.EqualTo("Current culture: fr-FR"));
			Assert.That(GetRequiredText("TransitionStatusLabel"), Is.EqualTo("post-change-complete"));
			Assert.That(GetRequiredText("ExpectedValueLabel"), Is.EqualTo(expectedText));
		});

		var observedText = NormalizeNativeTimePickerText(GetRequiredText("FlyoutButton"));

		Assert.That(observedText, Does.Contain(expectedText),
			$"TimePicker should immediately adopt the fr-FR 24-hour format after runtime culture change. Initial: '{initialText}', observed: '{observedText}', expected: '{expectedText}'.");
	}

	static string NormalizeNativeTimePickerText(string text) =>
		new(text.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.Format).ToArray());

	string GetRequiredText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' must expose text.");

		return text;
	}
}
#endif

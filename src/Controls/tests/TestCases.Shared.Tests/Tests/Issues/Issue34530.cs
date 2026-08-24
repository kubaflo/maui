#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34530 : _IssuesUITest
{
	public override string Issue => "TextToSpeech GetLocalesAsync does not return Lithuanian on iOS";

	public Issue34530(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Picker)]
	public void GetLocalesAsyncReturnsLithuanianAfterPickerIsFocused()
	{
		string ReadRequiredElementText(string automationId)
		{
			var text = App.WaitForElement(automationId).GetText();
			if (text is null)
				throw new InvalidOperationException($"{automationId} text was null.");

			return text;
		}

		var iosApp = (AppiumIOSApp)App;
		var platformVersionText = iosApp.Driver.Capabilities.GetCapability("platformVersion") as string;
		if (platformVersionText is null)
			throw new InvalidOperationException("The iOS platformVersion capability was missing.");

		if (!Version.TryParse(platformVersionText, out var platformVersion) || platformVersion is null)
			throw new InvalidOperationException($"The iOS platformVersion capability '{platformVersionText}' was invalid.");

		if (platformVersion.CompareTo(new Version(26, 3, 1)) < 0)
			return;

		App.SetOrientationPortrait();
		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The test app must be in portrait orientation.");

		Assert.That(ReadRequiredElementText("LocaleQueryStatusLabel"), Is.EqualTo("Locale query not started"));
		Assert.That(ReadRequiredElementText("LithuanianLocaleCountLabel"), Is.EqualTo("-1"));

		App.WaitForElement("LocalePicker");
		App.Tap("LocalePicker");

		App.WaitForTextToBePresentInElement("PickerFocusStatusLabel", "Picker focused", timeout: TimeSpan.FromSeconds(15));
		Assert.That(ReadRequiredElementText("PickerFocusStatusLabel"), Is.EqualTo("Picker focused"));
		App.WaitForTextToBePresentInElement("LocaleQueryStatusLabel", "Locale query completed", timeout: TimeSpan.FromSeconds(30));
		Assert.That(ReadRequiredElementText("LocaleQueryStatusLabel"), Is.EqualTo("Locale query completed"));

		var totalLocaleCountText = ReadRequiredElementText("TotalLocaleCountLabel");
		var lithuanianLocaleCountText = ReadRequiredElementText("LithuanianLocaleCountLabel");
		var totalLocaleCount = int.Parse(totalLocaleCountText, CultureInfo.InvariantCulture);
		var lithuanianLocaleCount = int.Parse(lithuanianLocaleCountText, CultureInfo.InvariantCulture);

		Assert.That(totalLocaleCount, Is.GreaterThan(0), "GetLocalesAsync should return at least one locale.");
		Assert.That(
			lithuanianLocaleCount,
			Is.GreaterThanOrEqualTo(1),
			$"Lithuanian locale count was {lithuanianLocaleCount}; expected at least 1 locale from TextToSpeech.Default.GetLocalesAsync(). Total locale count was {totalLocaleCount}.");
	}
}
#endif

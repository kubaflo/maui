#if IOS
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32288 : _IssuesUITest
{
	public Issue32288(TestDevice device) : base(device) { }

	public override string Issue => "Keyboard Numeric is not working in iOS";

	[Test]
	[Category(UITestCategories.Entry)]
	public void NumericKeyboardExposesSignedDecimalKeys()
	{
		App.SetOrientationPortrait();

		if (App is not AppiumApp appiumApp)
		{
			Assert.Fail("The iOS keyboard test requires the Appium driver.");
			return;
		}

		var platformVersionText = appiumApp.Driver.Capabilities.GetCapability("platformVersion")?.ToString();
		if (platformVersionText is null || !Version.TryParse(platformVersionText, out var platformVersion))
		{
			Assert.Fail("The iOS platform version capability was unavailable.");
			return;
		}

		Assert.That(platformVersion.Major, Is.GreaterThanOrEqualTo(26),
			"The numeric keyboard regression requires iOS 26 or later.");

		var culture = CultureInfo.GetCultureInfo("en-US");
		var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
		var negativeSign = culture.NumberFormat.NegativeSign;

		var numericEntry = App.WaitForElement("NumericEntry");
		var environmentStatus = App.FindElement("EnvironmentStatus");
		if (environmentStatus is null)
		{
			Assert.Fail("The environment status label was not found.");
			return;
		}

		Assert.That(environmentStatus.GetText(),
			Is.EqualTo($"Tap the numeric Entry and inspect its signed decimal keys.|{culture.Name}|{decimalSeparator}|{negativeSign}|Light"));
		Assert.That(numericEntry.GetAttribute<string>("focused"), Is.EqualTo("false"));
		var focusStatus = App.FindElement("FocusStatus");
		if (focusStatus is null)
		{
			Assert.Fail("The focus status label was not found.");
			return;
		}

		Assert.That(focusStatus.GetText(), Is.EqualTo("-1"));
		Assert.That(App.IsKeyboardShown(), Is.False);

		App.Tap("NumericEntry");

		Assert.That(App.WaitForTextToBePresentInElement("FocusStatus", "1", TimeSpan.FromSeconds(5)), Is.True,
			"The Entry focus callback did not run.");
		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5)), Is.True,
			"The iOS software keyboard did not appear.");

		var keyboard = appiumApp.Driver.FindElements(MobileBy.ClassName("XCUIElementTypeKeyboard"));
		Assert.That(keyboard, Is.Not.Empty, "The native iOS keyboard was not exposed to accessibility.");

		var keys = appiumApp.Driver.FindElements(
			MobileBy.XPath("//XCUIElementTypeKeyboard//XCUIElementTypeKey"));
		var keyNames = new List<string>();
		foreach (var key in keys)
		{
			var name = key.GetAttribute("name");
			if (!string.IsNullOrEmpty(name))
				keyNames.Add(name);
		}

		Assert.That(keyNames, Is.Not.Empty, "The native iOS keyboard exposed no key accessibility names.");
		for (var digit = 0; digit <= 9; digit++)
			Assert.That(keyNames, Does.Contain(digit.ToString(culture)), $"The numeric keyboard did not expose digit {digit}.");

		Assert.That(keyNames, Does.Contain(decimalSeparator),
			$"The numeric keyboard did not expose the en-US decimal separator '{decimalSeparator}'.");
		Assert.That(keyNames, Does.Contain(negativeSign),
			$"Numeric keyboard did not expose the negative-sign key. Expected '{negativeSign}'; observed keys: {string.Join(", ", keyNames)}");
	}
}
#endif

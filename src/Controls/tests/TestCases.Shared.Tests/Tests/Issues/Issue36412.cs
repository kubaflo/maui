#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public Issue36412(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	[Test]
	[Category(UITestCategories.Entry)]
	public void VisibleEntryAboveKeyboardShouldReceiveFocus()
	{
		bool IsTrueAttribute(string automationId, string attributeName) =>
			string.Equals(
				App.WaitForElement(automationId).GetAttribute<string>(attributeName),
				"true",
				StringComparison.OrdinalIgnoreCase);

		var platformVersionText = ((AppiumApp)App).Driver.Capabilities.GetCapability("platformVersion")?.ToString()
			?? throw new InvalidOperationException("platformVersion capability is missing or null.");
		if (Version.Parse(platformVersionText).Major < 15)
			Assert.Ignore("The Done keyboard accessory is only present on iOS 15 or later.");

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(OpenQA.Selenium.ScreenOrientation.Portrait));

		var field1 = App.WaitForElement("Issue36412Field1");
		var field2 = App.WaitForElement("Issue36412Field2");
		var field7 = App.WaitForElement("Issue36412Field7");
		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();

		var field1Rect = field1.GetRect();
		App.TapCoordinates(field1Rect.CenterX(), field1Rect.CenterY());
		Assert.That(App.WaitForKeyboardToShow(), Is.True, "The software numeric keyboard should be visible");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36412FocusEventToken", "focusEventToken=1", TimeSpan.FromSeconds(3)),
			Is.True,
			"Field 1 should receive a focus event");
		var toolbar = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar"));
		var doneButton = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar//XCUIElementTypeButton"));

		var field2Rect = field2.GetRect();
		App.TapCoordinates(field2Rect.CenterX(), field2Rect.CenterY());
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36412FocusEventToken", "focusEventToken=2", TimeSpan.FromSeconds(3)),
			Is.True,
			"Unobstructed Field 2 should receive a focus event");
		App.TapCoordinates(field1Rect.CenterX(), field1Rect.CenterY());
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36412FocusEventToken", "focusEventToken=1", TimeSpan.FromSeconds(3)),
			Is.True,
			"Field 1 should receive focus again");
		var field7Rect = field7.GetRect();
		Assert.That(IsTrueAttribute("Issue36412Field7", "visible"), Is.True, "Field 7 should be natively visible");
		Assert.That(IsTrueAttribute("Issue36412Field7", "enabled"), Is.True, "Field 7 should be natively enabled");
		Assert.That(field7Rect.Width, Is.GreaterThan(0), "Field 7 should have a nonzero native width");
		Assert.That(field7Rect.Height, Is.GreaterThan(0), "Field 7 should have a nonzero native height");
		Assert.That(field7Rect.CenterX(), Is.InRange(windowRect.X, windowRect.X + windowRect.Width), "Field 7 center should be horizontally in the window");
		Assert.That(field7Rect.CenterY(), Is.InRange(windowRect.Y, windowRect.Y + windowRect.Height), "Field 7 center should be vertically in the window");

		TestContext.WriteLine($"Field7={field7Rect}; Toolbar={toolbar.GetRect()}; DoneButton={doneButton.GetRect()}");
		App.TapCoordinates(field7Rect.CenterX(), field7Rect.CenterY());

		var focusTransitionOccurred = App.WaitForTextToBePresentInElement(
			"Issue36412FocusEventToken",
			"focusEventToken=7",
			TimeSpan.FromSeconds(3));
		var focusEventToken = App.WaitForElement("Issue36412FocusEventToken").GetText()?
			.Replace("focusEventToken=", string.Empty, StringComparison.Ordinal) ?? "<missing>";
		var field7Focused = string.Equals(focusEventToken, "7", StringComparison.Ordinal);
		var field1Focused = string.Equals(focusEventToken, "1", StringComparison.Ordinal);

		Assert.That(
			focusTransitionOccurred && field7Focused && !field1Focused,
			Is.True,
			$"Field 7 visible-center tap should move focus; Field7Focused={field7Focused}, Field1Focused={field1Focused}, focusEventToken={focusEventToken}, expectedToken=7");
	}
}
#endif

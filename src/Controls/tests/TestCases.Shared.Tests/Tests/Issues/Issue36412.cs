#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public Issue36412(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] Done keyboard accessory blocks taps on the Entry above the keyboard";

	[Test]
	[Category(UITestCategories.Entry)]
	public void VisibleEntryAboveNumericKeyboardReceivesFocus()
	{
		if (App is not AppiumApp)
			throw new InvalidOperationException("The iOS test requires the Appium driver.");

		App.SetOrientationPortrait();

		var windowQuery = AppiumQuery.ByXPath("//XCUIElementTypeWindow");
		var window = App.WaitForElement(
			() =>
			{
				var candidate = App.FindElement(windowQuery);
				if (candidate is null)
					return null;

				var candidateRect = candidate.GetRect();
				return candidateRect.Height > candidateRect.Width ? candidate : null;
			},
			"The device should settle in portrait orientation.",
			TimeSpan.FromSeconds(10));
		var windowRect = window.GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The device must be in portrait orientation.");

		App.WaitForElement("InstructionsLabel");
		var field1 = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeTextField[@name='Field1' and @visible='true' and @enabled='true']"));
		var field7 = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeTextField[@name='Field7' and @visible='true' and @enabled='true']"));

		App.Tap("Field1");
		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(10)), Is.True,
			"The iOS numeric software keyboard should be visible after tapping Field 1.");

		var keyboard = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeKeyboard"));
		var accessory = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar"));
		var accessoryButton = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar//XCUIElementTypeButton"));

		var field7Rect = field7.GetRect();
		var keyboardRect = keyboard.GetRect();
		var accessoryRect = accessory.GetRect();
		var accessoryButtonRect = accessoryButton.GetRect();
		int tapX = field7Rect.CenterX();
		int tapY = field7Rect.CenterY();

		Assert.That(accessoryRect.Width, Is.EqualTo(windowRect.Width),
			"The MAUI input accessory should span the native window.");
		Assert.That(accessoryRect.Height, Is.EqualTo(44),
			"The MAUI input accessory should have its native 44-point height.");
		Assert.That(field7Rect.Width, Is.GreaterThan(0), "Field 7 must have a native frame.");
		Assert.That(field7Rect.Height, Is.GreaterThan(0), "Field 7 must have a native frame.");
		Assert.That(field7Rect.Y, Is.LessThan(keyboardRect.Y), "Field 7 must be visible above the keyboard.");
		Assert.That(tapX, Is.InRange(field7Rect.Left, field7Rect.Right - 1));
		Assert.That(tapY, Is.InRange(field7Rect.Top, field7Rect.Bottom - 1));

		bool tapInsideAccessoryButton =
			tapX >= accessoryButtonRect.Left &&
			tapX < accessoryButtonRect.Right &&
			tapY >= accessoryButtonRect.Top &&
			tapY < accessoryButtonRect.Bottom;
		Assert.That(tapInsideAccessoryButton, Is.False,
			"The Field 7 tap point must be outside the accessory button.");

		App.Tap("Field7");

		var observedField1Focus = field1.GetAttribute<string>("focused");
		var observedField7Focus = field7.GetAttribute<string>("focused");
		Assert.That(observedField1Focus, Is.Not.Null, "Field 1 native focus state should be available.");
		Assert.That(observedField7Focus, Is.Not.Null, "Field 7 native focus state should be available.");

		Assert.That(bool.TryParse(observedField1Focus, out bool field1IsFocused), Is.True);
		Assert.That(bool.TryParse(observedField7Focus, out bool field7IsFocused), Is.True);

		string geometry =
			$"Field1 focused={field1IsFocused}, Field7 focused={field7IsFocused}, " +
			$"Field7={field7Rect}, keyboard={keyboardRect}, accessory={accessoryRect}, tap=({tapX},{tapY})";
		Assert.That(field7IsFocused, Is.True,
			$"Field 7 should receive focus after tapping its visible native text field above the numeric keyboard. {geometry}");
		Assert.That(field1IsFocused, Is.False,
			$"Field 1 should relinquish focus after Field 7 is tapped. {geometry}");
	}
}
#endif

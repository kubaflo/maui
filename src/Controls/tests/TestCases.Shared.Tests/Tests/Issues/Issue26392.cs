#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26392 : _IssuesUITest
{
	public override string Issue => "Click on flyout clicks on page behind";

	public Issue26392(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void OpenFlyoutConsumesTapOverPicker()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumAndroidApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Width, Is.LessThan(windowSize.Height), "The app window must be in portrait orientation.");

		var firstPicker = App.WaitForElement("Issue26392FirstPicker");
		Assert.That(firstPicker, Is.Not.Null);
		var firstPickerRect = firstPicker.GetRect();
		Assert.That(firstPickerRect.Width, Is.GreaterThan(0), "The first Picker must be arranged.");
		Assert.That(firstPickerRect.Height, Is.GreaterThan(0), "The first Picker must be arranged.");

		var focusStateElement = App.WaitForElement("Issue26392FirstPickerIsFocused");
		Assert.That(focusStateElement, Is.Not.Null);
		var initialFocusState = focusStateElement.GetText();
		Assert.That(initialFocusState, Is.EqualTo("False"), "The first Picker must not be focused before opening the flyout.");
		Assert.That(App.FindElements("Issue26392FlyoutLabel"), Is.Empty, "The Shell flyout must initially be closed.");
		Assert.That(App.FindElementsByText("Baboon"), Is.Empty, "The Picker dialog must initially be closed.");

		var swipeY = windowSize.Height / 2;
		App.DragCoordinates(5, swipeY, windowSize.Width * 0.8f, swipeY);

		var transitionSentinel = -1;
		var flyoutLabel = App.WaitForElement("Issue26392FlyoutLabel");
		Assert.That(flyoutLabel, Is.Not.Null);
		transitionSentinel = 1;
		Assert.That(transitionSentinel, Is.EqualTo(1), "The Shell flyout must complete its closed-to-open transition.");

		var flyoutLabelRect = flyoutLabel.GetRect();
		var tapY = flyoutLabelRect.Y + (flyoutLabelRect.Height / 2);
		Assert.That(tapY, Is.InRange(firstPickerRect.Y, firstPickerRect.Y + firstPickerRect.Height),
			"The flyout tap must be vertically aligned with the first Picker.");

		App.Tap("Issue26392FlyoutLabel");

		var pickerReportedFocused = App.WaitForTextToBePresentInElement(
			"Issue26392FirstPickerIsFocused",
			"True",
			TimeSpan.FromSeconds(2));
		var pickerDialogOpened = App.FindElementsByText("Baboon").Count > 0;

		Assert.That(
			pickerReportedFocused || pickerDialogOpened,
			Is.False,
			$"Open Shell flyout forwarded the tap to the underlying Picker (focus reported: {pickerReportedFocused}; dialog opened: {pickerDialogOpened})");
	}
}
#endif

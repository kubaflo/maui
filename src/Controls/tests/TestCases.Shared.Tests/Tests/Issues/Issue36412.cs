#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public Issue36412(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	[Test]
	[Category(UITestCategories.Entry)]
	public void TappingEntryBehindDoneAccessoryTransfersFocus()
	{
		var appiumApp = (AppiumApp)App;
		var platformVersion = appiumApp.Driver.Capabilities.GetCapability("platformVersion") as string
			?? throw new InvalidOperationException("platformVersion capability is missing or null.");
		if (Version.Parse(platformVersion).Major < 15)
			return;

		App.SetOrientationPortrait();
		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width),
			$"Issue 36412 requires portrait orientation, but the window was {windowSize.Width}x{windowSize.Height}.");

		Assert.That(App.WaitForElement("LastFocusedIdentity").GetText(), Is.EqualTo("-1"));
		Assert.That(App.WaitForElement("ObservationSequence").GetText(), Is.EqualTo("-1"));
		App.WaitForElement("Field7");

		App.Tap("Field1");
		App.WaitForTextToBePresentInElement("LastFocusedIdentity", "1");
		Assert.That(App.WaitForElement("LastFocusedIdentity").GetText(), Is.EqualTo("1"),
			"Issue 36412 prerequisite failed: Field 1's Focused callback did not record identity 1.");
		App.WaitForKeyboardToShow();

		var toolbarRect = App.WaitForElement("Toolbar").GetRect();
		Assert.That(toolbarRect.Width, Is.GreaterThan(0), "Issue 36412 prerequisite failed: the native Toolbar had no width.");
		Assert.That(toolbarRect.Height, Is.GreaterThan(0), "Issue 36412 prerequisite failed: the native Toolbar had no height.");

		var field7 = App.WaitForElement("Field7");
		Assert.That(field7.IsDisplayed(), Is.True, "Issue 36412 prerequisite failed: Field 7 was not visible.");
		Assert.That(field7.IsEnabled(), Is.True, "Issue 36412 prerequisite failed: Field 7 was not enabled.");
		var field7Rect = field7.GetRect();
		Assert.That(field7Rect.Width, Is.GreaterThan(0), "Issue 36412 prerequisite failed: Field 7 had no width.");
		Assert.That(field7Rect.Height, Is.GreaterThan(0), "Issue 36412 prerequisite failed: Field 7 had no height.");
		var tapX = field7Rect.X + field7Rect.Width / 2;
		var tapY = field7Rect.Y + field7Rect.Height / 2;
		Assert.That(tapX, Is.InRange(0, windowSize.Width), $"Issue 36412 prerequisite failed: Field 7 center X was {tapX}.");
		Assert.That(tapY, Is.InRange(0, windowSize.Height), $"Issue 36412 prerequisite failed: Field 7 center Y was {tapY}.");
		Assert.That(tapX, Is.InRange(toolbarRect.X, toolbarRect.Right),
			$"Issue 36412 prerequisite failed: Field 7 center X {tapX} was outside Toolbar {toolbarRect}.");
		Assert.That(tapY, Is.InRange(toolbarRect.Y, toolbarRect.Bottom),
			$"Issue 36412 prerequisite failed: Field 7 center Y {tapY} was outside Toolbar {toolbarRect}.");

		var beforeIdentity = App.WaitForElement("LastFocusedIdentity").GetText();
		Assert.That(beforeIdentity, Is.EqualTo("1"),
			"Issue 36412 prerequisite failed: Field 1 was not the last focused Entry immediately before the Field 7 tap.");
		App.TapCoordinates(tapX, tapY);

		App.DismissKeyboard();
		App.WaitForKeyboardToHide();
		App.Tap("ObserveFocusButton");
		App.WaitForTextToBePresentInElement("ObservationSequence", "0");
		Assert.That(App.WaitForElement("ObservationSequence").GetText(), Is.EqualTo("0"),
			"Issue 36412 prerequisite failed: the post-trigger observation callback did not run.");

		var afterIdentity = App.WaitForElement("LastFocusedIdentity").GetText();
		Assert.That(afterIdentity, Is.EqualTo("7"),
			$"Issue 36412: tapping Field 7 at its measured center did not focus Field 7; before={beforeIdentity}, after={afterIdentity}, Field7={field7Rect}, tap=({tapX},{tapY}), Toolbar={toolbarRect}.");
	}
}
#endif

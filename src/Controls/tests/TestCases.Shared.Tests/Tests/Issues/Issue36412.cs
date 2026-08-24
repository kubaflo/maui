#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	public Issue36412(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Entry)]
	public void EntryBehindDoneAccessoryReceivesFocus()
	{
		var iosApp = (AppiumIOSApp)App;
		var platformVersion = iosApp.Driver.Capabilities.GetCapability("platformVersion") as string;
		if (string.IsNullOrEmpty(platformVersion))
		{
			Assert.Fail("The iOS platform version must be available.");
			return;
		}

		if (!int.TryParse(platformVersion.Split('.')[0], out int majorVersion))
		{
			Assert.Fail($"Unable to parse iOS platform version '{platformVersion}'.");
			return;
		}

		if (majorVersion < 15)
			Assert.Ignore("Issue 36412 requires iOS 15 or later.");

		App.SetOrientationPortrait();

		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Width, Is.GreaterThan(0));
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The test must run in portrait orientation.");

		var initialStatus = App.WaitForElement("CounterStatusLabel").GetText();
		if (initialStatus is null)
		{
			Assert.Fail("The initial focus state must be available.");
			return;
		}
		Assert.That(initialStatus, Is.EqualTo("F1=0;U1=0;F8=0;Owner=None"));

		var field1Rect = App.WaitForElement("Field1").GetRect();
		var field8Rect = App.WaitForElement("Field8").GetRect();
		Assert.That(field1Rect.Width, Is.GreaterThan(0));
		Assert.That(field1Rect.Height, Is.GreaterThan(0));
		Assert.That(field8Rect.Width, Is.GreaterThan(0));
		Assert.That(field8Rect.Height, Is.GreaterThan(0));
		App.TapCoordinates(field1Rect.CenterX(), field1Rect.CenterY());

		bool field1FocusObserved = App.WaitForTextToBePresentInElement(
			"CounterStatusLabel",
			"F1=1;U1=0;F8=0;Owner=Field1",
			TimeSpan.FromSeconds(3));
		Assert.That(field1FocusObserved, Is.True, "Field 1 should receive focus from an in-bounds coordinate tap.");

		var toolbarRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar")).GetRect();
		var doneButtonRect = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeToolbar//XCUIElementTypeButton")).GetRect();

		Assert.That(toolbarRect.Width, Is.GreaterThan(0));
		Assert.That(toolbarRect.Height, Is.GreaterThan(0));
		Assert.That(doneButtonRect.Width, Is.GreaterThan(0));
		Assert.That(doneButtonRect.Height, Is.GreaterThan(0));
		Assert.That(field1Rect.Bottom, Is.LessThanOrEqualTo(toolbarRect.Top),
			"Field 1 must provide a non-overlapped control point for the coordinate-tap oracle.");

		int overlapTop = Math.Max(field8Rect.Top, toolbarRect.Top);
		int overlapBottom = Math.Min(field8Rect.Bottom, toolbarRect.Bottom);
		Assert.That(overlapBottom, Is.GreaterThan(overlapTop),
			"Field 8 must overlap the transparent Done accessory to reproduce the reported blocked tap.");

		int tapX = field8Rect.CenterX();
		int tapY = overlapTop + ((overlapBottom - overlapTop) / 2);
		bool tapOverlapsDoneButton =
			tapX >= doneButtonRect.Left &&
			tapX < doneButtonRect.Right &&
			tapY >= doneButtonRect.Top &&
			tapY < doneButtonRect.Bottom;
		Assert.That(tapOverlapsDoneButton, Is.False, "The Field 8 tap point must be outside the Done button.");

		int observedField8FocusCount = -1;
		App.TapCoordinates(tapX, tapY);
		bool field8FocusObserved = App.WaitForTextToBePresentInElement(
			"CounterStatusLabel",
			"F8=1",
			TimeSpan.FromSeconds(3));
		var postTapStatus = App.FindElement("CounterStatusLabel").GetText();
		if (postTapStatus is null)
		{
			Assert.Fail("A post-tap focus-state sample must be available.");
			return;
		}
		observedField8FocusCount = field8FocusObserved ? 1 : 0;
		Assert.That(observedField8FocusCount, Is.Not.EqualTo(-1), "A post-tap focus-state sample must replace the sentinel.");
		Assert.That(field8FocusObserved, Is.True,
			"Field 8 should receive focus after tapping it while the Done accessory is visible.");
		Assert.That(postTapStatus, Is.EqualTo("F1=1;U1=1;F8=1;Owner=Field8"),
			"Focus should transfer from Field 1 to Field 8.");
	}
}
#endif

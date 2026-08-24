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
	public void VisibleEntryAboveKeyboardCanReceiveFocus()
	{
		App.SetOrientationPortrait();

		var rootRect = App.WaitForElement("RootGrid").GetRect();
		Assert.That(rootRect.Height, Is.GreaterThan(rootRect.Width), "The reproduction requires portrait orientation");

		var initialMarker = App.WaitForElement("FocusMarker").GetText();
		Assert.That(initialMarker, Is.Not.Null);
		Assert.That(initialMarker, Is.EqualTo("Focus: none"), "No Entry should be focused before the trigger");

		for (int i = 1; i <= 15; i++)
			App.WaitForElement($"Field{i}");

		var field8 = App.WaitForElement("Field8");
		Assert.That(field8.IsDisplayed(), Is.True, "Field 8 must be visible before showing the keyboard");
		Assert.That(field8.IsEnabled(), Is.True, "Field 8 must be enabled");

		App.Tap("Field1");
		bool field1Focused = App.WaitForTextToBePresentInElement("FocusMarker", "Focus: Field 1", TimeSpan.FromSeconds(3));
		Assert.That(field1Focused, Is.True, "Field 1 must receive focus before testing the accessory");
		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5)), Is.True, "The numeric keyboard must be visible");

		var accessory = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeToolbar[@name='Toolbar']"),
			timeout: TimeSpan.FromSeconds(5));
		Assert.That(accessory.IsDisplayed(), Is.True, "The MAUI Done keyboard accessory must be visible");

		field8 = App.WaitForElement("Field8");
		Assert.That(field8.IsDisplayed(), Is.True, "Field 8 must remain visible above the keyboard accessory");
		var field8Rect = field8.GetRect();
		Assert.That(field8Rect.Width, Is.GreaterThan(0), "Field 8 must have a tappable width");
		Assert.That(field8Rect.Height, Is.GreaterThan(0), "Field 8 must have a tappable height");
		var accessoryRect = accessory.GetRect();
		var overlapTop = Math.Max(field8Rect.Y, accessoryRect.Y);
		var overlapBottom = Math.Min(field8Rect.Y + field8Rect.Height, accessoryRect.Y + accessoryRect.Height);
		Assert.That(overlapBottom, Is.GreaterThan(overlapTop),
			"Field 8 must overlap the transparent keyboard accessory");
		var markerBeforeTap = App.WaitForElement("FocusMarker").GetText();
		Assert.That(markerBeforeTap, Is.Not.Null);

		App.TapCoordinates(field8Rect.CenterX(), overlapTop + ((overlapBottom - overlapTop) / 2));

		bool field8Focused = App.WaitForTextToBePresentInElement("FocusMarker", "Focus: Field 8", TimeSpan.FromSeconds(3));
		var markerAfterTap = App.WaitForElement("FocusMarker").GetText();
		Assert.That(markerAfterTap, Is.Not.Null);
		Assert.That(field8Focused, Is.True,
			$"Field 8 should receive focus when its visible portion behind the accessory is tapped. Before: {markerBeforeTap}; After: {markerAfterTap}");
		Assert.That(markerAfterTap, Is.EqualTo("Focus: Field 8"));
	}
}
#endif

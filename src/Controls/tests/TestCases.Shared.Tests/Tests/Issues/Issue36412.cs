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
	public void VisibleEntryAboveNumericKeyboardReceivesFocus()
	{
		App.SetOrientationPortrait();

		var field1 = App.WaitForElement("Field1");

		Assert.That(field1.IsDisplayed(), Is.True, "Field 1 must be displayed before it is tapped.");

		var field1Rect = field1.GetRect();
		App.TapCoordinates(field1Rect.CenterX(), field1Rect.CenterY());

		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5)), Is.True,
			"The numeric keyboard did not appear after tapping Field 1.");
		App.RetryAssert(
			() => Assert.That(App.IsFocused("Field1"), Is.True, "Field 1 did not receive native automation focus."),
			timeout: TimeSpan.FromSeconds(5),
			retryFrequency: TimeSpan.FromMilliseconds(200));

		var accessoryRect = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeToolbar[@name='Toolbar']"),
			timeout: TimeSpan.FromSeconds(5)).GetRect();
		Assert.That(accessoryRect.Height, Is.GreaterThan(0), "The numeric keyboard accessory must be visible.");

		var coveredField = string.Empty;
		for (var fieldNumber = 2; fieldNumber <= 15; fieldNumber++)
		{
			var candidate = $"Field{fieldNumber}";
			var candidateElement = App.WaitForElement(candidate);
			var candidateRect = candidateElement.GetRect();
			if (candidateElement.IsDisplayed() &&
				candidateRect.CenterY() >= accessoryRect.Y &&
				candidateRect.CenterY() <= accessoryRect.Y + accessoryRect.Height)
			{
				coveredField = candidate;
				break;
			}
		}

		Assert.That(coveredField, Is.Not.Empty,
			"A visible Entry centered behind the numeric keyboard accessory was not found.");
		Assert.That(App.IsFocused(coveredField), Is.False);
		var field1FocusedBefore = App.IsFocused("Field1");
		var coveredFieldFocusedBefore = App.IsFocused(coveredField);
		var coveredFieldElement = App.WaitForElement(coveredField);
		var coveredFieldRect = coveredFieldElement.GetRect();

		App.TapCoordinates(coveredFieldRect.CenterX(), coveredFieldRect.CenterY());

		App.RetryAssert(() =>
		{
			var field1FocusedAfter = App.IsFocused("Field1");
			var coveredFieldFocusedAfter = App.IsFocused(coveredField);
			Assert.That(coveredFieldFocusedAfter, Is.True,
				$"The visible Entry behind the Done accessory did not receive focus after its center was tapped; " +
				$"target={coveredField}; before: Field1={field1FocusedBefore}, target={coveredFieldFocusedBefore}; " +
				$"after: Field1={field1FocusedAfter}, target={coveredFieldFocusedAfter}.");
			Assert.That(field1FocusedAfter, Is.False,
				$"Field 1 remained focused after {coveredField} received the tap.");
		}, timeout: TimeSpan.FromSeconds(5), retryFrequency: TimeSpan.FromMilliseconds(200));
	}
}
#endif

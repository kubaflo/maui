#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	public Issue36412(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Entry)]
	public void TappingVisibleEntryBehindDoneAccessoryTransfersFocus()
	{
		App.SetOrientationPortrait();

		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(windowRect.Height, Is.GreaterThan(windowRect.Width), "The test requires portrait orientation.");
		Assert.That(App.WaitForElement("LastFocusedIndex").GetText(), Is.EqualTo("-1"));
		Assert.That(App.WaitForElement("FocusEventCount").GetText(), Is.EqualTo("0"));

		App.Tap("Field1");
		App.EnterText("Field1", "1");
		Assert.That(App.WaitForTextToBePresentInElement("Field1", "1", TimeSpan.FromSeconds(3)), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("LastFocusedIndex", "1", TimeSpan.FromSeconds(3)), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("FocusEventCount", "1", TimeSpan.FromSeconds(3)), Is.True);

		var keyboardRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeKeyboard")).GetRect();
		var toolbarRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar")).GetRect();
		var doneRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar//XCUIElementTypeButton")).GetRect();

		Assert.That(keyboardRect.Height, Is.GreaterThan(0), "The software keyboard must be visible.");
		Assert.That(toolbarRect.Height, Is.GreaterThan(0), "The Done accessory toolbar must be visible.");
		Assert.That(toolbarRect.Width, Is.EqualTo(windowRect.Width).Within(2), "The Done accessory must span the window.");
		Assert.That(toolbarRect.X, Is.EqualTo(windowRect.X).Within(2), "The Done accessory must span from the window's leading edge.");

		var targetIndex = -1;
		for (var index = 2; index <= 15; index++)
		{
			var fieldRect = App.WaitForElement($"Field{index}").GetRect();
			var candidateOverlapTop = Math.Max(fieldRect.Y, toolbarRect.Y);
			var candidateOverlapBottom = Math.Min(fieldRect.Y + fieldRect.Height, toolbarRect.Y + toolbarRect.Height);
			if (candidateOverlapBottom > candidateOverlapTop)
			{
				targetIndex = index;
				break;
			}
		}

		Assert.That(targetIndex, Is.GreaterThan(1),
			"A visible numbered Entry must occupy the Done accessory band.");
		var targetId = $"Field{targetIndex}";
		var targetRect = App.WaitForElement(targetId).GetRect();
		Assert.That(targetRect.Width, Is.GreaterThan(0), "The target Entry must have a visible native frame.");
		var overlapTop = Math.Max(targetRect.Y, toolbarRect.Y);
		var overlapBottom = Math.Min(targetRect.Y + targetRect.Height, toolbarRect.Y + toolbarRect.Height);

		var field4Rect = App.WaitForElement("Field4").GetRect();
		Assert.That(field4Rect.Y + field4Rect.Height, Is.LessThanOrEqualTo(toolbarRect.Y),
			"Field 4 must remain above the accessory as the unobstructed focus control.");
		App.TapCoordinates(field4Rect.CenterX(), field4Rect.CenterY());
		Assert.That(App.WaitForTextToBePresentInElement("LastFocusedIndex", "4", TimeSpan.FromSeconds(3)), Is.True,
			"The unobstructed Field 4 tap must produce a Focused callback.");

		App.Tap("Field1");
		Assert.That(App.WaitForTextToBePresentInElement("LastFocusedIndex", "1", TimeSpan.FromSeconds(3)), Is.True,
			"Field 1 must receive focus again before the reported tap.");

		var beforeCountText = App.WaitForElement("FocusEventCount").GetText();
		Assert.That(int.TryParse(beforeCountText, CultureInfo.InvariantCulture, out var beforeCount), Is.True,
			"Focus callback count must be numeric.");

		targetRect = App.WaitForElement(targetId).GetRect();
		toolbarRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar")).GetRect();
		doneRect = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeToolbar//XCUIElementTypeButton")).GetRect();
		overlapTop = Math.Max(targetRect.Y, toolbarRect.Y);
		overlapBottom = Math.Min(targetRect.Y + targetRect.Height, toolbarRect.Y + toolbarRect.Height);
		Assert.That(overlapBottom, Is.GreaterThan(overlapTop),
			"The target Entry must still occupy the Done accessory band after refocusing Field 1.");
		Assert.That(doneRect.Width, Is.GreaterThan(0));
		Assert.That(doneRect.Height, Is.GreaterThan(0));
		Assert.That(doneRect.X, Is.GreaterThanOrEqualTo(windowRect.X));
		Assert.That(doneRect.Y, Is.GreaterThanOrEqualTo(windowRect.Y));
		Assert.That(doneRect.X + doneRect.Width, Is.LessThanOrEqualTo(windowRect.X + windowRect.Width));
		Assert.That(doneRect.Y + doneRect.Height, Is.LessThanOrEqualTo(windowRect.Y + windowRect.Height));

		var tapX = targetRect.CenterX();
		var tapY = (overlapTop + overlapBottom) / 2;
		Assert.That(tapX, Is.InRange(targetRect.X, targetRect.X + targetRect.Width));
		Assert.That(tapY, Is.InRange(targetRect.Y, targetRect.Y + targetRect.Height));
		Assert.That(tapX < doneRect.X || tapX > doneRect.X + doneRect.Width ||
			tapY < doneRect.Y || tapY > doneRect.Y + doneRect.Height, Is.True,
			"The target Entry tap point must be outside the Done button.");

		App.TapCoordinates(tapX, tapY);

		var callbackObserved = App.WaitForTextToBePresentInElement(
			"FocusEventCount", (beforeCount + 1).ToString(CultureInfo.InvariantCulture), TimeSpan.FromSeconds(3));
		var afterCountText = App.WaitForElement("FocusEventCount").GetText();
		var lastFocusedText = App.WaitForElement("LastFocusedIndex").GetText();

		Assert.That(callbackObserved, Is.True,
			$"Entry in the Done accessory band did not receive focus after its visible area was tapped outside the Done button; " +
			$"target index was {targetIndex}, last focused index was {lastFocusedText}, " +
			$"callback count before={beforeCount}, after={afterCountText}.");
		Assert.That(lastFocusedText, Is.EqualTo(targetIndex.ToString(CultureInfo.InvariantCulture)),
			$"The post-trigger Focused callback identified Field {lastFocusedText} instead of Field {targetIndex}.");
	}
}
#endif

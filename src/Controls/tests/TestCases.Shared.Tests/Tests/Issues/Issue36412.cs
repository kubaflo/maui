#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public Issue36412(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	[Test]
	[Category(UITestCategories.Entry)]
	public void TappingVisibleEntryAboveNumericKeyboardMovesFocus()
	{
		const string field1Id = "Issue36412Field1";
		const string field7Id = "Issue36412Field7";
		const string focusTokenId = "Issue36412FocusToken";

		App.SetOrientationPortrait();

		var appFrame = App.WaitForElement("Issue36412Root").GetRect();
		Assert.That(appFrame.Height, Is.GreaterThan(appFrame.Width), $"The test requires portrait orientation, but the app frame was {appFrame}.");

		var field1Frame = App.WaitForElement(field1Id).GetRect();
		var initialField7Frame = App.WaitForElement(field7Id).GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(field1Frame.Width, Is.GreaterThan(0), $"Field 1 had an invalid native frame: {field1Frame}.");
			Assert.That(field1Frame.Height, Is.GreaterThan(0), $"Field 1 had an invalid native frame: {field1Frame}.");
			Assert.That(initialField7Frame.Width, Is.GreaterThan(0), $"Field 7 had an invalid native frame: {initialField7Frame}.");
			Assert.That(initialField7Frame.Height, Is.GreaterThan(0), $"Field 7 had an invalid native frame: {initialField7Frame}.");
		});

		App.TapCoordinates(field1Frame.X + field1Frame.Width / 2, field1Frame.Y + field1Frame.Height / 2);

		bool field1FocusObserved = App.WaitForTextToBePresentInElement(
			focusTokenId,
			"Count=1;Last=Field1",
			TimeSpan.FromSeconds(5));
		Assert.That(field1FocusObserved, Is.True, "Field 1 did not receive focus after tapping its native frame.");
		Assert.That(App.IsFocused(field1Id), Is.True, "Field 1 was not the active native element.");

		bool keyboardShown = App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5));
		Assert.That(keyboardShown, Is.True, "The software numeric keyboard did not appear after Field 1 received focus.");

		var accessoryFrame = App.WaitForElement("Toolbar").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(accessoryFrame.Width, Is.GreaterThan(0), $"The numeric keyboard accessory had an invalid native frame: {accessoryFrame}.");
			Assert.That(accessoryFrame.Height, Is.GreaterThan(0), $"The numeric keyboard accessory had an invalid native frame: {accessoryFrame}.");
		});

		var field7Element = App.WaitForElement(field7Id);
		var field7Frame = field7Element.GetRect();
		int tapX = field7Frame.X + field7Frame.Width / 2;
		int tapY = field7Frame.Y + field7Frame.Height / 2;

		Assert.Multiple(() =>
		{
			Assert.That(field7Element.IsDisplayed(), Is.True, "Field 7 was not visible after the numeric keyboard appeared.");
			Assert.That(field7Frame.Width, Is.GreaterThan(0), $"Field 7 had an invalid post-keyboard native frame: {field7Frame}.");
			Assert.That(field7Frame.Height, Is.GreaterThan(0), $"Field 7 had an invalid post-keyboard native frame: {field7Frame}.");
			Assert.That(tapX, Is.InRange(appFrame.Left, appFrame.Right - 1), $"Field 7's center X was outside the app frame. Field7={field7Frame}; App={appFrame}.");
			Assert.That(tapY, Is.InRange(appFrame.Top, appFrame.Bottom - 1), $"Field 7's center Y was outside the app frame. Field7={field7Frame}; App={appFrame}.");
			Assert.That(tapX, Is.LessThan(accessoryFrame.Right - accessoryFrame.Height), $"Field 7's center was not safely left of the accessory's trailing Done-button area. Field7={field7Frame}; Accessory={accessoryFrame}.");
		});

		string beforeToken = App.FindElement(focusTokenId).GetText() ?? "missing";
		App.TapCoordinates(tapX, tapY);

		string postTriggerToken = "Count=-1;Last=unsampled";
		bool field7FocusObserved = App.WaitForTextToBePresentInElement(
			focusTokenId,
			"Count=2;Last=Field7",
			TimeSpan.FromSeconds(3));
		postTriggerToken = App.FindElement(focusTokenId).GetText() ?? "missing";

		Assert.That(postTriggerToken, Is.Not.EqualTo("Count=-1;Last=unsampled"), "The post-trigger focus token was not sampled.");
		Assert.That(
			field7FocusObserved,
			Is.True,
			$"Field 7 did not receive focus after tapping its visible native frame. Before={beforeToken}; After={postTriggerToken}; Field1Frame={field1Frame}; Field7Frame={field7Frame}; AccessoryFrame={accessoryFrame}.");
		Assert.That(
			postTriggerToken,
			Is.EqualTo("Count=2;Last=Field7"),
			$"Field 7 focus produced an unexpected event token. Before={beforeToken}; After={postTriggerToken}; Field1Frame={field1Frame}; Field7Frame={field7Frame}; AccessoryFrame={accessoryFrame}.");
		Assert.Multiple(() =>
		{
			Assert.That(App.IsFocused(field7Id), Is.True, $"Field 7 was not the active native element. Before={beforeToken}; After={postTriggerToken}.");
			Assert.That(App.IsFocused(field1Id), Is.False, $"Field 1 retained native focus. Before={beforeToken}; After={postTriggerToken}.");
		});
	}
}
#endif

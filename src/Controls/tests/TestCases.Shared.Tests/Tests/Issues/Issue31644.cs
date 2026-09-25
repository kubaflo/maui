#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31644 : _IssuesUITest
{
	public override string Issue => "Input blocked to sibling ContentView on iOS";

	public Issue31644(TestDevice testDevice) : base(testDevice) { }

	[Test]
	[Category(UITestCategories.Layout)]
	public void HiddenTopContentViewDoesNotBlockBottomButton()
	{
		App.SetOrientationPortrait();

		var pageRect = App.WaitForElement("Issue31644Page").GetRect();
		Assert.That(pageRect.Height, Is.GreaterThan(pageRect.Width), "The issue requires portrait geometry.");

		App.WaitForElement("TopButton");
		var interactionRect = App.WaitForElement("InteractionArea").GetRect();
		var bottomRect = App.WaitForElement("BottomButton").GetRect();
		Assert.That(interactionRect.Height, Is.GreaterThanOrEqualTo(320));
		Assert.That(bottomRect.X, Is.GreaterThanOrEqualTo(interactionRect.X));
		Assert.That(bottomRect.Y, Is.GreaterThanOrEqualTo(interactionRect.Y));
		Assert.That(bottomRect.X + bottomRect.Width, Is.LessThanOrEqualTo(interactionRect.X + interactionRect.Width));
		Assert.That(bottomRect.Y + bottomRect.Height, Is.LessThanOrEqualTo(interactionRect.Y + interactionRect.Height));
		Assert.That(App.WaitForElement("TopTransitionLabel").GetText(), Is.EqualTo("-1"));
		Assert.That(App.WaitForElement("BottomClickCountLabel").GetText(), Is.EqualTo("0"));

		App.Tap("TopButton");

		var topCallbackRan = App.WaitForTextToBePresentInElement(
			"TopTransitionLabel",
			"1",
			timeout: TimeSpan.FromSeconds(3));
		Assert.That(topCallbackRan, Is.True, "Top button callback did not hide the top Grid.");
		App.WaitForNoElement("TopButton");

		interactionRect = App.WaitForElement("InteractionArea").GetRect();
		bottomRect = App.WaitForElement("BottomButton").GetRect();
		var tapX = interactionRect.X + interactionRect.Width / 2;
		var tapY = interactionRect.Y + interactionRect.Height / 2;
		Assert.That(tapX, Is.InRange(bottomRect.X, bottomRect.X + bottomRect.Width));
		Assert.That(tapY, Is.InRange(bottomRect.Y, bottomRect.Y + bottomRect.Height));

		App.TapCoordinates(tapX, tapY);

		var bottomCallbackRan = App.WaitForTextToBePresentInElement(
			"BottomClickCountLabel",
			"1",
			timeout: TimeSpan.FromSeconds(3));
		var countText = App.WaitForElement("BottomClickCountLabel").GetText();
		if (!int.TryParse(countText, out var count))
			Assert.Fail($"Bottom click count was not numeric: '{countText}'.");

		Assert.That(
			bottomCallbackRan,
			Is.True,
			$"Bottom button click count was {count} after exposed-area tap; expected 1.");
		Assert.That(count, Is.EqualTo(1));
		App.WaitForElement("TopButton");
	}
}
#endif

#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36816 : _IssuesUITest
{
	public Issue36816(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Clicks pass through ContentView to controls underneath on Android";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void ContentViewShouldBlockTapFromButtonUnderneath()
	{
		var overlay = App.WaitForElement("GreenOverlay", timeout: TimeSpan.FromSeconds(10));
		Assert.That(overlay, Is.Not.Null, "The green ContentView overlay was not found.");

		var overlayRect = overlay.GetRect();
		Assert.That(overlayRect.Width, Is.GreaterThan(0), "The green ContentView overlay must have a visible width.");
		Assert.That(overlayRect.Height, Is.GreaterThan(0), "The green ContentView overlay must have a visible height.");

		var clickCount = App.WaitForElement("ButtonClickCount", timeout: TimeSpan.FromSeconds(10));
		Assert.That(clickCount, Is.Not.Null, "The underlying button click count was not found.");

		Assert.That(clickCount.GetText(), Is.EqualTo("Underlying button clicks: 0"),
			"The page must finish its Loaded initialization before the tap.");

		App.TapCoordinates(
			overlayRect.X + overlayRect.Width / 2,
			overlayRect.Y + overlayRect.Height / 2);

		var overlayAfterTap = App.WaitForElement("GreenOverlay", timeout: TimeSpan.FromSeconds(2));
		Assert.That(overlayAfterTap, Is.Not.Null, "The green ContentView overlay disappeared after the tap.");

		Assert.That(overlayAfterTap.GetRect().Width, Is.GreaterThan(0),
			"The same green ContentView overlay must remain visible after the tap.");

		_ = App.WaitForTextToBePresentInElement(
			"ButtonClickCount",
			"Underlying button clicks: 1",
			timeout: TimeSpan.FromSeconds(2));

		var measuredCountElement = App.WaitForElement("ButtonClickCount", timeout: TimeSpan.FromSeconds(2));
		Assert.That(measuredCountElement, Is.Not.Null, "The underlying button click count disappeared after the tap.");

		var measuredCount = measuredCountElement.GetText();
		Assert.That(measuredCount, Is.EqualTo("Underlying button clicks: 0"),
			$"Underlying button received a click through the green ContentView. Measured count: '{measuredCount}'.");
	}
}
#endif

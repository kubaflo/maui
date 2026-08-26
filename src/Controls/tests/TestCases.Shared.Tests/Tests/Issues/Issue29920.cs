using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29920 : _IssuesUITest
{
	public Issue29920(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Android tap event passes through containers";

#if ANDROID
	[Test]
	[Category(UITestCategories.Gestures)]
	public void BoxViewBlocksTapGestureRecognizerOnUnderlyingStackLayout()
	{
		var ready = App.WaitForTextToBePresentInElement("Issue29920TapCount", "Underlying taps: 0");
		Assert.That(ready, Is.True, "The attached page must reach its ready state before input");

		var overlay = App.WaitForElement("Issue29920OverlayBoxView");
		var overlayRect = overlay.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(overlayRect.Width, Is.GreaterThan(0), "The overlaid BoxView must be attached and visible");
			Assert.That(overlayRect.Height, Is.GreaterThan(0), "The overlaid BoxView must be attached and visible");
		});

		App.TapCoordinates(overlayRect.CenterX(), overlayRect.CenterY());

		var tapCount = App.WaitForElement("Issue29920TapCount").GetText();
		Assert.That(tapCount, Is.Not.Null);
		Assert.That(
			tapCount,
			Is.EqualTo("Underlying taps: 0"),
			$"Underlying StackLayout TapGestureRecognizer received a tap through the overlaid BoxView. Measured count: {tapCount}; expected count: Underlying taps: 0; overlay bounds: {overlayRect}");
	}
#endif
}

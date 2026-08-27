#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32226 : _IssuesUITest
{
	public Issue32226(TestDevice device) : base(device) { }

	public override string Issue => "TapGestureRecognizer is suppressed by Android native Touch with Handled false";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void NativeTouchHandledFalseDoesNotSuppressTapGestureRecognizer()
	{
		App.WaitForElement("TapTarget");

		var initialNativeTouchStatus = App.WaitForElement("NativeTouchStatus").GetText();
		if (initialNativeTouchStatus is null)
			Assert.Fail("Native touch status was not available before the tap.");

		Assert.That(initialNativeTouchStatus, Is.EqualTo("Touch received: 0"));
		Assert.That(App.WaitForElement("TapTarget").GetText(),
			Is.EqualTo("Click me (Label TapGestureRecognizer)"));

		App.Tap("TapTarget");

		var nativeTouchTransitioned = App.WaitForTextToBePresentInElement(
			"NativeTouchStatus",
			"Touch received: 1",
			TimeSpan.FromSeconds(10));
		Assert.That(nativeTouchTransitioned, Is.True,
			"Issue32226 setup failed: the Android native Touch handler did not receive the pointer-down event.");

		App.WaitForTextToBePresentInElement(
			"TapTarget",
			"TapGestureRecognizer invoked",
			TimeSpan.FromSeconds(5));

		Assert.That(App.WaitForElement("TapTarget").GetText(),
			Is.EqualTo("TapGestureRecognizer invoked"),
			"Issue32226: TapGestureRecognizer did not update the tapped Label after Android native Touch with Handled=false.");
	}
}
#endif

#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29920 : _IssuesUITest
{
	public Issue29920(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Android tap event passes through overlapping containers";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void BoxViewAndStackLayoutBlockTapToObscuredGestureRecognizer()
	{
		App.WaitForElement("Issue29920OracleBox");
		App.WaitForElement("Issue29920ObscuredBox");
		App.WaitForTextToBePresentInElement("Issue29920Result", "Oracle=-1; Obscured=-1");

		App.Tap("Issue29920OracleBox");

		var oracleTapObserved = App.WaitForTextToBePresentInElement(
			"Issue29920Result",
			"Oracle=1; Obscured=0",
			TimeSpan.FromSeconds(5));
		Assert.That(oracleTapObserved, Is.True, "The unobscured BoxView TapGestureRecognizer callback was not observed");

		App.Tap("Issue29920ObscuredBox");

		var obscuredTapObserved = App.WaitForTextToBePresentInElement(
			"Issue29920Result",
			"Oracle=1; Obscured=1",
			TimeSpan.FromSeconds(3));

		Assert.That(
			obscuredTapObserved,
			Is.False,
			"Obscured StackLayout TapGestureRecognizer received a tap through the overlaid BoxView and StackLayout");
	}
}
#endif

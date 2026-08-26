#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36040 : _IssuesUITest
{
	public Issue36040(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Modal page has a title bar gap in Windows full-screen mode";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ModalContentBeginsAtFullScreenWindowTop()
	{
		var baselineElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("FullScreenTopBaseline");
			if (element is null)
				return null;

			return Math.Abs(element.GetRect().Y) <= 1 ? element : null;
		}, "The clean baseline control did not reach the full-screen window top.");
		if (baselineElement is null)
		{
			Assert.Fail("The full-screen top-edge baseline control was not found.");
			return;
		}

		var baselineRect = baselineElement.GetRect();
		Assert.That(baselineRect.Y, Is.EqualTo(0).Within(1), "The clean baseline control must begin at the verified full-screen window top.");
		Assert.That(baselineRect.Height, Is.GreaterThan(0), "The clean top-edge control must have a rendered height.");

		App.Tap("PushModalButton");

		var modalMarkerElement = App.WaitForElement("ModalPageMarker");
		if (modalMarkerElement is null)
		{
			Assert.Fail("The modal page marker was not found.");
			return;
		}

		Assert.That(modalMarkerElement.GetText(), Is.EqualTo("This is the MODAL PAGE (Red)"));

		var topEdgeElement = App.WaitForElement("TopEdgeButton");
		if (topEdgeElement is null)
		{
			Assert.Fail("The modal top-edge button was not found.");
			return;
		}

		Assert.That(topEdgeElement.GetText(), Is.EqualTo("Top Edge Button"));

		var modalRect = topEdgeElement.GetRect();
		Assert.That(modalRect.Height, Is.EqualTo(baselineRect.Height).Within(1), "The matching top-edge controls must have the same rendered height.");
		Assert.That(
			modalRect.Y,
			Is.EqualTo(baselineRect.Y).Within(1),
			$"Modal content must begin at the verified full-screen window top; expected {baselineRect.Y:0.##}, but was {modalRect.Y:0.##}.");
	}
}
#endif

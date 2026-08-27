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

	public override string Issue => "Modal page leaves a title bar gap in Windows full-screen mode";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ModalContentStartsAtFullScreenWindowTop()
	{
		App.WaitForElement("FullScreenReady");

		var mainLayout = App.WaitForElement("MainLayout");
		Assert.That(mainLayout, Is.Not.Null, "The blue main page layout should exist.");
		if (mainLayout is null)
			throw new AssertionException("The blue main page layout should exist.");

		var mainRect = mainLayout.GetRect();
		Assert.That(mainRect.Width, Is.GreaterThan(0), "The blue main page layout should have positive width.");
		Assert.That(mainRect.Height, Is.GreaterThan(0), "The blue main page layout should have positive height.");
		Assert.That(mainRect.Y, Is.EqualTo(0).Within(2),
			$"The blue main page should establish the full-screen window origin; observed top was {mainRect.Y}.");

		App.Tap("PushModalButton");

		var modalLabel = App.WaitForElement("ModalPageLabel");
		Assert.That(modalLabel, Is.Not.Null, "The red modal page label should exist after navigation.");
		if (modalLabel is null)
			throw new AssertionException("The red modal page label should exist after navigation.");

		Assert.That(modalLabel.GetText(), Is.EqualTo("This is the MODAL PAGE (Red)"));
		var modalLayout = App.WaitForElement("ModalLayout");
		Assert.That(modalLayout, Is.Not.Null, "The red modal page layout should exist after navigation.");
		if (modalLayout is null)
			throw new AssertionException("The red modal page layout should exist after navigation.");

		var modalRect = modalLayout.GetRect();
		Assert.That(modalRect.Width, Is.GreaterThan(0), "The red modal page layout should have positive width.");
		Assert.That(modalRect.Height, Is.GreaterThan(0), "The red modal page layout should have positive height.");
		Assert.That(modalRect.Y, Is.EqualTo(mainRect.Y).Within(2),
			$"Modal content should start at the full-screen window top; observed top was {modalRect.Y}, expected {mainRect.Y}.");
	}
}
#endif

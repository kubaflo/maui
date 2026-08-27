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
	[Category(UITestCategories.Window)]
	public void ModalCoversAndAcceptsInputAtFullScreenTopEdge()
	{
		var fullScreenStatus = App.WaitForElement("Issue36040FullScreenStatus");
		Assert.That(fullScreenStatus, Is.Not.Null);
		if (fullScreenStatus is null)
			throw new AssertionException("The full-screen status element was not found.");
		Assert.That(fullScreenStatus.GetText(), Is.EqualTo("Presenter kind: FullScreen"),
			"Windows AppWindow did not report the FullScreen presenter kind.");

		var mainRootElement = App.WaitForElement("Issue36040MainRoot");
		Assert.That(mainRootElement, Is.Not.Null);
		if (mainRootElement is null)
			throw new AssertionException("The full-screen reference element was not found.");
		var mainRect = mainRootElement.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(mainRect.Width, Is.GreaterThan(0), "The full-screen reference had no width.");
			Assert.That(mainRect.Height, Is.GreaterThan(0), "The full-screen reference had no height.");
			Assert.That(mainRect.X, Is.EqualTo(0).Within(1),
				$"The full-screen reference left edge was {mainRect.X}, not the viewport origin.");
			Assert.That(mainRect.Y, Is.EqualTo(0).Within(1),
				$"The full-screen reference top edge was {mainRect.Y}, not the viewport origin.");
		});

		App.Tap("Issue36040PushModalButton");

		var modalTitle = App.WaitForElement("Issue36040ModalTitle");
		Assert.That(modalTitle, Is.Not.Null);
		if (modalTitle is null)
			throw new AssertionException("The modal title element was not found.");
		Assert.That(modalTitle.GetText(), Is.EqualTo("This is the MODAL PAGE (Red)"));
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36040ModalStatus", "Modal state: 1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The modal transition did not complete.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36040PressCount", "Top-edge presses: 0", TimeSpan.FromSeconds(10)),
			Is.True,
			"The modal press count did not leave its sentinel state.");

		var modalRootElement = App.WaitForElement("Issue36040ModalRoot");
		var topEdgeButtonElement = App.WaitForElement("Issue36040TopEdgeButton");
		Assert.That(modalRootElement, Is.Not.Null);
		Assert.That(topEdgeButtonElement, Is.Not.Null);
		if (modalRootElement is null)
			throw new AssertionException("The modal root element was not found.");
		if (topEdgeButtonElement is null)
			throw new AssertionException("The top-edge button element was not found.");
		var modalRect = modalRootElement.GetRect();
		var buttonRect = topEdgeButtonElement.GetRect();
		Assert.That(modalRect.Width, Is.GreaterThan(0), "The modal surface had no width.");
		Assert.That(modalRect.Height, Is.GreaterThan(0), "The modal surface had no height.");
		Assert.That(buttonRect.Width, Is.GreaterThan(4), "The top-edge button had no tappable width.");
		Assert.That(buttonRect.Height, Is.GreaterThan(4), "The top-edge button had no tappable height.");

		var tapX = buttonRect.X + buttonRect.Width / 4;
		var tapY = mainRect.Y + Math.Min(buttonRect.Height / 4, 8);
		Assert.That(tapX, Is.InRange(buttonRect.X, buttonRect.X + buttonRect.Width),
			"The intended top-edge tap was outside the button's horizontal span.");
		Assert.That(tapY, Is.InRange(mainRect.Y, mainRect.Y + buttonRect.Height),
			"The intended top-edge tap was outside the button's expected full-screen vertical span.");

		App.TapCoordinates(tapX, tapY);
		var pressRegistered = App.WaitForTextToBePresentInElement(
			"Issue36040PressCount",
			"Top-edge presses: 1",
			TimeSpan.FromSeconds(5));
		var pressCountElement = App.WaitForElement("Issue36040PressCount");
		Assert.That(pressCountElement, Is.Not.Null);
		if (pressCountElement is null)
			throw new AssertionException("The press count element was not found.");
		var actualPressCount = pressCountElement.GetText();

		Assert.Multiple(() =>
		{
			Assert.That(modalRect.Y, Is.EqualTo(mainRect.Y).Within(1),
				$"Modal top edge did not cover the full-screen surface. Main top: {mainRect.Y}; modal top: {modalRect.Y}.");
			Assert.That(pressRegistered, Is.True,
				$"The top-edge button did not receive the mouse tap. Actual state: {actualPressCount}.");
		});
	}
}
#endif

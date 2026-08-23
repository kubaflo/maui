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

	public override string Issue => "Modal page has a title bar gap in full-screen mode on Windows";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ModalContentCoversFullScreenWindow()
	{
		const int tolerance = 2;

		var cleanRootElement = App.WaitForElement("Issue36040MainRoot");
		App.WaitForElement("Issue36040MainCaption");
		App.WaitForElement("PushModalButton");
		var cleanRoot = cleanRootElement.GetRect();

		Assert.That(cleanRoot.Width, Is.GreaterThan(0), "The clean full-screen root must have a nonzero width.");
		Assert.That(cleanRoot.Height, Is.GreaterThan(0), "The clean full-screen root must have a nonzero height.");
		Assert.That(cleanRoot.Y, Is.EqualTo(0).Within(tolerance),
			$"The clean root must begin at the full-screen window top; cleanTop={cleanRoot.Y}, tolerance={tolerance}.");

		App.Tap("PushModalButton");

		var modalRootElement = App.WaitForElement("Issue36040ModalRoot");
		var modalTopButtonElement = App.WaitForElement("ModalTopButton");
		var modalRoot = modalRootElement.GetRect();
		var modalTopButton = modalTopButtonElement.GetRect();

		Assert.That(modalRoot.Width, Is.GreaterThan(0), "The modal root must have a nonzero width.");
		Assert.That(modalRoot.Height, Is.GreaterThan(0), "The modal root must have a nonzero height.");
		Assert.That(modalTopButton.Y, Is.GreaterThanOrEqualTo(modalRoot.Y),
			"The modal top button must be positioned inside the modal root.");
		Assert.That(modalTopButton.Y + modalTopButton.Height, Is.LessThanOrEqualTo(modalRoot.Y + modalRoot.Height),
			"The modal top button must fit inside the modal root.");
		Assert.That(modalRoot.X, Is.EqualTo(cleanRoot.X).Within(tolerance),
			"The modal root must retain the clean root's horizontal placement.");
		Assert.That(modalRoot.Width, Is.EqualTo(cleanRoot.Width).Within(tolerance),
			"The modal root must retain the clean root's full-window width.");
		Assert.That(modalRoot.Y, Is.EqualTo(cleanRoot.Y).Within(tolerance),
			$"Modal content top must match the clean full-screen root top; cleanTop={cleanRoot.Y}, modalTop={modalRoot.Y}, tolerance={tolerance}.");
		Assert.That(modalRoot.Y, Is.EqualTo(0).Within(tolerance),
			$"The modal root must begin at the absolute full-screen top; modalTop={modalRoot.Y}, tolerance={tolerance}.");
	}
}
#endif

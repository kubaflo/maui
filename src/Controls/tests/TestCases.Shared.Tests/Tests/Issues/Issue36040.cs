#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36040 : _IssuesUITest
{
	const double TopEdgeTolerance = 2;

	public Issue36040(TestDevice device) : base(device) { }

	public override string Issue => "Modal page leaves a title bar gap in full-screen mode";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ModalPageCoversFullScreenWindowFromTopEdge()
	{
		var fullScreenReady = App.WaitForElement("Issue36040FullScreenReady");
		Assert.That(fullScreenReady.GetText(), Is.EqualTo("1"),
			"The full-screen transition did not complete.");

		var mainButton = App.WaitForElement("Issue36040PushModalButton");
		var mainRect = mainButton.GetRect();
		Assert.That(mainRect.Y, Is.EqualTo(0).Within(TopEdgeTolerance),
			$"The clean full-screen page did not start at y=0: actual={mainRect.Y}.");

		App.Tap("Issue36040PushModalButton");

		var modalLoaded = App.WaitForElement("Issue36040ModalLoaded");
		Assert.That(modalLoaded.GetText(), Is.EqualTo("1"),
			"The modal Loaded transition did not complete.");

		var modalButton = App.WaitForElement("Issue36040ModalTopEdgeButton");
		Assert.That(modalButton.GetText(), Is.EqualTo("Modal Top Edge"),
			"The intended modal top-row button was not found.");

		var modalRect = modalButton.GetRect();
		Assert.That(modalRect.Y, Is.EqualTo(0).Within(TopEdgeTolerance),
			$"Modal top edge did not cover the full-screen window: baseline={mainRect.Y}, modal={modalRect.Y}, expected=0.");
	}
}
#endif

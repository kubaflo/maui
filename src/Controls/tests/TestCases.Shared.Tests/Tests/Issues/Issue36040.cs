#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36040 : _IssuesUITest
{
	public override string Issue => "[Windows] Full-screen modal page reserves title bar space";

	public Issue36040(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void FullScreenModalStartsAtWindowContentOrigin()
	{
		Assert.That(App.WaitForElement("ReadyLabel", timeout: TimeSpan.FromSeconds(15)).GetText(),
			Is.EqualTo("Ready: full-screen main page"));
		App.WaitForElement("MainPageLabel");
		var mainPage = App.WaitForElement("MainPage").GetRect();
		App.Tap("PushModalButton");

		App.WaitForElement("ModalTopButton", timeout: TimeSpan.FromSeconds(15));
		App.WaitForElement("ModalPageLabel");
		var modalPage = App.WaitForElement("ModalPage").GetRect();

		var gap = modalPage.Y - mainPage.Y;
		Assert.That(gap, Is.EqualTo(0).Within(1),
			$"Modal native top gap was {gap:0} px; expected 0 +/- 1 px after PushModalAsync in full-screen mode.");
	}
}
#endif

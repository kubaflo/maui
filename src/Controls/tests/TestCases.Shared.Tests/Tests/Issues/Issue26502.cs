#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26502 : _IssuesUITest
{
	public Issue26502(TestDevice device) : base(device)
	{
	}

	public override string Issue => "WindowManagerFlags.Secure does not block screenshots on modal pages";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void SecureFlagIsAppliedToModalWindow()
	{
		App.WaitForElement("Issue26502RootSurface");
		App.Tap("Issue26502OpenModal");
		App.WaitForElement("Issue26502ModalTitle");
		App.WaitForElement("Issue26502ModalButton");
		Assert.That(App.FindElements("Issue26502OpenModal"), Is.Empty, "PushModalAsync did not replace the root page with the modal page.");
		App.WaitForTextToBePresentInElement("Issue26502ModalDescription", "Modal secure flag evaluated:");

		var result = App.FindElement("Issue26502ModalDescription").GetText();
		Assert.That(result, Is.Not.Null);
		Assert.That(result, Is.EqualTo("Modal secure flag evaluated: True"), "Modal Android window did not inherit FLAG_SECURE.");
	}
}
#endif

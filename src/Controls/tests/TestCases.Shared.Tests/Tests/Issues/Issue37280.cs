#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37280 : _IssuesUITest
{
	public override string Issue => "Crash at Window Close";

	public Issue37280(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void LogoutShouldOpenLoadedLoginWindow()
	{
		Assert.That(App.WaitForElement("ResultStatus").GetText(),
			Is.EqualTo("NO BUG: The application is ready for the close-window trigger."));
		Assert.That(App.WaitForElement("ImportStatus").GetText(), Is.EqualTo("Import is idle."));
		App.WaitForElement("StartImportButton");
		App.WaitForElement("EjectDeviceButton");
		App.WaitForElement("RetryImportButton");
		App.WaitForElement("LogoutButton");

		App.Tap("StartImportButton");
		Assert.That(App.WaitForElement("ImportStatus").GetText(), Is.EqualTo("Import is active."));

		App.Tap("EjectDeviceButton");
		Assert.That(App.WaitForElement("ImportStatus").GetText(),
			Is.EqualTo("Import failure was caught after device ejection."));

		App.Tap("RetryImportButton");
		Assert.That(App.WaitForElement("ImportStatus").GetText(),
			Is.EqualTo("Retry failed because the device remains ejected."));

		App.Tap("LogoutButton");

		var loginStatus = App.WaitForElement("LoginWindowStatus", timeout: TimeSpan.FromSeconds(10));
		Assert.That(loginStatus, Is.Not.Null);
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"LoginWindowStatus",
				"Login window loaded.",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True);
		Assert.That(App.FindElement("LoginWindowStatus").GetText(), Is.EqualTo("Login window loaded."));
	}
}
#endif

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
	public void ModalWindowInheritsSecureFlag()
	{
		var activityInspectionCompleted = App.WaitForTextToBePresentInElement(
			"ActivitySecureStatus",
			"Secure=",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(activityInspectionCompleted, Is.True, "The activity window inspection did not complete.");

		var activityStatus = App.WaitForElement("ActivitySecureStatus").GetText();
		if (activityStatus is null)
		{
			Assert.Fail("The activity secure status did not contain text.");
		}

		Assert.That(activityStatus, Is.EqualTo("ActivityWindow=True;Secure=True"), "The activity window must be secure before opening the modal page.");

		App.Tap("OpenModalPage");
		App.WaitForElement("ModalPageContent");

		var inspectionCompleted = App.WaitForTextToBePresentInElement(
			"ModalSecureStatus",
			"Secure=",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(inspectionCompleted, Is.True, "The modal window inspection did not complete after the modal Loaded transition.");

		var modalStatus = App.WaitForElement("ModalSecureStatus").GetText();
		if (modalStatus is null)
		{
			Assert.Fail("The modal secure status did not contain text.");
		}

		Assert.That(
			modalStatus,
			Does.StartWith("Loaded=True;Handler=True;Dialog=True;Distinct=True;"),
			$"Modal inspection setup failed: {modalStatus}");
		Assert.That(
			modalStatus,
			Is.EqualTo("Loaded=True;Handler=True;Dialog=True;Distinct=True;Secure=True"),
			"Modal window secure flag was False; expected True after PushModalAsync.");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30388 : _IssuesUITest
{
	public Issue30388(TestDevice device) : base(device)
	{
	}

	public override string Issue => "COMException during OnLaunched in Debug";

	[Test]
	[Category(UITestCategories.Button)]
	public void GetActivatedEventArgsShouldNotThrow()
	{
		var hostState = App.WaitForElement("HostState").GetText();
		Assert.That(hostState, Is.Not.Null);
		Assert.That(hostState, Is.EqualTo("Windows AppInstance activation check"));

		var initialInvocationStatus = App.WaitForElement("InvocationStatus").GetText();
		Assert.That(initialInvocationStatus, Is.Not.Null);
		Assert.That(initialInvocationStatus, Is.EqualTo("Not invoked"));

		var initialExceptionStatus = App.WaitForElement("ExceptionStatus").GetText();
		Assert.That(initialExceptionStatus, Is.Not.Null);
		Assert.That(initialExceptionStatus, Is.EqualTo("Not observed"));

		App.Tap("TriggerButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("InvocationStatus", "API invocation reached"),
			Is.True,
			"The Windows button click did not reach the API invocation");
		App.WaitForNoElement(
			"Not observed",
			"AppInstance.GetActivatedEventArgs did not complete after the Windows button click");

		var exceptionStatus = App.WaitForElement("ExceptionStatus").GetText();
		Assert.That(exceptionStatus, Is.Not.Null);
		Assert.That(
			exceptionStatus,
			Is.EqualTo("None"),
			$"AppInstance.GetActivatedEventArgs threw after the Windows button click: {exceptionStatus}");
	}
}
#endif

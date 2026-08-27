#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	const string OpenChildWindowButton = "Issue34057OpenChildWindowButton";
	const string Telemetry = "Issue34057Telemetry";
	const string InitialTelemetry = "Loaded=-1;SceneVerified=-1;Disappearing=-1;CloseReturned=-1;AnimationReturned=-1;ExceptionType=None;ObjectName=None;Completed=-1";

	public Issue34057(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Animation accesses disposed services after closing a child window";

	[Test]
	[Category(UITestCategories.Animation)]
	public void AnimationAfterClosedChildWindowDoesNotAccessDisposedServices()
	{
		var initialElement = App.WaitForElement(Telemetry);
		if (initialElement is null)
		{
			Assert.Fail("The lifecycle telemetry element was not found.");
			return;
		}

		Assert.That(initialElement.GetText(), Is.EqualTo(InitialTelemetry));

		App.WaitForElement(OpenChildWindowButton);
		App.Tap(OpenChildWindowButton);

		Assert.That(
			App.WaitForTextToBePresentInElement(Telemetry, "Completed=1", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The child-window lifecycle did not complete.");

		var finalElement = App.FindElement(Telemetry);
		if (finalElement is null)
		{
			Assert.Fail("The lifecycle telemetry element was not found after the child window closed.");
			return;
		}

		var finalTelemetry = finalElement.GetText();
		Assert.That(finalTelemetry, Does.Contain("Loaded=1;SceneVerified=1;Disappearing=1;CloseReturned=1;"));
		Assert.That(
			finalTelemetry,
			Does.Contain("AnimationReturned=1;ExceptionType=None;ObjectName=None;Completed=1"),
			$"Animation after closed child window should not access disposed IServiceProvider. Telemetry: {finalTelemetry}");
	}
}
#endif

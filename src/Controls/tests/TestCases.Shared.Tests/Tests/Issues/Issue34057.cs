#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	const string RunScenarioButtonId = "Issue34057RunScenarioButton";
	const string TelemetryLabelId = "Issue34057TelemetryLabel";
	const string InitialTelemetry = "Loaded=0; PlatformViewReady=0; Destroyed=0; Dispatched=0; Attempted=0; InvocationReturned=0; Exception=None";

	public Issue34057(TestDevice device) : base(device)
	{
	}

	public override string Issue => "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window";

	[Test]
	[Category(UITestCategories.Animation)]
	public void AnimationAfterChildWindowDestructionDoesNotUseDisposedServices()
	{
		var initialTelemetryElement = App.WaitForElement(TelemetryLabelId);
		if (initialTelemetryElement is null)
		{
			Assert.Fail("The lifecycle telemetry element was not found.");
			return;
		}

		Assert.That(initialTelemetryElement.GetText(), Is.EqualTo(InitialTelemetry));

		App.WaitForElement(RunScenarioButtonId);
		App.Tap(RunScenarioButtonId);

		Assert.That(
			App.WaitForTextToBePresentInElement(TelemetryLabelId, "Attempted=1", timeout: TimeSpan.FromSeconds(20)),
			Is.True,
			"The post-close animation invocation was not attempted.");

		var finalTelemetryElement = App.FindElement(TelemetryLabelId);
		if (finalTelemetryElement is null)
		{
			Assert.Fail("The lifecycle telemetry element disappeared after the child window closed.");
			return;
		}

		var finalTelemetry = finalTelemetryElement.GetText();
		Assert.That(finalTelemetry, Does.Contain("Loaded=1;"), $"The child page did not load exactly once. Telemetry: {finalTelemetry}");
		Assert.That(finalTelemetry, Does.Contain("PlatformViewReady=1;"), $"The save popup was not attached to a native view. Telemetry: {finalTelemetry}");
		Assert.That(finalTelemetry, Does.Contain("Destroyed=1;"), $"The child window was not destroyed exactly once. Telemetry: {finalTelemetry}");
		Assert.That(finalTelemetry, Does.Contain("Dispatched=1;"), $"The post-close dispatcher callback did not run exactly once. Telemetry: {finalTelemetry}");
		Assert.That(finalTelemetry, Does.Contain("Attempted=1;"), $"The animation was not attempted exactly once. Telemetry: {finalTelemetry}");

		Assert.Multiple(() =>
		{
			Assert.That(
				finalTelemetry,
				Does.Contain("InvocationReturned=1;"),
				$"AnimationExtensions.Animate should not throw ObjectDisposedException after child window destruction. Telemetry: {finalTelemetry}");
			Assert.That(
				finalTelemetry,
				Does.Contain("Exception=None"),
				$"AnimationExtensions.Animate should not throw ObjectDisposedException after child window destruction. Telemetry: {finalTelemetry}");
		});
	}
}
#endif

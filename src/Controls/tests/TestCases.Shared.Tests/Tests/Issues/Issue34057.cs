#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	public Issue34057(TestDevice device) : base(device)
	{
	}

	public override string Issue => "AnimationManager ObjectDisposedException when closing a child window";

	[Test]
	[Category(UITestCategories.Animation)]
	public void AnimationAfterChildWindowTeardownDoesNotUseDisposedServices()
	{
		var initialResult = App.WaitForElement("Issue34057Result")
			?? throw new InvalidOperationException("Issue34057 result element was not found before the trigger.");

		Assert.That(initialResult.GetText(), Is.EqualTo("NotTriggered"));

		App.Tap("Issue34057OpenChildWindow");

		var lifecycleElement = App.WaitForElement("Issue34057LifecycleComplete")
			?? throw new InvalidOperationException("Issue34057 lifecycle completion element was not found.");

		var lifecycleState = lifecycleElement.GetText();
		Assert.That(
			lifecycleState,
			Is.EqualTo("Complete: Loaded=True; Disappearing=True; WindowRemoved=True; Dispatch=True"),
			$"Issue34057 child-window lifecycle did not complete as expected; measured state: {lifecycleState}");

		var resultElement = App.FindElement("Issue34057Result")
			?? throw new InvalidOperationException("Issue34057 result element was not found after the trigger.");

		var exceptionState = resultElement.GetText();
		Assert.That(
			exceptionState,
			Is.EqualTo("None"),
			$"Issue34057 animation after child-window teardown threw; lifecycle: {lifecycleState}; exception: {exceptionState}");
	}
}
#endif

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
	[Category(UITestCategories.Window)]
	public void AnimationExtensionsUsesIAnimatableAfterChildWindowDestruction()
	{
		AssertText("Issue34057AnimationState", "-1/NotStarted");
		AssertText("Issue34057AnimationAttemptCount", "0");
		AssertText("Issue34057CreatedCount", "0");
		AssertText("Issue34057DestroyingCount", "0");
		AssertText("Issue34057CreatedWindowIdentity", "-1/None");

		App.Tap("Issue34057OpenChildWindowButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue34057CreatedCount", "1", TimeSpan.FromSeconds(15)),
			Is.True,
			"Child Window.Created should be observed before closing the window.");
		AssertText("Issue34057CreatedWindowIdentity", "1/ChildWindow-1");

		App.Tap("Issue34057CloseChildWindowButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue34057DestroyingCount", "1", TimeSpan.FromSeconds(15)),
			Is.True,
			"Child Window.Destroying should be observed.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue34057AnimationAttemptCount", "1", TimeSpan.FromSeconds(15)),
			Is.True,
			"The post-destruction animation attempt should reach a concrete result.");

		var animationState = App.WaitForElement("Issue34057AnimationState");
		Assert.That(animationState, Is.Not.Null, "The animation state element should exist.");
		var observedState = animationState!.GetText();
		Assert.That(
			observedState,
			Is.EqualTo("AnimationCompleted"),
			$"AnimationExtensions.Animate should complete after child-window destruction; observed state: {observedState}");

		void AssertText(string automationId, string expectedText)
		{
			var element = App.WaitForElement(automationId);
			Assert.That(element, Is.Not.Null, $"The '{automationId}' element should exist.");
			Assert.That(element!.GetText(), Is.EqualTo(expectedText));
		}
	}
}
#endif

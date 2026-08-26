#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	public override string Issue => "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window";

	public Issue34057(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void PopupAnimationReturnsAfterChildWindowIsDestroyed()
	{
		var triggerButton = App.WaitForElement("RunTriggerButton");
		if (triggerButton is null)
			throw new AssertionException("The child-window trigger button was not found.");

		var initialLoadedCount = App.WaitForElement("LoadedCountLabel");
		if (initialLoadedCount is null)
			throw new AssertionException("The loaded callback count was not found.");
		Assert.That(initialLoadedCount.GetText(), Is.EqualTo("Loaded callbacks: 0"));

		var initialDestroyingCount = App.WaitForElement("DestroyingCountLabel");
		if (initialDestroyingCount is null)
			throw new AssertionException("The destroying callback count was not found.");
		Assert.That(initialDestroyingCount.GetText(), Is.EqualTo("Destroying callbacks: 0"));

		var initialContinuationCount = App.WaitForElement("ContinuationCountLabel");
		if (initialContinuationCount is null)
			throw new AssertionException("The continuation callback count was not found.");
		Assert.That(initialContinuationCount.GetText(), Is.EqualTo("Continuation callbacks: 0"));

		var initialAnimationState = App.WaitForElement("AnimationStateLabel");
		if (initialAnimationState is null)
			throw new AssertionException("The animation state was not found.");
		Assert.That(initialAnimationState.GetText(), Is.EqualTo("Animation state: NotStarted"));

		App.Tap("RunTriggerButton");

		var loadedObserved = App.WaitForTextToBePresentInElement(
			"LoadedCountLabel",
			"Loaded callbacks: 1",
			TimeSpan.FromSeconds(10));
		Assert.That(loadedObserved, Is.True, "The child page Loaded callback did not run.");

		var destroyingObserved = App.WaitForTextToBePresentInElement(
			"DestroyingCountLabel",
			"Destroying callbacks: 1",
			TimeSpan.FromSeconds(10));
		Assert.That(destroyingObserved, Is.True, "The child window Destroying callback did not run.");

		var continuationObserved = App.WaitForTextToBePresentInElement(
			"ContinuationCountLabel",
			"Continuation callbacks: 1",
			TimeSpan.FromSeconds(10));
		Assert.That(continuationObserved, Is.True, "The post-destruction animation continuation did not run.");

		var animationState = App.WaitForElement("AnimationStateLabel");
		if (animationState is null)
			throw new AssertionException("The post-destruction animation state was not found.");

		var observedState = animationState.GetText();
		Assert.That(
			observedState,
			Is.EqualTo("Animation state: AnimateReturned"),
			$"Popup Animate should return after child window destruction without resolving a disposed IServiceProvider; observed state: {observedState}");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	public Issue34057(TestDevice device) : base(device) { }

	public override string Issue => "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window";

	[Test]
	[Category(UITestCategories.Animation)]
	public void PopupAnimationCompletesAfterChildWindowCloses()
	{
		var initialChildState = App.WaitForElement("ChildLoadedStatus");
		Assert.That(initialChildState, Is.Not.Null);
		Assert.That(initialChildState!.GetText(), Is.EqualTo("Child not loaded"));

		var initialPopupState = App.WaitForElement("PopupIdentityStatus");
		Assert.That(initialPopupState, Is.Not.Null);
		Assert.That(initialPopupState!.GetText(), Is.EqualTo("Popup not created"));

		var initialDestructionState = App.WaitForElement("ChildDestructionStatus");
		Assert.That(initialDestructionState, Is.Not.Null);
		Assert.That(initialDestructionState!.GetText(), Is.EqualTo("Not destroyed"));

		var initialReactivationState = App.WaitForElement("RootReactivationStatus");
		Assert.That(initialReactivationState, Is.Not.Null);
		Assert.That(initialReactivationState!.GetText(), Is.EqualTo("Not reactivated"));

		var initialAnimationState = App.WaitForElement("AnimationState");
		Assert.That(initialAnimationState, Is.Not.Null);
		Assert.That(initialAnimationState!.GetText(), Is.EqualTo("Not started"));

		App.WaitForElement("OpenChildButton");
		App.Tap("OpenChildButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ChildLoadedStatus", "Child page loaded"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("PopupIdentityStatus", "SavePopup loaded"),
			Is.True);

		App.WaitForElement("CloseChildButton");
		App.Tap("CloseChildButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ChildDestructionStatus", "Destroyed"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("RootReactivationStatus", "Reactivated"),
			Is.True);

		var animationFinished = App.WaitForElement("AnimationFinishedStatus");
		Assert.That(animationFinished, Is.Not.Null);

		var animationState = App.WaitForElement("AnimationState");
		Assert.That(animationState, Is.Not.Null);
		var observedState = animationState!.GetText();
		Assert.That(observedState, Is.Not.EqualTo("Not started"));
		Assert.That(
			observedState,
			Is.EqualTo("Completed"),
			$"Popup hide animation after child-window close must complete without ObjectDisposedException; observed state: {observedState}");
	}
}
#endif

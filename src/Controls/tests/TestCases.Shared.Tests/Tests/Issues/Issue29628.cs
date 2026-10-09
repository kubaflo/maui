#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29628 : _IssuesUITest
{
	public override string Issue => "Deadlock with modal navigation and animation";

	public Issue29628(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Animation)]
	public void AnimationCancellationCompletesOnceDuringModalNavigation()
	{
		var animationStartedElement = App.WaitForElement("AnimationStartedToken");
		if (animationStartedElement is null)
		{
			Assert.Fail("The attached animation start token was not found.");
			return;
		}

		Assert.That(animationStartedElement.GetText(), Is.EqualTo("AttachedAnimationStarted"));

		var initialCountElement = App.WaitForElement("CancellationCount");
		if (initialCountElement is null)
		{
			Assert.Fail("The initial cancellation count was not found.");
			return;
		}

		Assert.That(initialCountElement.GetText(), Is.EqualTo("-1"));

		App.Tap("OpenFastModal");

		var modalLoadedElement = App.WaitForElement("ModalLoadedToken", timeout: TimeSpan.FromSeconds(10));
		if (modalLoadedElement is null)
		{
			Assert.Fail("The modal Loaded transition was not observed.");
			return;
		}

		Assert.That(modalLoadedElement.GetText(), Is.EqualTo("ModalLoaded"));

		var dispatchElement = App.WaitForElement("PostCancellationDispatchToken", timeout: TimeSpan.FromSeconds(10));
		if (dispatchElement is null)
		{
			Assert.Fail("The post-cancellation UI-thread dispatch did not complete.");
			return;
		}

		Assert.That(dispatchElement.GetText(), Is.EqualTo("PostCancellationDispatchCompleted"));

		var cancellationCountElement = App.WaitForElement("CancellationCount");
		if (cancellationCountElement is null)
		{
			Assert.Fail("The post-trigger cancellation count was not found.");
			return;
		}

		var cancellationCountText = cancellationCountElement.GetText();
		if (!int.TryParse(cancellationCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cancellationCount))
			Assert.Fail($"The cancellation count '{cancellationCountText}' was not an integer.");

		Assert.That(cancellationCount, Is.Not.EqualTo(-1), "The animation cancellation callback was not observed.");
		Assert.That(
			cancellationCount,
			Is.EqualTo(1),
			$"Animation cancellation callback re-entered during modal navigation: observed {cancellationCount}, expected 1.");
	}
}
#endif

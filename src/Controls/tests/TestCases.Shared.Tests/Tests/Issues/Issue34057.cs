#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34057 : _IssuesUITest
{
	public Issue34057(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "[Windows] AnimationManager ObjectDisposedException when closing a window";

	[Test]
	[Category(UITestCategories.Window)]
	public void ClosingChildWindowDoesNotUseDisposedAnimationServices()
	{
		var initialResultElement = App.WaitForElement("Issue34057ResultLabel");
		if (initialResultElement is null)
			throw new AssertionException("The initial transition result element was not found.");

		Assert.That(initialResultElement.GetText(), Is.EqualTo("NOT_RUN"));

		App.WaitForElement("Issue34057OpenChildWindowButton");
		App.Tap("Issue34057OpenChildWindowButton");

		var completionElement = App.WaitForElement("Issue34057CompletionLabel");
		if (completionElement is null)
			throw new AssertionException("The child-window completion element was not found.");

		Assert.That(completionElement.GetText(), Is.EqualTo("Child window close completed"));

		var resultElement = App.WaitForElement("Issue34057ResultLabel");
		if (resultElement is null)
			throw new AssertionException("The completed transition result element was not found.");

		var result = resultElement.GetText();
		Assert.That(result, Does.Contain("popupLoaded=True"));
		Assert.That(result, Does.Contain("windowDestroying=same"));
		Assert.That(result, Does.Contain("postDestructionCallback=True"));
		Assert.That(result, Does.Contain("animationAttempted=True"));
		Assert.That(
			result,
			Does.EndWith("exception=none"),
			$"Child-window popup animation must not resolve services from a disposed window scope. Observed state: {result}");
	}
}
#endif

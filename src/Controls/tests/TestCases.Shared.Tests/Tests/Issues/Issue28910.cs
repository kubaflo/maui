#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28910 : _IssuesUITest
{
	public override string Issue => "SetBlur has no effect on iOS";

	public Issue28910(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void GraphicsViewCanvasShouldApplyBlurAfterDrawableReplacement()
	{
		var patternView = App.WaitForElement("PatternView");
		if (patternView is null)
			throw new AssertionException("The Issue28910 GraphicsView was not found.");

		App.WaitForTextToBePresentInElement(
			"DrawStatusLabel",
			"InitialCompleted=True",
			timeout: TimeSpan.FromSeconds(10));

		var initialStatusElement = App.FindElement("DrawStatusLabel");
		if (initialStatusElement is null)
			throw new AssertionException("The Issue28910 initial draw status was not found.");

		var initialStatus = initialStatusElement.GetText();
		if (initialStatus is null)
			throw new AssertionException("The Issue28910 initial draw status was empty.");

		Assert.That(initialStatus, Does.Contain("BlurDraws=0").And.Contain("SupportsBlur=unset"),
			$"The initial draw did not preserve the blur capability sentinel. {initialStatus}");

		App.Tap("RenderBlurButton");
		App.WaitForTextToBePresentInElement(
			"DrawStatusLabel",
			"BlurCompleted=True",
			timeout: TimeSpan.FromSeconds(10));

		var postTriggerStatusElement = App.FindElement("DrawStatusLabel");
		if (postTriggerStatusElement is null)
			throw new AssertionException("The Issue28910 post-trigger draw status was not found.");

		var postTriggerStatus = postTriggerStatusElement.GetText();
		if (postTriggerStatus is null)
			throw new AssertionException("The Issue28910 post-trigger draw status was empty.");

		Assert.That(postTriggerStatus, Does.Contain("SupportsBlur=True"),
			$"Issue28910: the post-trigger iOS GraphicsView canvas cannot apply SetBlur(10). {postTriggerStatus}");
	}
}
#endif

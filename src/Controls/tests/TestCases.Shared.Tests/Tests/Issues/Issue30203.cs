using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30203 : _IssuesUITest
{
	public override string Issue => "[Windows] Unable to adjust the window background color visible when navigating";

	public Issue30203(TestDevice device) : base(device)
	{
	}

#if WINDOWS
	[Test]
	[Category(UITestCategories.Navigation)]
	public void NavigationDoesNotExposePlatformBackground()
	{
		App.WaitForElement("PageAMarker");
		var observedBackgrounds = new List<string>();

		PushAndAwaitCompletion(0, observedBackgrounds);
		PopAndAwaitCompletion(0);
		PushAndAwaitCompletion(1, observedBackgrounds);
		PopAndAwaitCompletion(1);
		PushAndAwaitCompletion(2, observedBackgrounds);

		Assert.That(
			observedBackgrounds,
			Is.All.EqualTo("Expected:#FFF4E6;Actual:#FFF4E6"),
			"Navigation transition frame background did not match the app background.");
	}

	void PushAndAwaitCompletion(int completion, List<string> observedBackgrounds)
	{
		App.Tap("NavigateButton");
		App.WaitForElement("PageBMarker");
		Assert.That(
			App.WaitForTextToBePresentInElement("PushCompletion", $"Completed:{completion}", TimeSpan.FromSeconds(10)),
			Is.True,
			$"Push completion token {completion} was not observed.");
		Assert.That(
			App.WaitForTextToBePresentInElement("FrameBackground", "Expected:#FFF4E6;Actual:", TimeSpan.FromSeconds(10)),
			Is.True,
			$"Frame background observation {completion} was not produced.");

		var observedBackground = App.FindElement("FrameBackground").GetText();
		if (observedBackground is null)
			throw new AssertionException($"Frame background observation {completion} was null.");

		observedBackgrounds.Add(observedBackground);
	}

	void PopAndAwaitCompletion(int completion)
	{
		App.Tap("ReturnButton");
		App.WaitForElement("PageAMarker");
		Assert.That(
			App.WaitForTextToBePresentInElement("PopCompletion", $"Completed:{completion}", TimeSpan.FromSeconds(10)),
			Is.True,
			$"Pop completion token {completion} was not observed.");
	}
#endif
}

#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36800 : _IssuesUITest
{
	const string Sentinel = "SENTINEL: diagnostic not run";
	const string PassResult = "PASS: Inset-aware native ScrollView range is non-positive.";

	public Issue36800(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ScrollView reserves the safe area twice on iOS";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void FittingContentHasNoInsetAwareScrollRange()
	{
		App.SetOrientationPortrait();
		App.CloseApp();
		App.LaunchApp();
		App.WaitForGoToTestButtonWithRecovery(Issue);
		App.NavigateTo(Issue);

		App.WaitForElement("Issue36800Content");
		App.WaitForElement("Issue36800DumpButton");
		Assert.That(App.WaitForElement("Issue36800Result").GetText(), Is.EqualTo(Sentinel),
			"The native diagnostic must not run before the reported gesture");

		var scrollRect = App.WaitForElement("Issue36800Scroll").GetRect();
		App.DragCoordinates(
			scrollRect.CenterX(),
			scrollRect.Y + (scrollRect.Height * 3 / 4),
			scrollRect.CenterX(),
			scrollRect.Y + (scrollRect.Height / 4));

		App.Tap("Issue36800DumpButton");
		var result = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue36800Result");
			return element?.GetText() != Sentinel ? element : null;
		}, "The post-gesture native diagnostic callback did not complete");

		var resultText = result.GetText();
		Assert.That(resultText, Does.Not.StartWith("SETUP FAILED:"),
			$"The iOS runtime prerequisites for the native diagnostic were not met: {resultText}");
		Assert.That(resultText, Is.EqualTo(PassResult),
			"Inset-aware native ScrollView range should be non-positive after the post-gesture diagnostic callback");
	}
}
#endif

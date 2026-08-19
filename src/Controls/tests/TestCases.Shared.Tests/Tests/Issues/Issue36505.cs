#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36505 : _IssuesUITest
{
	public Issue36505(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] Span tap hitbox is displaced for wrapped formatted text";

	[Test]
	[Category(UITestCategories.Label)]
	public void TappingVisibleWrappedSpanInvokesRecognizer()
	{
		const string expectedText = "Read these opening words carefully because they wrap over several full lines before the interactive link. " +
			"TAP LINK NOW Then continue reading this ordinary trailing text after the link for several more wrapped lines.";

		App.SetOrientationPortrait();

		var affectedLabel = App.WaitForElement("Issue36505AffectedLabel");
		Assert.That(affectedLabel.GetText(), Is.EqualTo(expectedText),
			"The intended highlighted Span must be present at the expected location in the formatted text.");

		var labelRect = affectedLabel.GetRect();
		Assert.That(labelRect.Width, Is.EqualTo(280).Within(1),
			"The formatted Label must retain the issue's 280-point width constraint.");
		Assert.That(labelRect.Height, Is.GreaterThan(200),
			"The issue requires enough wrapped lines to place the highlighted Span at the Label center.");

		var initialTapCount = App.WaitForElement("Issue36505Result").GetText();
		Assert.That(initialTapCount, Is.EqualTo("0"), "The tap count must start at its sentinel value.");

		App.TapCoordinates(labelRect.CenterX(), labelRect.CenterY());

		Assert.That(
			() => App.FindElement("Issue36505Result").GetText(),
			Is.EqualTo("1").After(3).Seconds.PollEvery(100).MilliSeconds,
			"Visible Span tap missed: observed tap count 0; expected 1.");

		var finalTapCount = App.FindElement("Issue36505Result").GetText();
		Assert.That(finalTapCount, Is.EqualTo("1"), "The visible Span must invoke its recognizer exactly once.");
	}
}
#endif

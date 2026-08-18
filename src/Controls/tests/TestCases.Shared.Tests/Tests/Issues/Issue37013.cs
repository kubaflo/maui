using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37013 : _IssuesUITest
{
	const string LeadingText = "Paragraph one contains ordinary text that wraps naturally across several lines in this constrained label.\n\nParagraph two adds more words before the interactive span so layout differences can accumulate vertically.\n\nParagraph three continues the platform-default formatted text with no explicit font or line-height styling.\n\nParagraph four provides another naturally wrapped block before the link that must be tapped on its visible glyphs.\n\nParagraph five keeps the same Label and FormattedString hierarchy while increasing the rendered line count.\n\nParagraph six supplies additional wrapping content needed to expose a progressively displaced late-span hit region.\n\nParagraph seven appears near the bottom of the ordinary text that precedes the interactive span.\n\nParagraph eight completes more than twenty rendered lines before the visible link.\n";
	const string LinkText = "Click here for details";
	const string TrailingText = "\nParagraph one contains ordinary text that wraps naturally across several lines in this constrained label.\n\nParagraph two adds more words before the interactive span so layout differences can accumulate vertically.\n\nParagraph three continues the platform-default formatted text with no explicit font or line-height styling.\n\nParagraph four provides another naturally wrapped block before the link that must be tapped on its visible glyphs.\n\nParagraph five keeps the same Label and FormattedString hierarchy while increasing the rendered line count.\n\nParagraph six supplies additional wrapping content needed to expose a progressively displaced late-span hit region.\n\nParagraph seven appears near the bottom of the ordinary text that precedes the interactive span.\n\nParagraph eight completes more than twenty rendered lines before the visible link.";

	public override string Issue => "FormattedString span tap target is vertically offset on iOS";

	public Issue37013(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Label)]
	public void TappingVisibleLateSpanInvokesGestureRecognizer()
	{
		var appiumApp = (AppiumApp)App;
		var platformVersion = appiumApp.Driver.Capabilities.GetCapability("platformVersion")?.ToString()
			?? throw new InvalidOperationException("platformVersion capability is missing.");
		Assert.That(Version.Parse(platformVersion), Is.GreaterThanOrEqualTo(new Version(16, 0)),
			"Issue 37013 requires iOS 16 or newer.");

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation().ToString(), Is.EqualTo("Portrait"));

		var affectedLabel = App.WaitForElement("AffectedFormattedLabel");
		var affectedBounds = affectedLabel.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(affectedBounds.Width, Is.EqualTo(300).Within(2));
			Assert.That(affectedBounds.Height, Is.GreaterThan(0));
			Assert.That(affectedLabel.GetText(), Is.EqualTo(LeadingText + LinkText + TrailingText));
		});

		Assert.That(App.WaitForTextToBePresentInElement("LayoutStateLabel", "x"), Is.True,
			"The affected label did not complete a nonzero layout.");
		Assert.That(App.FindElement("LayoutStateLabel").GetText(), Does.Not.Contain("-1"));
		Assert.That(App.FindElement("TapStateLabel").GetText(), Is.EqualTo("NOT_TAPPED"));

		App.TapCoordinates(
			affectedBounds.X + affectedBounds.Width / 2,
			affectedBounds.Y + affectedBounds.Height / 2);
		App.Tap("CheckResultButton");
		Assert.That(App.WaitForTextToBePresentInElement("CheckStateLabel", "CHECK:COMPLETED"), Is.True,
			"The check callback did not complete.");

		Assert.That(App.FindElement("CheckStateLabel").GetText(), Is.EqualTo("CHECK:COMPLETED"),
			"The check callback did not complete.");
		var visibleSpanState = App.FindElement("TapStateLabel").GetText();
		Assert.That(visibleSpanState, Is.EqualTo("TAPPED"),
			$"Visible span tap state was '{visibleSpanState}'; expected 'TAPPED' after tapping the rendered link text.");
	}
}

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26173 : _IssuesUITest
{
	public Issue26173(TestDevice device) : base(device) { }

	public override string Issue => "Fancy Sample Code Uses Copyrighted Fonts";

	[Test]
	[Category(UITestCategories.Fonts)]
	public void IncludedSampleContentDoesNotContainRestrictedFonts()
	{
		App.SetOrientationPortrait();

		var fluentFontEntry = App.WaitForElement("FluentFontEntry").GetText()
			?? throw new AssertionException("FluentFontEntry did not expose text.");
		var segoeFontEntry = App.WaitForElement("SegoeFontEntry").GetText()
			?? throw new AssertionException("SegoeFontEntry did not expose text.");
		var initialStatus = App.WaitForElement("InspectionStatus").GetText()
			?? throw new AssertionException("InspectionStatus did not expose text.");

		Assert.That(initialStatus, Is.EqualTo("Inspection pending"));
		Assert.That(fluentFontEntry, Is.EqualTo("FluentSystemIcons-Regular.ttf"));
		Assert.That(segoeFontEntry, Is.EqualTo("SegoeUI-Semibold.ttf"));

		App.Tap("InspectFontsButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionStatus", "Inspection complete"),
			Is.True,
			"The post-click inspection callback did not complete.");

		var unexpectedFonts = new[] { fluentFontEntry, segoeFontEntry };
		Assert.That(
			unexpectedFonts,
			Is.Empty,
			$"Included sample content unexpectedly contains font resources: {string.Join(", ", unexpectedFonts)}");
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26173 : _IssuesUITest
{
	public Issue26173(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Generated sample content includes restricted fonts";

	[Test]
	[Category(UITestCategories.Fonts)]
	public void GeneratedSampleContentExcludesRestrictedFonts()
	{
		App.LaunchApp(Issue, true);
		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionSummary", "Inspection not started", TimeSpan.FromSeconds(10)),
			Is.True,
			"The fresh page should not have run its inspection callback");

		bool fluentFontInitiallyPresent = App.FindElements("FluentFontEntry").Count > 0;
		bool segoeFontInitiallyPresent = App.FindElements("SegoeFontEntry").Count > 0;
		App.Tap("InspectButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("InspectionSummary", "Inspection completed", TimeSpan.FromSeconds(10)),
			Is.True,
			"The inspection button callback should complete");

		bool fluentFontPresent = App.FindElements("FluentFontEntry").Count > 0;
		bool segoeFontPresent = App.FindElements("SegoeFontEntry").Count > 0;

		Assert.Multiple(() =>
		{
			Assert.That(
				fluentFontPresent,
				Is.False,
				$"Issue 26173: generated sample content still exposes the restricted font entries. FluentSystemIcons-Regular.ttf presence changed from {fluentFontInitiallyPresent} to {fluentFontPresent}; expected False after inspection.");
			Assert.That(
				segoeFontPresent,
				Is.False,
				$"Issue 26173: generated sample content still exposes the restricted font entries. SegoeUI-Semibold.ttf presence changed from {segoeFontInitiallyPresent} to {segoeFontPresent}; expected False after inspection.");
		});
	}
}
#endif

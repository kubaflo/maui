#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26173 : _IssuesUITest
{
	const string FluentFontFile = "FluentSystemIcons-Regular.ttf";
	const string SegoeFontFile = "SegoeUI-Semibold.ttf";

	public Issue26173(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Sample content distributes restricted fonts";

	[Test]
	[Category(UITestCategories.Fonts)]
	public void GeneratedSampleDoesNotContainRestrictedFonts()
	{
		App.WaitForElement("Issue26173CheckButton");
		App.Tap("Issue26173CheckButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue26173CheckButton", "Inventory check requested"),
			Is.True,
			"The generated font inventory check did not complete.");

		Assert.Multiple(() =>
		{
			Assert.That(
				App.FindElements("Issue26173FluentFontEntry"),
				Is.Empty,
				$"Generated sample font inventory contains restricted font {FluentFontFile}.");
			Assert.That(
				App.FindElements("Issue26173SegoeFontEntry"),
				Is.Empty,
				$"Generated sample font inventory contains restricted font {SegoeFontFile}.");
		});
	}
}
#endif

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
	[Category(UITestCategories.CheckBox)]
	public void GeneratedSampleContentDoesNotContainRestrictedFonts()
	{
		App.SetOrientationPortrait();
		Assert.That(App.WaitForTextToBePresentInElement("OrientationState", "Portrait"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("ThemeState", "Light"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("IncludeSampleContentState", "False"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("CompletionSequence", "-1"), Is.True);

		App.Tap("IncludeSampleContentCheckBox");
		Assert.That(App.WaitForTextToBePresentInElement("IncludeSampleContentState", "True"), Is.True);

		App.Tap("CreateProjectButton");
		Assert.That(App.WaitForTextToBePresentInElement("CompletionSequence", "0"), Is.True);
		Assert.That(App.WaitForElement("GeneratedFontsHeading").GetText(),
			Is.EqualTo("Generated project: Resources/Fonts"));

		var fluentFontPresent = App.WaitForElement("FluentFontLabel").GetText()
			== "FluentSystemIcons-Regular.ttf";
		var segoeFontPresent = App.WaitForElement("SegoeFontLabel").GetText()
			== "SegoeUI-Semibold.ttf";

		Assert.That(fluentFontPresent || segoeFontPresent, Is.False,
			$"Generated sample content unexpectedly contains restricted fonts: " +
			$"FluentSystemIcons-Regular.ttf={fluentFontPresent}, " +
			$"SegoeUI-Semibold.ttf={segoeFontPresent}; expected both False.");
	}
}
#endif

#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15524 : _IssuesUITest
{
	public override string Issue => "Text entry border disappear when changing to/from dark mode";

	public Issue15524(TestDevice device) : base(device) { }

	[Test]
	public async Task TextEntryBorderShouldNotDisappearOnThemeChange()
	{
		try
		{
			App.WaitForElement("entry");
			App.SetDarkTheme();
			await Task.Delay(5000);

			//The test passes if the entry border is visible
			VerifyScreenshot();
		}
		finally
		{
			App.SetLightTheme();
		}
	}
}
#endif

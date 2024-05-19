#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue14793 : _IssuesUITest
	{
		public override string Issue => "[Android] Toolbar's text style for default theme has too low contrast";

		public Issue14793(TestDevice device) : base(device) { }

		[Test]
		public void PageTitleShouldBeWhite()
		{
			App.WaitForElement("label");

			// The test passes if the page title is white
			VerifyScreenshot();
		}
	}
}
#endif

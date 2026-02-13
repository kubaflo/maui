using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32419 : _IssuesUITest
	{
		public Issue32419(TestDevice device) : base(device)
		{
		}

		public override string Issue => "Shell Flyout and Content Do Not Fully Support RightToLeft (RTL)";

		[Test]
		[Category(UITestCategories.Shell)]
		public void ShellFlyoutRespectRTLLayoutWithLockedBehavior()
		{
			// Wait for the shell to load
			App.WaitForElement("ContentStack");

			// Verify the shell content is visible with RTL layout
			// The screenshot will show:
			// 1. Flyout on the RIGHT side (in RTL mode with Locked behavior)
			// 2. Content on the LEFT side
			// 3. Content labels/buttons aligned to the RIGHT
			VerifyScreenshot();
		}
	}
}

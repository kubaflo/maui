using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32717 : _IssuesUITest
	{
		public Issue32717(TestDevice device) : base(device)
		{
		}

		public override string Issue => "LinearGradientBrush disappears after showing a Popup on iOS";

		[Test]
		[Category(UITestCategories.Brush)]
		public void LinearGradientBrushShouldPersistAfterOverlayChanges()
		{
			// Wait for page to load
			App.WaitForElement("ContentGrid");

			// Take screenshot of gradient before showing overlay
			VerifyScreenshot("01-GradientBeforeOverlay");

			// Show the overlay (simulating popup)
			App.Tap("ShowOverlayButton");
			App.WaitForElement("OverlayLabel");

			// Verify gradient is still visible while overlay is shown
			// (The gradient should be behind the overlay)
			VerifyScreenshot("02-GradientWithOverlay");

			// Close the overlay
			App.Tap("CloseButton");
			
			// Wait for overlay to disappear
			App.WaitForNoElement("OverlayLabel");

			// Verify gradient is still visible after closing overlay
			// This is the main test - the gradient should NOT disappear
			VerifyScreenshot("03-GradientAfterOverlay");
		}
	}
}

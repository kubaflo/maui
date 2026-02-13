using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32673 : _IssuesUITest
	{
		public Issue32673(TestDevice device) : base(device)
		{
		}

		public override string Issue => "[Android] FlowDirection not working on EmptyView in CollectionView";

		[Test]
		[Category(UITestCategories.CollectionView)]
		public void EmptyViewShouldRespectFlowDirection()
		{
			// Wait for the page to load
			App.WaitForElement("CollectionViewString");

			// Verify initial state with RightToLeft FlowDirection
			// The EmptyView should be displayed with RTL layout
			App.WaitForElement("StatusLabel");

			// Take screenshot to verify RTL EmptyView is displayed correctly
			VerifyScreenshot("InitialRTL");

			// Toggle to LeftToRight
			App.Tap("ToggleButton");
			App.WaitForElement("StatusLabel");

			// Verify EmptyView updates to LTR
			VerifyScreenshot("AfterToggleToLTR");

			// Toggle back to RightToLeft
			App.Tap("ToggleButton");
			App.WaitForElement("StatusLabel");

			// Verify EmptyView returns to RTL
			VerifyScreenshot("AfterToggleBackToRTL");
		}
	}
}

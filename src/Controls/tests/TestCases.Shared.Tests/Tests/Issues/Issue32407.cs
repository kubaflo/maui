using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32407 : _IssuesUITest
	{
		public Issue32407(TestDevice device) : base(device)
		{
		}

		public override string Issue => "BoxView as item separator not displaying in CollectionView2 after scrolling";

		[Test]
		[Category(UITestCategories.CollectionView)]
		public void BoxViewSeparatorsShouldRemainVisibleDuringScrolling()
		{
			// Wait for the CollectionView to load
			App.WaitForElement("TestCollection");

			// Take initial screenshot showing separators are visible
			VerifyScreenshot("01_InitialLoad");

			// Scroll down to trigger cell reuse
			// The bug would cause BoxView separators to disappear during scrolling
			App.ScrollDown("TestCollection");
			System.Threading.Thread.Sleep(500); // Allow time for cells to be reused

			// Verify separators are still visible after scrolling
			VerifyScreenshot("02_AfterScrollDown");

			// Scroll back up
			App.ScrollUp("TestCollection");
			System.Threading.Thread.Sleep(500);

			// Verify separators are still visible after scrolling back
			VerifyScreenshot("03_AfterScrollUp");
		}
	}
}

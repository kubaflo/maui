using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32578 : _IssuesUITest
	{
		public Issue32578(TestDevice device) : base(device)
		{
		}

		public override string Issue => "Grouped CollectionView doesn't size correctly when ItemSizingStrategy=MeasureFirstItem";

		[Test]
		[Category(UITestCategories.CollectionView)]
		public void GroupedCollectionViewMeasureFirstItemShouldSizeCorrectly()
		{
			// Wait for the CollectionView to be loaded
			App.WaitForElement("TestCollection");

			// Wait a moment for items to render
			Task.Delay(1000).Wait();

			// Verify that group headers and items are visible
			// This test primarily validates through visual verification via screenshot
			// The bug would cause items to be sized incorrectly (too small or overlapping)
			VerifyScreenshot();
		}
	}
}

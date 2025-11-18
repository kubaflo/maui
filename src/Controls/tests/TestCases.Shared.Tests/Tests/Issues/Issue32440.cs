using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue32440 : _IssuesUITest
	{
		public Issue32440(TestDevice device) : base(device)
		{
		}

		public override string Issue => "CheckBox alignment issue in .NET 10 when placed inside Grid with VerticalOptions=Center";

		[Test]
		[Category(UITestCategories.CheckBox)]
		public void CheckBoxShouldBeCenteredInGrid()
		{
			// Wait for the test page to load
			App.WaitForElement("TestGrid");

			// Visual verification via screenshot
			// The CheckBox should be vertically centered in the Grid,
			// similar to how the reference dot is centered in its Grid
			VerifyScreenshot();
		}
	}
}

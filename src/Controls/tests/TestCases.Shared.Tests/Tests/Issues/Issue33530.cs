using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	public override string Issue => "[Android] Border with Rotation and HorizontalOptions.Start/End positioned incorrectly on initial load";

	public Issue33530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Border)]
	public void RotatedBorderWithStartHorizontalOptionsPositionedCorrectly()
	{
		// Wait for the border to load
		App.WaitForElement("RotatedBorder");

		// Get the border's position
		var borderRect = App.WaitForElement("RotatedBorder").GetRect();

		// The border should be positioned at or near the left edge of the screen
		// With HorizontalOptions.Start and -90° rotation, the left edge should be close to 0
		// We allow some tolerance for padding/margins
		Assert.That(borderRect.X, Is.LessThanOrEqualTo(50),
			$"Border X position should be close to the left edge, but was {borderRect.X}");

		// Verify the border is not off-screen (negative X or very large X)
		Assert.That(borderRect.X, Is.GreaterThanOrEqualTo(-10),
			$"Border should not be positioned off-screen to the left, but X was {borderRect.X}");

		// Verify shadow is aligned with border (not decoupled)
		// This is harder to test directly, but we can check the border is visible
		Assert.That(borderRect.Width, Is.GreaterThan(0), "Border should have visible width");
		Assert.That(borderRect.Height, Is.GreaterThan(0), "Border should have visible height");
	}
}

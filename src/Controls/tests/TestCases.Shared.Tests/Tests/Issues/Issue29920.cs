#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29920 : _IssuesUITest
{
	public Issue29920(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Android tap events pass through covering containers";

	[Test]
	[Category(UITestCategories.Layout)]
	public void CoveringContainersPreventTapGestureOnLowerStackLayout()
	{
		App.WaitForElement("LayeredRoot");
		var bottomLayerRect = App.WaitForElement("BottomLayer").GetRect();
		var middleLayerRect = App.WaitForElement("MiddleLayer").GetRect();
		var topLayerRect = App.WaitForElement("TopLayer").GetRect();
		var tapTargetRect = App.WaitForElement("BottomTapTarget").GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(bottomLayerRect.Width, Is.GreaterThan(0));
			Assert.That(bottomLayerRect.Height, Is.GreaterThan(0));
			Assert.That(middleLayerRect, Is.EqualTo(bottomLayerRect));
			Assert.That(topLayerRect, Is.EqualTo(bottomLayerRect));
			Assert.That(tapTargetRect.Width, Is.GreaterThan(0));
			Assert.That(tapTargetRect.Height, Is.GreaterThan(0));
			Assert.That(tapTargetRect.CenterX(), Is.InRange(topLayerRect.X, topLayerRect.X + topLayerRect.Width));
			Assert.That(tapTargetRect.CenterY(), Is.InRange(topLayerRect.Y, topLayerRect.Y + topLayerRect.Height));
		});

		var before = App.WaitForElement("ResultLabel").GetText();
		if (before is null)
			Assert.Fail("The result label did not expose its initial text.");

		Assert.That(before, Is.EqualTo("Not tapped"));

		App.TapCoordinates(tapTargetRect.CenterX(), tapTargetRect.CenterY());

		var after = App.WaitForElement("ResultLabel").GetText();
		if (after is null)
			Assert.Fail("The result label did not expose its post-tap text.");

		const string expected = "Not tapped";
		Assert.That(after, Is.EqualTo(expected),
			$"Covered lower StackLayout received TapGestureRecognizer input: before='{before}', after='{after}', expected='{expected}'.");
	}
}
#endif

#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36816 : _IssuesUITest
{
	public Issue36816(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Clicks pass through ContentView to controls underneath";

	[Test]
	[Category(UITestCategories.Button)]
	public void ContentViewBlocksTapFromCoveredButton()
	{
		App.SetOrientationPortrait();

		var pageRect = App.WaitForElement("Issue36816Page").GetRect();
		Assert.That(pageRect.Width, Is.GreaterThan(0), "The issue page should have a nonzero width.");
		Assert.That(pageRect.Height, Is.GreaterThan(pageRect.Width), "The issue page should be displayed in portrait orientation.");

		var buttonRect = App.WaitForElement("Issue36816CoveredButton").GetRect();
		var overlayRect = App.WaitForElement("Issue36816GreenOverlay").GetRect();
		Assert.That(buttonRect.Width, Is.GreaterThan(0), "The covered button should have a nonzero width.");
		Assert.That(buttonRect.Height, Is.GreaterThan(0), "The covered button should have a nonzero height.");
		Assert.That(overlayRect.Width, Is.GreaterThan(0), "The green overlay should have a nonzero width.");
		Assert.That(overlayRect.Height, Is.GreaterThan(0), "The green overlay should have a nonzero height.");

		var overlayCenterX = overlayRect.X + (overlayRect.Width / 2.0);
		var overlayCenterY = overlayRect.Y + (overlayRect.Height / 2.0);
		Assert.That(overlayCenterX, Is.InRange(buttonRect.X, buttonRect.X + buttonRect.Width), "The overlay center should be over the covered button.");
		Assert.That(overlayCenterY, Is.InRange(buttonRect.Y, buttonRect.Y + buttonRect.Height), "The overlay center should be over the covered button.");

		const string countPrefix = "Underlying button press count: ";
		var initialCountText = App.WaitForElement("Issue36816PressCount").GetText();
		Assert.That(initialCountText, Is.EqualTo($"{countPrefix}0"), "The button press count should be zero before tapping the overlay.");

		App.Tap("Issue36816GreenOverlay");

		var measuredPressCount = -1;
		var countTextAfterTap = App.WaitForElement("Issue36816PressCount").GetText();
		Assert.That(countTextAfterTap, Is.Not.Null, "The post-tap count query should return label text.");
		Assert.That(countTextAfterTap, Does.StartWith(countPrefix), "The post-tap count label should contain the underlying button press count.");
		var countParsed = int.TryParse(countTextAfterTap![countPrefix.Length..], out measuredPressCount);
		Assert.That(countParsed, Is.True, "The post-tap count label should end with a numeric press count.");
		Assert.That(measuredPressCount, Is.Not.EqualTo(-1), "The post-tap count query should replace the sentinel value.");
		Assert.That(measuredPressCount, Is.EqualTo(0), $"Underlying button press count after overlay tap was {measuredPressCount}; expected 0.");
	}
}
#endif

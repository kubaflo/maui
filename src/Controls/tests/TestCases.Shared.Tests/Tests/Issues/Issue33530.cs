#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33530 : _IssuesUITest
{
	const float PositionTolerance = 2;
	const string ExpectedContent = "Rotated Border Content";

	public override string Issue => "[Android] Initially rotated Border with Start alignment is positioned incorrectly";

	public Issue33530(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Border)]
	public void InitiallyRotatedBorderUsesVisualStartEdge()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue33530PageReady");

		App.Tap("Issue33530InitiallyRotatedButton");
		const string completedPrefix = "READY:";
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue33530TargetStatus",
				completedPrefix,
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The initially rotated Border did not complete its first attached layout.");

		var targetContent = App.WaitForElement("Issue33530TargetContent");
		var targetContentText = targetContent.GetText();
		Assert.That(targetContentText, Is.Not.Null);
		Assert.That(targetContentText, Is.EqualTo(ExpectedContent));

		var targetRect = App.WaitForElement("Issue33530TargetBorder").GetRect();
		Assert.That(targetRect.Width, Is.GreaterThan(0), "The initially rotated Border must have a positive width.");
		Assert.That(targetRect.Height, Is.GreaterThan(0), "The initially rotated Border must have a positive height.");

		var statusText = App.FindElement("Issue33530TargetStatus").GetText();
		if (statusText is null)
			throw new AssertionException("The native visual-left measurement was missing.");

		Assert.That(statusText, Does.StartWith(completedPrefix));
		var visualLeftText = statusText[completedPrefix.Length..];
		Assert.That(
			float.TryParse(visualLeftText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var visualLeft),
			Is.True,
			$"The native visual-left measurement was invalid: '{visualLeftText}'.");

		var leftGap = Math.Abs(visualLeft);
		Assert.That(
			leftGap,
			Is.LessThanOrEqualTo(PositionTolerance),
			$"Initially rotated Border visual left edge did not touch the modal content left edge. " +
			$"Expected X=0 within {PositionTolerance}px, actual X={visualLeft}, gap={leftGap}px.");
	}
}
#endif

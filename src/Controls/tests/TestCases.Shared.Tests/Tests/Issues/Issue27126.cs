#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27126 : _IssuesUITest
{
	public Issue27126(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Line control prevents tapping other controls on iOS";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void LineDoesNotPreventTappingEarlierControl()
	{
		const string countPrefix = "Target tap count: ";
		const string failureSignature = "Issue27126 expected the earlier Label tap recognizer to report count 1 after tapping the later Line frame";

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(OpenQA.Selenium.ScreenOrientation.Portrait));

		var targetElement = App.WaitForElement("TapTarget");
		var countElement = App.WaitForElement("TapCount");
		var lineElement = App.WaitForElement("IssueLine");

		var initialCountText = countElement.GetText();
		if (initialCountText is null)
		{
			Assert.Fail("Issue27126 could not read the initial tap count");
		}
		else
		{
			Assert.That(initialCountText, Is.EqualTo($"{countPrefix}0"));
		}

		var targetRect = targetElement.GetRect();
		var lineRect = lineElement.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(lineRect.Width, Is.GreaterThan(0), "The Line must be measured after X2 is updated");
			Assert.That(lineRect.CenterX(), Is.InRange(targetRect.X, targetRect.X + targetRect.Width));
			Assert.That(lineRect.CenterY(), Is.InRange(targetRect.Y, targetRect.Y + targetRect.Height));
		});

		App.TapCoordinates(lineRect.CenterX(), lineRect.CenterY());

		bool tapCountChanged = App.WaitForTextToBePresentInElement(
			"TapCount",
			$"{countPrefix}1",
			timeout: TimeSpan.FromSeconds(3));

		int observedCount = -1;
		var observedCountElement = App.WaitForElement("TapCount");
		var observedCountText = observedCountElement.GetText();
		if (observedCountText is null)
		{
			Assert.Fail("Issue27126 could not read the observed tap count");
		}
		else if (observedCountText.StartsWith(countPrefix, StringComparison.Ordinal) &&
			int.TryParse(observedCountText[countPrefix.Length..], out int parsedCount))
		{
			observedCount = parsedCount;
		}

		Assert.That(tapCountChanged, Is.True,
			$"{failureSignature}; observed {observedCount}, expected 1");
		Assert.That(observedCount, Is.EqualTo(1));
	}
}
#endif

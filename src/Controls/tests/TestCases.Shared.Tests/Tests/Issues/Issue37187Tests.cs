#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37187Tests : _IssuesUITest
{
	public Issue37187Tests(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Replacing Shell.FlyoutFooter leaves the previous footer active";

	[Test]
	[Category(UITestCategories.Shell)]
	public void InvalidatingReplacedFlyoutFooterDoesNotMeasureCurrentFooter()
	{
		App.WaitForElement("ReplaceFooterButton");
		App.TapShellFlyoutIcon();
		App.WaitForElement("CurrentFlyoutItem");
		App.Tap("CurrentFlyoutItem");
		App.WaitForElement("ReplaceFooterButton");

		App.Tap("ReplaceFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("SetupStatus", "Footer B ready"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("FooterIdentityStatus", "Current footer: Footer B"),
			Is.True);

		var baseline = ReadCount("BaselineMeasurementCount");
		Assert.That(baseline, Is.EqualTo(0), "Footer B must settle at a zero measurement baseline.");

		App.Tap("InvalidateOldFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("CompletionGenerationStatus", "0"),
			Is.True);

		var completionGeneration = ReadCount("CompletionGenerationStatus");
		Assert.That(completionGeneration, Is.GreaterThanOrEqualTo(0), "The post-invalidation observation must complete.");

		var before = ReadCount("BeforeMeasurementCount");
		var after = ReadCount("AfterMeasurementCount");
		var measurementsCausedByInvalidation = after - before;
		Assert.That(
			measurementsCausedByInvalidation,
			Is.EqualTo(0),
			"Invalidating removed footer A must not measure current footer B.");
	}

	int ReadCount(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Unable to find {automationId}.");

		var text = element.GetText();
		if (!int.TryParse(text, out int value))
			throw new AssertionException($"Expected {automationId} to contain an integer, but found '{text}'.");

		return value;
	}
}
#endif

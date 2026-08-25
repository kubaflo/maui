#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37187 : _IssuesUITest
{
	public Issue37187(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Replacing Shell.FlyoutFooter leaves the previous footer active";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RemovedFlyoutFooterDoesNotMeasureCurrentFooter()
	{
		App.WaitForElement("MainPageContent");

		App.TapShellFlyoutIcon();
		App.WaitForElement("Main Page");
		App.SwipeRightToLeft("Main Page");
		Assert.That(
			App.WaitForTextToBePresentInElement("TransitionStatus", "Opened=True;Closed=True"),
			Is.True,
			"The real Shell flyout must open and close before replacing its footer.");

		App.Tap("PrepareFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ReplacementStatus",
				"Ready=True;A=Footer A;B=Footer B;Current=Footer B"),
			Is.True,
			"Footer A must be replaced by footer B through Shell.FlyoutFooter.");

		var baseline = ReadCount("FooterBMeasureCount");
		Assert.That(baseline, Is.Zero, "Footer B must settle at the arranged zero measure baseline.");

		App.Tap("InvalidateOldFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("CompletionStatus", "Completed=True"),
			Is.True,
			"The post-invalidation measurement observation must complete.");

		var before = ReadCount("BeforeMeasureCount");
		var after = ReadCount("AfterMeasureCount");
		Assert.That(before, Is.EqualTo(baseline), "The trigger must begin from the captured footer B baseline.");
		Assert.That(
			after,
			Is.EqualTo(before),
			$"Removed footer A invalidation measured current footer B: before={before}, after={after}");
	}

	int ReadCount(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' must expose its count.");

		return int.Parse(text, CultureInfo.InvariantCulture);
	}
}
#endif

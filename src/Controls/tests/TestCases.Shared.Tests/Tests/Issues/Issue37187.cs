using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37187 : _IssuesUITest
{
	public override string Issue => "Replacing Shell FlyoutFooter leaves the previous footer active";

	public Issue37187(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Shell)]
	public void RemovedFlyoutFooterInvalidationDoesNotMeasureActiveFooter()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("ReplaceFooterButton");

		App.TapShellFlyoutIcon();
		var footerARect = App.WaitForElement("FooterA").GetRect();
		CloseFlyout(footerARect.X, footerARect.Y, footerARect.Width, footerARect.Height);
		App.WaitForElement("ReplaceFooterButton");

		App.Tap("ReplaceFooterButton");
		App.TapShellFlyoutIcon();
		var footerBRect = App.WaitForElement("FooterB").GetRect();
		App.WaitForNoElement("FooterA");
		CloseFlyout(footerBRect.X, footerBRect.Y, footerBRect.Width, footerBRect.Height);
		App.WaitForElement("InvalidateOldFooterButton");

		App.Tap("InvalidateOldFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("CompletionStatus", "True", TimeSpan.FromSeconds(5)),
			Is.True,
			"The post-trigger completion callback did not run.");

		var beforeMeasureCount = int.Parse(
			App.FindElement("BeforeMeasureCount").GetText()
				?? throw new InvalidOperationException("Before measure count was not available."));
		var afterMeasureCount = int.Parse(
			App.FindElement("AfterMeasureCount").GetText()
				?? throw new InvalidOperationException("After measure count was not available."));
		Assert.That(afterMeasureCount, Is.Not.EqualTo(-1), "The post-trigger measure count was not captured.");
		Assert.That(
			afterMeasureCount,
			Is.EqualTo(beforeMeasureCount),
			$"Removed Footer A invalidation must not measure Footer B. Before: {beforeMeasureCount}; After: {afterMeasureCount}.");
	}

	void CloseFlyout(float x, float y, float width, float height)
	{
		var centerY = y + height / 2;
		App.DragCoordinates(
			x + width - 10,
			centerY,
			Math.Max(x + 10, 1),
			centerY);
	}
}

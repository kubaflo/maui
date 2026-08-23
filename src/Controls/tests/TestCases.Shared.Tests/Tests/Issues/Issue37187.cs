#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37187 : _IssuesUITest
{
	public override string Issue => "Replacing Shell.FlyoutFooter leaves the previous footer active";

	public Issue37187(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void RemovedFooterInvalidationDoesNotMeasureCurrentFooter()
	{
		App.WaitForElement("ReplaceFooterButton");

		App.Tap("OK");
		Assert.That(App.WaitForElement("FooterA").GetText(), Is.EqualTo("Footer A"));
		App.Tap("Footer Callback");
		App.WaitForElement("ReplaceFooterButton");

		App.Tap("ReplaceFooterButton");
		Assert.That(App.WaitForElement("FooterIdentity").GetText(), Is.EqualTo("FooterB"));

		App.Tap("OK");
		Assert.That(App.WaitForElement("FooterB").GetText(), Is.EqualTo("Footer B"));
		App.Tap("Footer Callback");
		App.WaitForElement("InvalidateOldFooterButton");
		App.WaitForTextToBePresentInElement("FooterStatus", "Footer B ready");

		var beforeText = App.FindElement("FooterBMeasureCount").GetText();
		Assert.That(int.TryParse(beforeText, out var before), Is.True,
			$"Expected a numeric pre-trigger footer B measure count, but found '{beforeText}'.");

		App.Tap("InvalidateOldFooterButton");
		App.WaitForTextToBePresentInElement("TriggerCompletion", "Completed-1");

		Assert.That(App.FindElement("TriggerSequence").GetText(), Is.EqualTo("1"));
		Assert.That(App.FindElement("BeforeMeasureCount").GetText(), Is.EqualTo(beforeText));

		var afterText = App.FindElement("AfterMeasureCount").GetText();
		Assert.That(int.TryParse(afterText, out var after), Is.True,
			$"Expected a numeric post-trigger footer B measure count, but found '{afterText}'.");
		Assert.That(after, Is.EqualTo(before),
			$"Removed footer A invalidation unexpectedly measured current footer B (before: {before}, after: {after})");
	}
}
#endif

#if IOS
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

	public override string Issue => "Replacing Shell FlyoutFooter leaves the previous footer active";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RemovedFlyoutFooterDoesNotMeasureCurrentFooter()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("FooterIdentity", "FooterA"),
			Is.True,
			"Footer A was not installed.");
		App.WaitForElement("OK");
		App.Tap("OK");
		App.WaitForElement("FooterA");
		App.WaitForElement("Home");
		App.Tap("Home");
		App.WaitForElement("ReplaceFooterButton");

		App.Tap("ReplaceFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("FooterIdentity", "FooterB"),
			Is.True,
			"Shell.FlyoutFooter did not reference footer B.");
		Assert.That(
			App.WaitForTextToBePresentInElement("FooterBBaseline", "0"),
			Is.True,
			"Footer B did not reach the arranged zero measure baseline.");

		App.Tap("InvalidateOldFooterButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("TriggerCompletion", "Completion:1"),
			Is.True,
			"The post-trigger callback did not complete.");

		var beforeElement = App.FindElement("FooterBMeasureBefore");
		var afterElement = App.FindElement("FooterBMeasureAfter");
		if (beforeElement is null)
			throw new AssertionException("The footer B before-measure result was not available.");
		if (afterElement is null)
			throw new AssertionException("The footer B after-measure result was not available.");

		var before = beforeElement.GetText();
		var after = afterElement.GetText();
		if (before is null)
			throw new AssertionException("The footer B before-measure result text was not available.");
		if (after is null)
			throw new AssertionException("The footer B after-measure result text was not available.");

		Assert.That(before, Is.EqualTo("0"), $"Footer B was not at the arranged baseline: before={before}, after={after}.");
		Assert.That(after, Is.EqualTo("0"), $"Removed footer A invalidation unexpectedly measured footer B: before={before}, after={after}.");
	}
}
#endif

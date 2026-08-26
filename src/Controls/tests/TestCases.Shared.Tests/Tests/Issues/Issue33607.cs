#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	public Issue33607(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void ILayoutCollectionChangeAfterClosingWindowDoesNotUseDisposedServices()
	{
		Assert.That(ReadCount("Issue33607TemplateReadyCount", "Template-ready count: "), Is.Zero);
		Assert.That(ReadCount("Issue33607CloseReturnCount", "Close-return count: "), Is.Zero);
		Assert.That(ReadCount("Issue33607SuccessfulUpdateCount", "Successful-update count: "), Is.Zero);

		for (var cycle = 1; cycle <= 5; cycle++)
		{
			var templateReadyObserved = -1;
			var closeReturnObserved = -1;
			var successfulUpdateObserved = -1;

			App.Tap("Issue33607CycleButton");

			WaitForCount("Template-ready count: ", cycle);
			templateReadyObserved = ReadCount("Issue33607TemplateReadyCount", "Template-ready count: ");
			Assert.That(templateReadyObserved, Is.EqualTo(cycle),
				$"Cycle {cycle} did not load the expected bound template: expected template-ready count {cycle}, observed {templateReadyObserved}.");

			WaitForCount("Close-return count: ", cycle);
			closeReturnObserved = ReadCount("Issue33607CloseReturnCount", "Close-return count: ");
			Assert.That(closeReturnObserved, Is.EqualTo(cycle),
				$"Cycle {cycle} did not return from CloseWindow: expected close-return count {cycle}, observed {closeReturnObserved}.");

			successfulUpdateObserved = ReadCount("Issue33607SuccessfulUpdateCount", "Successful-update count: ");
			Assert.That(successfulUpdateObserved, Is.EqualTo(cycle),
				$"Issue33607 post-close BindableLayout update did not complete for cycle {cycle}: expected successful-update count {cycle}, observed {successfulUpdateObserved}.");
		}
	}

	void WaitForCount(string prefix, int expected)
	{
		var element = App.WaitForElement(
			$"{prefix}{expected}",
			timeoutMessage: $"Timed out waiting for {prefix}{expected}.",
			timeout: TimeSpan.FromSeconds(15));
		if (element is null)
			throw new AssertionException($"Could not find count text '{prefix}{expected}'.");
	}

	int ReadCount(string automationId, string prefix)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Could not find '{automationId}'.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"'{automationId}' did not expose text.");
		if (!text.StartsWith(prefix, StringComparison.Ordinal) ||
			!int.TryParse(text[prefix.Length..], out var count))
		{
			throw new AssertionException($"'{automationId}' exposed unexpected text '{text}'.");
		}

		return count;
	}
}
#endif

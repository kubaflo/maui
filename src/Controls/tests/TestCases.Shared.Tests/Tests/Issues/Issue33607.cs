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
	public void ILayoutCollectionMutationAfterClosingWindowDoesNotUseDisposedServices()
	{
		var createdText = App.WaitForElement("CreatedCycleLabel").GetText();
		var destroyingText = App.WaitForElement("DestroyingCycleLabel").GetText();
		var completedText = App.WaitForElement("CompletedCycleLabel").GetText();

		Assert.That(createdText, Is.EqualTo("-1"), "Created cycle must start at the sentinel before opening a secondary window.");
		Assert.That(destroyingText, Is.EqualTo("-1"), "Destroying cycle must start at the sentinel before opening a secondary window.");
		Assert.That(completedText, Is.EqualTo("-1"), "Completed cycle must start at the sentinel before opening a secondary window.");

		for (var cycle = 1; cycle <= 3; cycle++)
		{
			App.Tap("RunCycleButton");

			App.RetryAssert(
				() => Assert.That(App.WaitForElement("CreatedCycleLabel").GetText(), Is.EqualTo(cycle.ToString()),
					$"Window.Created was not observed for cycle {cycle}."),
				timeout: TimeSpan.FromSeconds(10));

			App.RetryAssert(
				() => Assert.That(App.WaitForElement("DestroyingCycleLabel").GetText(), Is.EqualTo(cycle.ToString()),
					$"Window.Destroying was not observed for cycle {cycle}."),
				timeout: TimeSpan.FromSeconds(10));

			App.RetryAssert(
				() => Assert.That(App.WaitForElement("CompletedCycleLabel").GetText(), Is.EqualTo(cycle.ToString()),
					$"The post-close collection mutation did not complete for cycle {cycle}."),
				timeout: TimeSpan.FromSeconds(10));

			var result = App.WaitForElement($"Cycle{cycle}ResultLabel").GetText();
			Assert.That(result, Is.Not.Null, $"Cycle {cycle} must expose a concrete mutation result.");
			Assert.That(result, Is.EqualTo("Completed"),
				$"Issue33607 cycle {cycle} post-close ILayout collection mutation threw {result ?? "<null>"}");
		}
	}
}
#endif

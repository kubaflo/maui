#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "ObjectDisposedException after closing a window while applying an ILayout collection change";

	public Issue33607(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void ApplyingCollectionChangeToRetainedILayoutAfterWindowCloseDoesNotUseDisposedHandlers()
	{
		App.WaitForElement("RunCycleButton");
		Assert.That(ReadCount("CompletedCyclesStatus", "Completed cycles: "), Is.EqualTo(0));
		Assert.That(ReadCount("LoadedPagesStatus", "Loaded pages: "), Is.EqualTo(0));
		Assert.That(ReadCount("ClosedWindowsStatus", "Closed windows: "), Is.EqualTo(0));
		Assert.That(ReadCount("MutationCallbacksStatus", "Mutation callbacks: "), Is.EqualTo(0));
		Assert.That(ReadCount("ExceptionCountStatus", "Post-close exceptions: "), Is.EqualTo(-1));

		for (var cycle = 1; cycle <= 3; cycle++)
		{
			var completedBefore = ReadCount("CompletedCyclesStatus", "Completed cycles: ");
			var loadedBefore = ReadCount("LoadedPagesStatus", "Loaded pages: ");
			var closedBefore = ReadCount("ClosedWindowsStatus", "Closed windows: ");
			var mutationsBefore = ReadCount("MutationCallbacksStatus", "Mutation callbacks: ");

			App.Tap("RunCycleButton");

			Assert.That(
				App.WaitForTextToBePresentInElement("LoadedPagesStatus", $"Loaded pages: {loadedBefore + 1}", TimeSpan.FromSeconds(10)),
				Is.True,
				$"Secondary page did not load for cycle {cycle}.");
			Assert.That(
				App.WaitForTextToBePresentInElement("ClosedWindowsStatus", $"Closed windows: {closedBefore + 1}", TimeSpan.FromSeconds(10)),
				Is.True,
				$"Secondary window did not close for cycle {cycle}.");
			Assert.That(
				App.WaitForTextToBePresentInElement("MutationCallbacksStatus", $"Mutation callbacks: {mutationsBefore + 1}", TimeSpan.FromSeconds(10)),
				Is.True,
				$"Retained collection mutation callback did not run for cycle {cycle}.");
			Assert.That(
				App.WaitForTextToBePresentInElement("ItemCountStatus", "Item count: 2", TimeSpan.FromSeconds(10)),
				Is.True,
				$"Retained collection did not contain both expected items for cycle {cycle}.");
			Assert.That(
				App.WaitForTextToBePresentInElement("CompletedCyclesStatus", $"Completed cycles: {completedBefore + 1}", TimeSpan.FromSeconds(10)),
				Is.True,
				$"Window lifecycle did not complete for cycle {cycle}.");
		}

		var completedCycles = ReadCount("CompletedCyclesStatus", "Completed cycles: ");
		var exceptionCount = ReadCount("ExceptionCountStatus", "Post-close exceptions: ");
		Assert.That(completedCycles, Is.EqualTo(3));
		Assert.That(
			exceptionCount,
			Is.EqualTo(0),
			$"Post-close ILayout Apply update threw ObjectDisposedException: expected 0 exceptions after 3 completed cycles; observed {exceptionCount}.");
	}

	int ReadCount(string automationId, string prefix)
	{
		var element = App.FindElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		Assert.That(text, Does.StartWith(prefix), $"Unexpected text for '{automationId}'.");
		return int.Parse(text[prefix.Length..], System.Globalization.CultureInfo.InvariantCulture);
	}
}
#endif

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
	public void BindableLayoutUpdateAfterClosingWindowDoesNotUseDisposedServices()
	{
		var initialCycleElement = App.WaitForElement("Issue33607CycleStatus");
		if (initialCycleElement is null)
			throw new AssertionException("The cycle status element was not found.");

		var initialExceptionElement = App.WaitForElement("Issue33607ExceptionStatus");
		if (initialExceptionElement is null)
			throw new AssertionException("The exception status element was not found.");

		var initialApiElement = App.WaitForElement("Issue33607ApiStatus");
		if (initialApiElement is null)
			throw new AssertionException("The API status element was not found.");

		if (!initialCycleElement.TryGetText(out var initialCycle))
			throw new AssertionException("The initial cycle status text was not available.");

		if (!initialExceptionElement.TryGetText(out var initialException))
			throw new AssertionException("The initial exception status text was not available.");

		if (!initialApiElement.TryGetText(out var initialApiStatus))
			throw new AssertionException("The initial API status text was not available.");

		Assert.That(initialCycle, Is.EqualTo("Cycle=-1"));
		Assert.That(initialException, Is.EqualTo("NotRun"));
		Assert.That(initialApiStatus, Is.EqualTo("ILayout.Apply=NotRun"));

		var observedException = "NotRun";
		for (var cycle = 1; cycle <= 3; cycle++)
		{
			App.Tap("Issue33607RunCycleButton");
			Assert.That(
				App.WaitForTextToBePresentInElement(
					"Issue33607CycleStatus",
					$"Cycle={cycle}",
					timeout: TimeSpan.FromSeconds(10)),
				Is.True,
				$"Post-close callback for cycle {cycle} did not complete.");

			var apiStatusElement = App.WaitForElement("Issue33607ApiStatus");
			if (apiStatusElement is null)
				throw new AssertionException($"The API status element was not found after cycle {cycle}.");

			if (!apiStatusElement.TryGetText(out var apiStatus))
				throw new AssertionException($"The API status text was not available after cycle {cycle}.");

			Assert.That(
				apiStatus,
				Does.StartWith($"ILayout.Apply=Insert;Cycle={cycle};Children="),
				$"NotifyCollectionChangedEventArgsExtensions.Apply did not process the ILayout insertion for cycle {cycle}.");

			var exceptionElement = App.WaitForElement("Issue33607ExceptionStatus");
			if (exceptionElement is null)
				throw new AssertionException($"The exception status element was not found after cycle {cycle}.");

			if (!exceptionElement.TryGetText(out var cycleException))
				throw new AssertionException($"The exception status text was not available after cycle {cycle}.");

			if (observedException == "NotRun" || cycleException != "None")
				observedException = cycleException;
		}

		Assert.That(
			observedException,
			Is.EqualTo("None"),
			$"Post-close BindableLayout insertion failed: observed exception={observedException}, expected exception=None.");
	}
}
#endif

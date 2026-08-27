#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public Issue33607(TestDevice device)
		: base(device)
	{ }

	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	[Test]
	[Category(UITestCategories.Window)]
	public void ApplyingCollectionChangeToILayoutAfterClosingWindowDoesNotUseDisposedServices()
	{
		var initialStatus = App.WaitForElement("AttemptStatus");
		if (initialStatus is null)
			throw new InvalidOperationException("The initial attempt status was not found.");

		Assert.That(initialStatus.GetText(), Is.EqualTo("Ready for attempt 1"));

		for (var attempt = 1; attempt <= 3; attempt++)
		{
			var priorStatus = App.FindElement("AttemptStatus");
			if (priorStatus is null)
				throw new InvalidOperationException($"The status before attempt {attempt} was not found.");

			var priorToken = priorStatus.GetText();

			App.Tap("RunAttemptButton");

			var expectedToken = $"Attempt {attempt} complete";
			Assert.That(
				App.WaitForTextToBePresentInElement("AttemptStatus", expectedToken, TimeSpan.FromSeconds(20)),
				Is.True,
				$"Attempt {attempt} did not complete after the secondary window closed.");

			var completedStatus = App.FindElement("AttemptStatus");
			if (completedStatus is null)
				throw new InvalidOperationException($"The status after attempt {attempt} was not found.");

			var completedToken = completedStatus.GetText();
			Assert.That(completedToken, Is.EqualTo(expectedToken));
			Assert.That(completedToken, Is.Not.EqualTo(priorToken));
		}

		var mutationCount = App.FindElement("MutationCount");
		var attemptResults = App.FindElement("AttemptResults");
		if (mutationCount is null || attemptResults is null)
			throw new InvalidOperationException("The post-close mutation results were not found.");

		var mutationCountText = mutationCount.GetText();
		var attemptResultsText = attemptResults.GetText();
		if (mutationCountText is null || attemptResultsText is null)
			throw new InvalidOperationException("The post-close mutation result text was not available.");

		var allMutationsSucceeded =
			mutationCountText == "Successful mutations: 3" &&
			!attemptResultsText.Contains(nameof(ObjectDisposedException), StringComparison.Ordinal);

		Assert.That(
			allMutationsSucceeded,
			Is.True,
			$"Post-close ILayout collection application threw ObjectDisposedException. {mutationCountText}. {attemptResultsText}");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public Issue33607(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	[Test]
	[Category(UITestCategories.Window)]
	public void ApplyingCollectionChangeToILayoutAfterWindowClosesDoesNotUseDisposedServices()
	{
		var initialResult = App.WaitForElement("MutationResultLabel");
		if (initialResult is null)
			throw new AssertionException("The mutation result label was not found.");

		var initialText = initialResult.GetText();
		if (initialText is null)
			throw new AssertionException("The initial mutation result was unavailable.");

		Assert.That(initialText, Is.EqualTo("Cycle 0: not run"));

		var cycle1Result = RunWindowCloseAndMutationCycle(1);
		var cycle2Result = RunWindowCloseAndMutationCycle(2);
		var cycle3Result = RunWindowCloseAndMutationCycle(3);

		Assert.That(cycle1Result, Is.EqualTo("Cycle 1: no exception"),
			"Post-close ILayout insertion from NotifyCollectionChangedEventArgsExtensions.Apply cycle 1 threw ObjectDisposedException");
		Assert.That(cycle2Result, Is.EqualTo("Cycle 2: no exception"),
			"Post-close ILayout insertion from NotifyCollectionChangedEventArgsExtensions.Apply cycle 2 threw ObjectDisposedException");
		Assert.That(cycle3Result, Is.EqualTo("Cycle 3: no exception"),
			"Post-close ILayout insertion from NotifyCollectionChangedEventArgsExtensions.Apply cycle 3 threw ObjectDisposedException");
	}

	string RunWindowCloseAndMutationCycle(int cycle)
	{
		var openButton = App.WaitForElement("OpenWindowButton");
		if (openButton is null)
			throw new AssertionException($"The open-window button was unavailable for cycle {cycle}.");

		App.Tap("OpenWindowButton");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ClosureStatusLabel",
				$"Destroyed windows: {cycle}",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			$"The secondary window Destroying callback did not complete for cycle {cycle}.");

		var updateButton = App.WaitForElement("UpdateItemsButton");
		if (updateButton is null)
			throw new AssertionException($"The update button was unavailable for cycle {cycle}.");

		App.Tap("UpdateItemsButton");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"MutationResultLabel",
				$"Cycle {cycle}:",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			$"The post-close mutation did not complete for cycle {cycle}.");

		var resultElement = App.FindElement("MutationResultLabel");
		if (resultElement is null)
			throw new AssertionException($"The mutation result label was unavailable for cycle {cycle}.");

		var resultText = resultElement.GetText();
		if (resultText is null)
			throw new AssertionException($"The mutation result text was unavailable for cycle {cycle}.");

		return resultText;
	}
}
#endif

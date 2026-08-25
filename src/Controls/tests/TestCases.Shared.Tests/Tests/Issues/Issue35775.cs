#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35775 : _IssuesUITest
{
	public Issue35775(TestDevice device) : base(device)
	{
	}

	public override string Issue => "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection";

	[Test]
	[Category(UITestCategories.IndicatorView)]
	public void PoppedControlsUsingSharedObservableCollectionAreCollected()
	{
		AssertStateContains("Completed pops: 0");
		AssertStateContains("Collection generation: -1");

		for (var visit = 1; visit <= 3; visit++)
		{
			App.WaitForElement("Issue35775OpenButton");
			App.Tap("Issue35775OpenButton");
			App.WaitForElement("Feed item 1");
			App.WaitForElement("Issue35775Indicator");
			App.Tap("Issue35775PopButton");
			var popCompleted = App.WaitForTextToBePresentInElement(
				"Issue35775State",
				$"Completed pops: {visit}",
				timeout: TimeSpan.FromSeconds(20));
			Assert.That(popCompleted, Is.True, $"Visit {visit} did not complete its pop transition.");
		}

		AssertStateContains("Completed pops: 3");
		AssertStateContains("Tracked controls: 4");
		AssertStateContains("Tracked payloads: 4");

		App.Tap("Issue35775CheckButton");
		var collectionCompleted = App.WaitForTextToBePresentInElement(
			"Issue35775State",
			"Collection generation: 1",
			timeout: TimeSpan.FromSeconds(30));
		Assert.That(collectionCompleted, Is.True, "The post-GC collection generation did not complete.");

		var state = App.WaitForElement("Issue35775State").GetText();
		if (state is null)
		{
			Assert.Fail("The collection state did not expose text.");
			return;
		}

		var aliveControls = ReadCount(state, "Alive controls: ");
		var alivePayloads = ReadCount(state, "Alive payloads: ");
		Assert.That(
			aliveControls == 0 && alivePayloads == 0,
			Is.True,
			$"Popped IndicatorView/CarouselView instances should be collectible after three shared-feed navigation visits. Observed controls: {aliveControls}/4 (expected 0/4); observed payloads: {alivePayloads}/4 (expected 0/4).");
	}

	void AssertStateContains(string expected)
	{
		var text = App.WaitForElement("Issue35775State").GetText();
		if (text is null)
		{
			Assert.Fail("The collection state did not expose text.");
			return;
		}

		Assert.That(text, Does.Contain(expected));
	}

	static int ReadCount(string state, string prefix)
	{
		var line = state.Split('\n').Single(value => value.StartsWith(prefix, StringComparison.Ordinal));
		var text = line[prefix.Length..].Trim();
		Assert.That(int.TryParse(text, out var count), Is.True, $"State line '{line}' contained a non-numeric count.");
		return count;
	}
}
#endif

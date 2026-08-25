#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Incremental loading jumps back to the top of the CollectionView";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalLoadingPreservesVisiblePosition()
	{
		App.WaitForElement("Issue29120CollectionView");
		App.WaitForElement("Issue29120Animal1");

		var initialPositionText = App.WaitForElement("Issue29120PositionLabel").GetText();
		var initialResultText = App.WaitForElement("Issue29120ResultLabel").GetText();
		Assert.That(initialPositionText, Is.Not.Null);
		Assert.That(initialResultText, Is.Not.Null);
		Assert.That(initialPositionText, Is.EqualTo("First visible item: 0"));
		Assert.That(initialResultText, Is.EqualTo("Count=10; Pre=-1; Post=-1; Last=Animal 10@Location 10"));

		for (int gesture = 0; gesture < 6; gesture++)
		{
			App.ScrollDown("Issue29120CollectionView", ScrollStrategy.Gesture, 0.67, 500);
			var resultText = App.WaitForElement("Issue29120ResultLabel").GetText();
			if (resultText is null)
				throw new AssertionException("The incremental loading result label did not expose text.");

			if (resultText.Contains("Count=30", StringComparison.Ordinal))
				break;
		}

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue29120ResultLabel",
				"Count=30",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"Incremental loading did not append the target batch and produce a post-append Scrolled callback.");

		var finalResultText = App.WaitForElement("Issue29120ResultLabel").GetText();
		if (finalResultText is null)
			throw new AssertionException("The incremental loading result label did not expose final text.");

		string[] values = finalResultText.Split(["; ", "="], StringSplitOptions.None);
		Assert.That(values, Has.Length.EqualTo(8));
		Assert.That(values[0], Is.EqualTo("Count"));
		Assert.That(values[2], Is.EqualTo("Pre"));
		Assert.That(values[4], Is.EqualTo("Post"));
		Assert.That(values[6], Is.EqualTo("Last"));

		int count = int.Parse(values[1], CultureInfo.InvariantCulture);
		int preLoadIndex = int.Parse(values[3], CultureInfo.InvariantCulture);
		int postLoadIndex = int.Parse(values[5], CultureInfo.InvariantCulture);
		Assert.That(count, Is.EqualTo(30));
		Assert.That(preLoadIndex, Is.GreaterThanOrEqualTo(3), "The gesture did not scroll the CollectionView far enough before incremental loading.");
		Assert.That(values[7], Is.EqualTo("Animal 30@Location 30"));
		Assert.That(
			postLoadIndex,
			Is.GreaterThanOrEqualTo(3),
			"CollectionView reset after incremental load: expected post-load index >= 3.");
	}
}
#endif

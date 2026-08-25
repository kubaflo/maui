#if WINDOWS
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView jumps to the top during incremental loading";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalLoadingPreservesScrollPosition()
	{
		var collectionView = App.WaitForElement("AnimalsCollectionView", timeout: TimeSpan.FromSeconds(15));
		if (collectionView is null)
			throw new AssertionException("The incremental-loading CollectionView was not found.");

		var initialAnimal = App.WaitForElement("Animal 01", timeout: TimeSpan.FromSeconds(10));
		if (initialAnimal is null)
			throw new AssertionException("The first animal was not rendered.");

		App.ScrollDown("AnimalsCollectionView", ScrollStrategy.Gesture);
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ResultLabel",
				"Reached100=True",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The first gesture did not produce a CollectionView.Scrolled callback above 100 pixels.");

		App.ScrollDown("AnimalsCollectionView", ScrollStrategy.Gesture);
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ResultLabel",
				"Reached250=True",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The second gesture did not produce a CollectionView.Scrolled callback above 250 pixels.");

		App.ScrollDown("AnimalsCollectionView", ScrollStrategy.Gesture);

		bool postLoadObserved = App.WaitForTextToBePresentInElement(
			"MeasurementStatus",
			"PostCallback=True",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(postLoadObserved, Is.True, "The post-append CollectionView.Scrolled callback was not observed.");

		var finalMeasurement = App.FindElement("MeasurementStatus");
		if (finalMeasurement is null)
			throw new AssertionException("The final CollectionView scroll measurement status was not found.");

		var finalText = finalMeasurement.GetText();
		if (finalText is null)
			throw new AssertionException("The final CollectionView scroll measurement was null.");

		Match measurement = Regex.Match(
			finalText,
			@"^Attached=(?<attached>True|False);Generation=(?<generation>\d+);Count=(?<count>\d+);PreOffset=(?<pre>-?\d+(?:\.\d+)?);PostOffset=(?<post>-?\d+(?:\.\d+)?);PostCallback=True$");
		Assert.That(measurement.Success, Is.True, $"Unexpected measurement status: {finalText}");

		bool attached = bool.Parse(measurement.Groups["attached"].Value);
		int generation = int.Parse(measurement.Groups["generation"].Value, CultureInfo.InvariantCulture);
		int count = int.Parse(measurement.Groups["count"].Value, CultureInfo.InvariantCulture);
		double preOffset = double.Parse(measurement.Groups["pre"].Value, CultureInfo.InvariantCulture);
		double postOffset = double.Parse(measurement.Groups["post"].Value, CultureInfo.InvariantCulture);

		Assert.That(attached, Is.True, "The CollectionView handler was not attached during the measured incremental load.");
		Assert.That(generation, Is.GreaterThanOrEqualTo(1));
		Assert.That(count, Is.GreaterThanOrEqualTo(20));
		Assert.That(preOffset, Is.GreaterThan(100), "The CollectionView did not reach the reported pre-load scroll position.");
		Assert.That(
			postOffset,
			Is.GreaterThan(20),
			"CollectionView returned to top after incremental load; expected the post-load offset to remain above 20.");
	}
}
#endif

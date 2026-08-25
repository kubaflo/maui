#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue24987 : _IssuesUITest
{
	public Issue24987(TestDevice device) : base(device) { }

	public override string Issue => "Shell TabBar is slow to open for the first time when combined with Grid and Border";

	[Test]
	[Category(UITestCategories.Shell)]
	public void FirstOpenTabTransitionsAreComparableToRepeatTransitions()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue24987Content");
		Assert.That(ReadText("Issue24987Content"), Is.EqualTo("MiddlePage content"));

		TapTab("NewPage1");
		AssertTransitionCompleted("NewPage1", 1);
		string newPage1Instance = ReadInstance();

		TapTab("NewPage2");
		AssertTransitionCompleted("NewPage2", 1);
		string newPage2Instance = ReadInstance();

		TapTab("NewPage1");
		AssertTransitionCompleted("NewPage1", 2);
		Assert.That(ReadInstance(), Is.EqualTo(newPage1Instance), "NewPage1 should retain the same page instance.");

		TapTab("NewPage2");
		AssertTransitionCompleted("NewPage2", 2);
		Assert.That(ReadInstance(), Is.EqualTo(newPage2Instance), "NewPage2 should retain the same page instance.");

		string metrics = ReadText("Issue24987Metrics");
		Dictionary<string, long> measurements = ParseMeasurements(metrics);
		long firstOpen = measurements["NewPage1First"] + measurements["NewPage2First"];
		long repeatOpen = measurements["NewPage1Repeat"] + measurements["NewPage2Repeat"];
		bool firstOpenWithinBudget = firstOpen <= repeatOpen;

		Assert.That(
			firstOpenWithinBudget,
			Is.True,
			$"First-open Shell tab transitions exceeded the smooth-navigation budget. First-open: {firstOpen} ms; repeat-open: {repeatOpen} ms.");
	}

	void TapTab(string title)
	{
		App.WaitForElement(AppiumQuery.ByAccessibilityId(title));
		App.Tap(AppiumQuery.ByAccessibilityId(title));
	}

	void AssertTransitionCompleted(string pageName, int transition)
	{
		string expectedContent = $"{pageName} content";
		string expectedTransition = $"{pageName} transition {transition} measured";

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue24987Content", expectedContent),
			Is.True,
			$"{pageName} content did not become visible.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue24987Transition", expectedTransition),
			Is.True,
			$"{pageName} transition {transition} callback did not occur.");
		Assert.That(ReadText("Issue24987Content"), Is.EqualTo(expectedContent));
	}

	string ReadInstance()
	{
		string transition = ReadText("Issue24987Transition");
		const string marker = "; instance ";
		int markerIndex = transition.IndexOf(marker, StringComparison.Ordinal);

		if (markerIndex < 0)
			throw new AssertionException($"Transition text did not contain an instance identity: {transition}");

		return transition[(markerIndex + marker.Length)..];
	}

	string ReadText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();

		if (text is null)
			throw new AssertionException($"{automationId} did not expose text.");

		return text;
	}

	static Dictionary<string, long> ParseMeasurements(string metrics)
	{
		var measurements = new Dictionary<string, long>(StringComparer.Ordinal);

		foreach (string measurement in metrics.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] parts = measurement.Split('=', 2);

			if (parts.Length != 2 || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long value))
				throw new AssertionException($"Invalid transition measurement: {measurement}");

			measurements.Add(parts[0], value);
		}

		string[] requiredMeasurements = ["NewPage1First", "NewPage2First", "NewPage1Repeat", "NewPage2Repeat"];
		foreach (string requiredMeasurement in requiredMeasurements)
		{
			if (!measurements.ContainsKey(requiredMeasurement))
				throw new AssertionException($"Missing transition measurement: {requiredMeasurement}");
		}

		return measurements;
	}
}
#endif

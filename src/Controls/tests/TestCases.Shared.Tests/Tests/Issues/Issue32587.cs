#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DirectContentViewReportsRenderedBoundsWhenTapped()
	{
		var sceneState = GetRequiredText("SceneState");
		Assert.That(sceneState, Is.EqualTo("One item; default styling; unconstrained sizing."));

		var referenceInitialText = GetRequiredText("ReferenceObservation");
		Assert.That(referenceInitialText, Does.Contain("Reference item: Item; sequence=-1"));

		var referenceRect = App.WaitForElement("ReferenceItem").GetRect();
		Assert.That(referenceRect.Width, Is.GreaterThan(0), "The Grid-wrapped reference item should have a positive native width.");
		Assert.That(referenceRect.Height, Is.GreaterThan(0), "The Grid-wrapped reference item should have a positive native height.");

		App.Tap("ReferenceItem");
		Assert.That(
			App.WaitForTextToBePresentInElement("ReferenceObservation", "sequence=1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The Grid-wrapped reference tap callback did not run.");

		var referenceText = GetRequiredText("ReferenceObservation");
		var referenceWidth = ParseMeasurement(referenceText, "width");
		var referenceHeight = ParseMeasurement(referenceText, "height");
		Assert.That(referenceWidth, Is.GreaterThan(0), $"The reference callback reported Width={referenceWidth}.");
		Assert.That(referenceHeight, Is.GreaterThan(0), $"The reference callback reported Height={referenceHeight}.");

		var directInitialText = GetRequiredText("DirectObservation");
		Assert.That(directInitialText, Does.Contain("Direct item: Item; sequence=-1"));

		var directRect = App.WaitForElement("DirectItem").GetRect();
		Assert.That(directRect.Width, Is.GreaterThan(0), "The direct ContentView item should have a positive native width.");
		Assert.That(directRect.Height, Is.GreaterThan(0), "The direct ContentView item should have a positive native height.");

		App.Tap("DirectItem");
		Assert.That(
			App.WaitForTextToBePresentInElement("DirectObservation", "sequence=1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The direct ContentView tap callback did not run.");

		var directText = GetRequiredText("DirectObservation");
		var directWidth = ParseMeasurement(directText, "width");
		var directHeight = ParseMeasurement(directText, "height");
		Assert.That(
			directWidth > 0 && directHeight > 0,
			Is.True,
			$"ContentView gesture bounds were invalid after a rendered Windows CollectionView item was tapped. " +
			$"Managed Width={directWidth}, Height={directHeight}; native rectangle={directRect}.");
	}

	string GetRequiredText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
		{
			throw new AssertionException($"Element '{automationId}' did not expose text.");
		}

		return text;
	}

	static double ParseMeasurement(string observation, string measurementName)
	{
		var prefix = $"{measurementName}=";
		var start = observation.IndexOf(prefix, StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Observation did not contain '{prefix}': {observation}");
		start += prefix.Length;

		var end = observation.IndexOf(';', start);
		var value = end < 0 ? observation[start..] : observation[start..end];
		Assert.That(
			double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var measurement),
			Is.True,
			$"Observation contained an invalid {measurementName}: {observation}");
		return measurement;
	}
}
#endif

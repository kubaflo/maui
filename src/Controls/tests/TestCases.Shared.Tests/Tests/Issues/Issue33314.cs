#if WINDOWS
using OpenQA.Selenium;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33314 : _IssuesUITest
{
	public override string Issue => "Editor caret becomes a dot after clearing text and hiding adjacent content";

	public Issue33314(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Editor)]
	public void CaretRemainsVerticalAfterShiftClearsTextAndHidesCancelView()
	{
		var editor = App.WaitForElement("AffectedEditor");
		App.Tap("AffectedEditor");

		App.WaitForElement("BaselineReady");
		string baselineText = GetRequiredText("BaselineMetrics");
		CaretMetrics baseline = ParseMetrics(baselineText);
		Assert.That(baseline.Generation, Is.EqualTo(0));
		Assert.That(baseline.Width, Is.GreaterThan(0));
		Assert.That(baseline.Pixels, Is.GreaterThan(0));
		Assert.That(baseline.FrameWidth, Is.GreaterThan(0));
		Assert.That(baseline.FrameHeight, Is.GreaterThan(0));
		Assert.That(baseline.Scale, Is.GreaterThan(0));
		Assert.That(baseline.TextScale, Is.GreaterThan(0));
		Assert.That(baseline.Theme, Is.Not.Empty);
		Assert.That(baseline.Height, Is.GreaterThanOrEqualTo(baseline.Minimum), "The healthy empty Editor must render a vertical baseline caret");

		editor.SendKeys("Caret reference");
		string editorText = GetRequiredText("AffectedEditor");
		Assert.That(editorText, Is.EqualTo("Caret reference"));
		App.WaitForElement("CancelView");

		editor.SendKeys(Keys.Shift);

		bool triggerCompleted = App.WaitForTextToBePresentInElement(
			"TriggerState",
			"Shift key received; text cleared; cancel hidden");
		Assert.That(triggerCompleted, Is.True);
		App.WaitForNoElement("CancelView");

		string clearedText = GetRequiredText("AffectedEditor");
		Assert.That(clearedText, Is.Empty);

		App.WaitForElement("PostCaptureReady");
		string postMetricsText = GetRequiredText("PostMetrics");
		CaretMetrics postTrigger = ParseMetrics(postMetricsText);
		Assert.That(postTrigger.Generation, Is.EqualTo(1));
		Assert.That(postTrigger.Width, Is.GreaterThan(0));
		Assert.That(postTrigger.Pixels, Is.GreaterThan(0));
		Assert.That(postTrigger.FrameWidth, Is.GreaterThan(0));
		Assert.That(postTrigger.FrameHeight, Is.GreaterThan(0));
		Assert.That(postTrigger.Scale, Is.GreaterThan(0));
		Assert.That(postTrigger.TextScale, Is.GreaterThan(0));
		Assert.That(postTrigger.Theme, Is.Not.Empty);
		Assert.That(postTrigger.Height, Is.GreaterThanOrEqualTo(postTrigger.Minimum),
			"Issue33314 caret should remain a vertical insertion line after Shift clears text and hides the cancel ContentView");
	}

	string GetRequiredText(string automationId)
	{
		var text = App.FindElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"Element {automationId} did not expose text");

		return text;
	}

	static CaretMetrics ParseMetrics(string value)
	{
		var values = value.Split(';')
			.Select(part => part.Split('=', 2))
			.Where(part => part.Length == 2)
			.ToDictionary(part => part[0], part => part[1]);

		Assert.That(values.ContainsKey("height"), Is.True, $"Caret capture did not produce a height: {value}");
		Assert.That(values.ContainsKey("minimum"), Is.True, $"Caret capture did not produce a minimum height: {value}");

		return new CaretMetrics(
			int.Parse(values["generation"]),
			int.Parse(values["height"]),
			int.Parse(values["width"]),
			int.Parse(values["pixels"]),
			int.Parse(values["minimum"]),
			int.Parse(values["frameWidth"]),
			int.Parse(values["frameHeight"]),
			double.Parse(values["scale"], System.Globalization.CultureInfo.InvariantCulture),
			double.Parse(values["textScale"], System.Globalization.CultureInfo.InvariantCulture),
			values["theme"]);
	}

	readonly record struct CaretMetrics(
		int Generation,
		int Height,
		int Width,
		int Pixels,
		int Minimum,
		int FrameWidth,
		int FrameHeight,
		double Scale,
		double TextScale,
		string Theme);
}
#endif

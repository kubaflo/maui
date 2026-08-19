#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35635 : _IssuesUITest
{
	public Issue35635(TestDevice device) : base(device)
	{
	}

	public override string Issue => "MapElements.Add repeatedly re-syncs the full element collection";

	[Test]
	[Category(UITestCategories.Maps)]
	public void LiveMapElementAddsCompleteWithoutRepeatedFullSynchronization()
	{
		App.SetOrientationPortrait();

		var androidApp = (AppiumAndroidApp)App;
		var windowSize = androidApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires portrait orientation.");

		Assert.That(
			App.WaitForTextToBePresentInElement("ReadyLabel", "Ready: attached map empty", timeout: TimeSpan.FromSeconds(30)),
			Is.True,
			"The production Android map handler did not become ready.");
		Assert.That(App.FindElement("ReferenceLabel").GetText(), Is.EqualTo("Reference: 1000 detached circles populated"));
		Assert.That(App.FindElement("StatusLabel").GetText(), Is.EqualTo("NO BUG:"));
		Assert.That(App.FindElement("TimingLabel").GetText(), Does.Contain("live: not started"));

		App.Tap("RunButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("StatusLabel", "Completed: 1000 circles", timeout: TimeSpan.FromSeconds(45)),
			Is.True,
			"The dispatched post-add completion callback did not report all 1000 circles.");
		Assert.That(App.FindElement("StatusLabel").GetText(), Is.EqualTo("Completed: 1000 circles"));

		var timing = App.FindElement("TimingLabel").GetText();
		Assert.That(timing, Is.Not.Null, "The dispatched post-add completion callback did not publish timing data.");
		var durations = ParseDurations(timing!);
		var completedWithinLimit = durations.Live < 1000;
		Assert.That(completedWithinLimit, Is.True, "Live attached add exceeded 1000 ms for 1000 circles.");

		var avoidedRepeatedFullSynchronization = durations.Live < durations.Detached * 10;
		Assert.That(
			avoidedRepeatedFullSynchronization,
			Is.True,
			"Live attached add was at least ten times detached population.");
	}

	static (long Detached, long Live) ParseDurations(string timing)
	{
		const string detachedPrefix = "Detached: ";
		const string separator = " ms; live: ";
		const string suffix = " ms";

		Assert.That(timing, Does.StartWith(detachedPrefix).And.EndWith(suffix));
		var values = timing[detachedPrefix.Length..^suffix.Length].Split(separator, StringSplitOptions.None);
		Assert.That(values, Has.Length.EqualTo(2));

		return (
			long.Parse(values[0], CultureInfo.InvariantCulture),
			long.Parse(values[1], CultureInfo.InvariantCulture));
	}
}
#endif

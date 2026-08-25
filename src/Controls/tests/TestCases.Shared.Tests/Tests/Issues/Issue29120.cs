#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice device) : base(device) { }

	public override string Issue => "Incremental loading jumps back to the top of the list";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalLoadingPreservesVisibleRange()
	{
		const string CollectionId = "Issue29120Collection";
		const string TelemetryId = "Issue29120Telemetry";

		var collection = App.WaitForElement(CollectionId);

		var initialCollectionRect = collection.GetRect();
		var initialFirstItemRect = App.WaitForElement("Bear 1").GetRect();
		Assert.That(
			initialCollectionRect.IntersectsWith(initialFirstItemRect),
			Is.True,
			"Bear 1 should initially be rendered inside the native CollectionView viewport.");

		collection.Tap();
		collection.SendKeys(Keys.PageDown);
		collection.SendKeys(Keys.PageDown);
		collection.SendKeys(Keys.PageDown);
		collection.SendKeys(Keys.PageDown);

		Assert.That(
			App.WaitForTextToBePresentInElement(TelemetryId, "ThresholdAway=True"),
			Is.True,
			"The incremental-load threshold was not reached after scrolling away from index 0.");
		Assert.That(
			App.WaitForTextToBePresentInElement(TelemetryId, "AppendComplete=True"),
			Is.True,
			"The bound ObservableCollection did not finish an incremental append away from index 0.");
		Assert.That(
			App.WaitForTextToBePresentInElement(TelemetryId, "PostObserved=True"),
			Is.True,
			"No Scrolled callback was observed after the incremental append.");

		var statusText = App.WaitForElement(TelemetryId).GetText()
			?? throw new AssertionException("Issue29120 telemetry text was null.");

		static string ReadTelemetry(string telemetry, string key)
		{
			var prefix = $"{key}=";
			foreach (var entry in telemetry.Split(';'))
			{
				if (entry.StartsWith(prefix, StringComparison.Ordinal))
					return entry[prefix.Length..];
			}

			throw new AssertionException($"Telemetry did not contain '{key}': {telemetry}");
		}

		var thresholdIndex = int.Parse(ReadTelemetry(statusText, "Threshold"));
		var postAppendIndex = int.Parse(ReadTelemetry(statusText, "Post"));
		var postAppendIdentity = ReadTelemetry(statusText, "Identity");
		var itemCount = int.Parse(ReadTelemetry(statusText, "Count"));

		Assert.That(
			thresholdIndex,
			Is.GreaterThan(0),
			$"The incremental load did not begin away from the first item; status={statusText}.");
		Assert.That(
			postAppendIndex,
			Is.GreaterThan(0),
			$"CollectionView jumped to the first item after incremental loading; " +
			$"pre-load index={thresholdIndex}; post-load index={postAppendIndex}; identity={postAppendIdentity}; " +
			$"count={itemCount}.");
	}
}
#endif

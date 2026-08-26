#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29522 : _IssuesUITest
{
	public override string Issue => "Scaled Editor is behind the keyboard on Android";

	public Issue29522(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.Editor)]
	public void ScaledEditorRemainsAboveKeyboard()
	{
		Dictionary<string, int> ReadMetrics(IUIElement element)
		{
			var text = element.GetText();
			if (text is null)
				throw new AssertionException("The native metrics element did not contain text.");

			var values = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var pair in text.Split(';'))
			{
				var separator = pair.IndexOf('=', StringComparison.Ordinal);
				if (separator <= 0 ||
					!int.TryParse(pair.AsSpan(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
				{
					throw new AssertionException($"Invalid native metrics: {text}");
				}

				values.Add(pair[..separator], value);
			}

			return values;
		}

		int GetMetric(Dictionary<string, int> metrics, string name)
		{
			if (!metrics.TryGetValue(name, out int value))
				throw new AssertionException($"Native metric '{name}' was not reported.");

			return value;
		}

		App.SetOrientationPortrait();
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires a portrait app window.");

		var editorElement = App.WaitForElement("Issue29522Editor10");
		if (editorElement is null)
			throw new AssertionException("Editor 10 was not found.");

		var editorRect = editorElement.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(editorRect.Width, Is.GreaterThan(0), "Editor 10 must have a nonzero native width.");
			Assert.That(editorRect.Height, Is.GreaterThan(0), "Editor 10 must have a nonzero native height.");
		});

		var baselineElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue29522Metrics");
			if (element is null)
				return null;

			var metrics = ReadMetrics(element);
			return GetMetric(metrics, "seq") >= 0 &&
				GetMetric(metrics, "focused") == 0 &&
				GetMetric(metrics, "width") > 0 &&
				GetMetric(metrics, "height") > 0
					? element
					: null;
		}, "Timed out waiting for the pre-focus native Editor 10 metrics.");

		var baseline = ReadMetrics(baselineElement);
		var baselineSequence = GetMetric(baseline, "seq");
		var baselineVisibleBottom = GetMetric(baseline, "visible");
		var baselineEditorBottom = GetMetric(baseline, "bottom");
		Assert.That(
			baselineEditorBottom,
			Is.LessThanOrEqualTo(baselineVisibleBottom + 2),
			$"Editor 10 must not overlap the visible display frame before focus. Editor bottom: {baselineEditorBottom}; visible bottom: {baselineVisibleBottom}.");

		App.Tap("Issue29522Editor10");
		var keyboardShown = App.WaitForKeyboardToShow(TimeSpan.FromSeconds(10));
		Assert.That(keyboardShown, Is.True, "The Android soft keyboard must be visible after tapping Editor 10.");

		var focusedElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue29522Metrics");
			if (element is null)
				return null;

			var metrics = ReadMetrics(element);
			return GetMetric(metrics, "seq") > baselineSequence &&
				GetMetric(metrics, "focused") == 1 &&
				GetMetric(metrics, "visible") < baselineVisibleBottom
					? element
					: null;
		}, "Timed out waiting for a focused Editor 10 callback with a contracted visible display frame.");

		Assert.That(App.IsKeyboardShown(), Is.True, "The keyboard must remain visible while the native geometry is sampled.");
		var focusedMetrics = ReadMetrics(focusedElement);
		var focusedSequence = GetMetric(focusedMetrics, "seq");

		App.Tap("Issue29522CheckOverlap");
		var snapshotElement = App.WaitForElement(() =>
		{
			var element = App.FindElement("Issue29522Metrics");
			if (element is null)
				return null;

			var metrics = ReadMetrics(element);
			return GetMetric(metrics, "snapshotSeq") >= focusedSequence ? element : null;
		}, "Timed out waiting for the native metric snapshot.");

		var snapshot = ReadMetrics(snapshotElement);
		var editorBottom = GetMetric(snapshot, "snapshotBottom");
		var visibleBottom = GetMetric(snapshot, "snapshotVisible");
		Assert.That(
			editorBottom,
			Is.LessThanOrEqualTo(visibleBottom + 2),
			$"Scaled Editor 10 should remain above the Android keyboard. Editor bottom: {editorBottom}; visible display-frame bottom: {visibleBottom}.");
	}
}
#endif

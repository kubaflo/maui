#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29125 : _IssuesUITest
{
	public Issue29125(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "[Windows] Slider thumb image is rendered too large";

	[Test]
	[Category(UITestCategories.Slider)]
	public void ThumbImageRetainsDefaultThumbAndSliderSize()
	{
		var initialSliderRect = App.WaitForElement("Issue29125Slider").GetRect();
		Assert.That(initialSliderRect.Width, Is.GreaterThan(0));
		Assert.That(initialSliderRect.Height, Is.GreaterThan(0));

		var initialText = string.Empty;
		App.RetryAssert(() =>
		{
			var text = App.WaitForElement("Issue29125Result").GetText();
			if (text is null)
				Assert.Fail("Initial native Slider measurements were not exposed.");

			initialText = text;
			Assert.That(text, Does.Contain("defaultWidth="));
		});

		var initialMetrics = ParseMetrics(initialText);
		Assert.That(initialMetrics["token"], Is.EqualTo("-1"));
		Assert.That(initialMetrics["source"], Is.EqualTo("none"));
		var defaultWidth = GetDouble(initialMetrics, "defaultWidth");
		var defaultHeight = GetDouble(initialMetrics, "defaultHeight");
		Assert.That(defaultWidth, Is.GreaterThan(0));
		Assert.That(defaultHeight, Is.GreaterThan(0));
		Assert.That(GetDouble(initialMetrics, "thumbWidth"), Is.EqualTo(defaultWidth).Within(1));
		Assert.That(GetDouble(initialMetrics, "thumbHeight"), Is.EqualTo(defaultHeight).Within(1));

		App.WaitForElement("Issue29125SetThumbImage");
		App.Tap("Issue29125SetThumbImage");

		var settledText = string.Empty;
		App.RetryAssert(() =>
		{
			var text = App.WaitForElement("Issue29125Result").GetText();
			if (text is null)
				Assert.Fail("Post-image native Slider measurements were not exposed.");

			settledText = text;
			Assert.That(text, Does.StartWith("token=1;source=dotnet_bot.png;sameThumb=1;"));
		});

		var settledMetrics = ParseMetrics(settledText);
		Assert.That(settledMetrics["token"], Is.EqualTo("1"));
		Assert.That(settledMetrics["source"], Is.EqualTo("dotnet_bot.png"));
		Assert.That(settledMetrics["sameThumb"], Is.EqualTo("1"));

		var settledSliderRect = App.WaitForElement("Issue29125Slider").GetRect();
		Assert.That(
			settledSliderRect.Height <= initialSliderRect.Height + 1,
			Is.True,
			"Slider thumb image enlarged native slider height:");

		Assert.That(GetDouble(settledMetrics, "thumbWidth"), Is.EqualTo(defaultWidth).Within(1));
		Assert.That(GetDouble(settledMetrics, "thumbHeight"), Is.EqualTo(defaultHeight).Within(1));
	}

	static Dictionary<string, string> ParseMetrics(string text)
	{
		var metrics = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var component in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			var pair = component.Split('=', 2);
			Assert.That(pair, Has.Length.EqualTo(2), $"Invalid native metric '{component}'.");
			metrics.Add(pair[0], pair[1]);
		}

		return metrics;
	}

	static double GetDouble(IReadOnlyDictionary<string, string> metrics, string key)
	{
		Assert.That(metrics.ContainsKey(key), Is.True, $"Native metric '{key}' was not reported.");
		Assert.That(
			double.TryParse(metrics[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var value),
			Is.True,
			$"Native metric '{key}' was not numeric.");
		return value;
	}
}
#endif

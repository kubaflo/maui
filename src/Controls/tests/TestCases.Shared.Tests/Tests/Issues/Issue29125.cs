#if WINDOWS
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29125 : _IssuesUITest
{
	public Issue29125(TestDevice device) : base(device)
	{
	}

	public override string Issue => "[Windows] Slider thumb image renders too large";

	[Test]
	[Category(UITestCategories.Slider)]
	public void ImageThumbRetainsDefaultNativeSize()
	{
		var baselineElement = App.WaitForElement("Issue29125BaselineReport");
		Assert.That(baselineElement, Is.Not.Null);
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29125BaselineReport", "Pending=-1", TimeSpan.FromSeconds(15)),
			Is.True,
			"The attached native default thumb was not measured.");

		var baseline = baselineElement.GetText();
		if (baseline is null)
			throw new AssertionException("The default thumb report was null.");

		var defaultWidth = ReadMeasurement(baseline, "DefaultWidth");
		var defaultHeight = ReadMeasurement(baseline, "DefaultHeight");
		var styledWidth = ReadMeasurement(baseline, "StyledWidth");
		var styledHeight = ReadMeasurement(baseline, "StyledHeight");
		Assert.That(defaultWidth, Is.GreaterThan(0), "The default native Thumb width must be positive.");
		Assert.That(defaultHeight, Is.GreaterThan(0), "The default native Thumb height must be positive.");
		Assert.That(Math.Abs(defaultWidth - styledWidth), Is.LessThanOrEqualTo(1), "The native Thumb width must match its default style.");
		Assert.That(Math.Abs(defaultHeight - styledHeight), Is.LessThanOrEqualTo(1), "The native Thumb height must match its default style.");

		var pendingElement = App.WaitForElement("Issue29125ResultReport");
		Assert.That(pendingElement, Is.Not.Null);
		Assert.That(pendingElement.GetText(), Is.EqualTo("Sequence=-1"));

		App.Tap("Issue29125ApplyButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29125ResultReport", "Source=True", TimeSpan.FromSeconds(15)),
			Is.True,
			"The native image Thumb SizeChanged callback did not publish a result.");

		var resultElement = App.FindElement("Issue29125ResultReport");
		Assert.That(resultElement, Is.Not.Null);
		var result = resultElement.GetText();
		if (result is null)
			throw new AssertionException("The image thumb report was null.");

		Assert.That(ReadMeasurement(result, "Sequence"), Is.GreaterThan(0), "The post-assignment native SizeChanged callback must occur.");
		Assert.That(result, Does.Contain("Source=True"), "The bundled groceries.png source must be applied.");
		Assert.That(result, Does.Contain("ImageTemplate=True"), "The native image-thumb template must be active.");

		var imageWidth = ReadMeasurement(result, "ImageWidth");
		var imageHeight = ReadMeasurement(result, "ImageHeight");
		Assert.Multiple(() =>
		{
			Assert.That(
				Math.Abs(imageWidth - defaultWidth),
				Is.LessThanOrEqualTo(1),
				$"Slider image thumb must retain the captured default native size; default={defaultWidth:0.###}x{defaultHeight:0.###}, observed={imageWidth:0.###}x{imageHeight:0.###}");
			Assert.That(
				Math.Abs(imageHeight - defaultHeight),
				Is.LessThanOrEqualTo(1),
				$"Slider image thumb must retain the captured default native size; default={defaultWidth:0.###}x{defaultHeight:0.###}, observed={imageWidth:0.###}x{imageHeight:0.###}");
		});
	}

	static double ReadMeasurement(string report, string key)
	{
		var match = Regex.Match(report, $@"(?:^|;){Regex.Escape(key)}=([0-9]+(?:\.[0-9]+)?)");
		Assert.That(match.Success, Is.True, $"Measurement '{key}' was missing from '{report}'.");
		return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
	}
}
#endif

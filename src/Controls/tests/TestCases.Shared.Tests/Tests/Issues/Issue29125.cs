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

	public override string Issue => "[Windows] Slider thumb image is rendered too large";

	[Test]
	[Category(UITestCategories.Slider)]
	public void ImageThumbShouldUsePlatformDefaultThumbSize()
	{
		App.WaitForElement("DefaultSlider");
		App.WaitForNoElement("ImageSlider");
		App.WaitForElement("ShowSliderButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultStatus", "Default thumb measured."),
			Is.True,
			"The default native thumb callback did not complete.");

		var defaultDetails = App.WaitForElement("MeasurementDetails").GetText();
		if (defaultDetails is null)
			throw new AssertionException("The default native thumb measurement was null.");

		var defaultMatch = Regex.Match(
			defaultDetails,
			@"^phase=default;sequence=(?<sequence>-?\d+);native=True;thumb=True;hosted=True;source=False;value=(?<value>[\d.]+);defaultWidth=(?<width>[\d.]+);defaultHeight=(?<height>[\d.]+);templateWidth=(?<templateWidth>[\d.]+);templateHeight=(?<templateHeight>[\d.]+)$",
			RegexOptions.CultureInvariant);
		if (!defaultMatch.Success)
			throw new AssertionException($"The default native thumb state was incomplete: {defaultDetails}");

		var defaultSequence = int.Parse(defaultMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
		var defaultValue = double.Parse(defaultMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
		var defaultWidth = double.Parse(defaultMatch.Groups["width"].Value, CultureInfo.InvariantCulture);
		var defaultHeight = double.Parse(defaultMatch.Groups["height"].Value, CultureInfo.InvariantCulture);
		var templateWidth = double.Parse(defaultMatch.Groups["templateWidth"].Value, CultureInfo.InvariantCulture);
		var templateHeight = double.Parse(defaultMatch.Groups["templateHeight"].Value, CultureInfo.InvariantCulture);

		Assert.That(defaultSequence, Is.GreaterThanOrEqualTo(0));
		Assert.That(defaultValue, Is.EqualTo(0.5).Within(0.001));
		Assert.That(defaultWidth, Is.GreaterThan(0));
		Assert.That(defaultHeight, Is.GreaterThan(0));
		Assert.That(templateWidth, Is.GreaterThan(0));
		Assert.That(templateHeight, Is.GreaterThan(0));
		Assert.That(defaultWidth, Is.LessThanOrEqualTo(templateWidth + 0.5));
		Assert.That(defaultHeight, Is.LessThanOrEqualTo(templateHeight + 0.5));

		App.Tap("ShowSliderButton");
		App.WaitForNoElement("DefaultSlider");
		App.WaitForElement("ImageSlider");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultStatus", "Image thumb measured."),
			Is.True,
			"The image-backed native thumb callback did not complete.");

		var imageDetails = App.WaitForElement("MeasurementDetails").GetText();
		if (imageDetails is null)
			throw new AssertionException("The image-backed native thumb measurement was null.");

		var imageMatch = Regex.Match(
			imageDetails,
			@"^phase=image;sequence=(?<sequence>-?\d+);native=True;thumb=True;hosted=True;source=True;value=(?<value>[\d.]+);defaultWidth=(?<defaultWidth>[\d.]+);defaultHeight=(?<defaultHeight>[\d.]+);templateWidth=(?<templateWidth>[\d.]+);templateHeight=(?<templateHeight>[\d.]+);imageWidth=(?<imageWidth>[\d.]+);imageHeight=(?<imageHeight>[\d.]+)$",
			RegexOptions.CultureInvariant);
		if (!imageMatch.Success)
			throw new AssertionException($"The image-backed native thumb state was incomplete: {imageDetails}");

		var imageSequence = int.Parse(imageMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
		var imageValue = double.Parse(imageMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
		var measuredDefaultWidth = double.Parse(imageMatch.Groups["defaultWidth"].Value, CultureInfo.InvariantCulture);
		var measuredDefaultHeight = double.Parse(imageMatch.Groups["defaultHeight"].Value, CultureInfo.InvariantCulture);
		var measuredTemplateWidth = double.Parse(imageMatch.Groups["templateWidth"].Value, CultureInfo.InvariantCulture);
		var measuredTemplateHeight = double.Parse(imageMatch.Groups["templateHeight"].Value, CultureInfo.InvariantCulture);
		var imageWidth = double.Parse(imageMatch.Groups["imageWidth"].Value, CultureInfo.InvariantCulture);
		var imageHeight = double.Parse(imageMatch.Groups["imageHeight"].Value, CultureInfo.InvariantCulture);

		Assert.That(imageSequence, Is.GreaterThan(defaultSequence));
		Assert.That(imageValue, Is.EqualTo(0.5).Within(0.001));
		Assert.That(measuredDefaultWidth, Is.EqualTo(defaultWidth).Within(0.001));
		Assert.That(measuredDefaultHeight, Is.EqualTo(defaultHeight).Within(0.001));
		Assert.That(measuredTemplateWidth, Is.EqualTo(templateWidth).Within(0.001));
		Assert.That(measuredTemplateHeight, Is.EqualTo(templateHeight).Within(0.001));
		Assert.That(imageWidth, Is.GreaterThan(0));
		Assert.That(imageHeight, Is.GreaterThan(0));

		var imageThumbUsesDefaultSize =
			imageWidth <= templateWidth + 0.5 &&
			imageHeight <= templateHeight + 0.5;
		Assert.That(
			imageThumbUsesDefaultSize,
			Is.True,
			$"Image-backed Slider thumb exceeded the platform-default thumb dimensions; default={defaultWidth:R}x{defaultHeight:R}, template={templateWidth:R}x{templateHeight:R}, image={imageWidth:R}x{imageHeight:R}");
	}
}
#endif

#if ANDROID
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32465 : _IssuesUITest
{
	const byte WhiteLevel = 245;
	const double GreyLevel = 128;

	public Issue32465(TestDevice device) : base(device) { }

	public override string Issue => "GraphicsView line stroke rendering is inconsistent on Android";

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void EqualOneDipGridStrokesHaveUniformRenderedCoverage()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The grid stroke reproduction requires portrait orientation.");

		var initialResult = App.WaitForElement("Issue32465InitialDrawResult")
			?? throw new InvalidOperationException("The initial draw result was not found.");
		Assert.That(initialResult.GetText(), Is.EqualTo("NOT DRAWN"));
		Assert.That(App.FindElements("Issue32465GraphicsView"), Is.Empty);

		App.Tap("Issue32465OpenGridButton");

		var graphicsElement = App.WaitForElement("Issue32465GraphicsView")
			?? throw new InvalidOperationException("The GraphicsView was not found after navigation.");
		var overlayElement = App.WaitForElement("Issue32465Overlay")
			?? throw new InvalidOperationException("The grid overlay was not found after navigation.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32465DrawResult", "DRAWN", TimeSpan.FromSeconds(10)),
			Is.True,
			"The newly attached GraphicsView did not complete a draw.");

		var resultElement = App.FindElement("Issue32465DrawResult")
			?? throw new InvalidOperationException("The draw result was not found after navigation.");
		string resultText = resultElement.GetText()
			?? throw new InvalidOperationException("The draw result did not expose text.");

		var drawMatch = Regex.Match(
			resultText,
			@"^DRAWN count=(\d+) width=([0-9.]+) height=([0-9.]+) stroke=([0-9.]+) color=(\w+)$",
			RegexOptions.CultureInvariant);
		Assert.That(drawMatch.Success, Is.True, $"Unexpected draw report: {resultText}");

		int drawCount = int.Parse(drawMatch.Groups[1].Value, CultureInfo.InvariantCulture);
		double drawWidth = double.Parse(drawMatch.Groups[2].Value, CultureInfo.InvariantCulture);
		double drawHeight = double.Parse(drawMatch.Groups[3].Value, CultureInfo.InvariantCulture);
		double strokeSize = double.Parse(drawMatch.Groups[4].Value, CultureInfo.InvariantCulture);
		Assert.That(drawCount, Is.GreaterThan(0));
		Assert.That(drawWidth, Is.GreaterThan(0));
		Assert.That(drawHeight, Is.GreaterThan(0));
		Assert.That(strokeSize, Is.EqualTo(1));
		Assert.That(drawMatch.Groups[5].Value, Is.EqualTo("Grey"));

		var graphicsRect = graphicsElement.GetRect();
		var overlayRect = overlayElement.GetRect();
		Assert.That(graphicsRect.Width, Is.GreaterThan(0));
		Assert.That(graphicsRect.Height, Is.GreaterThan(0));
		Assert.That(overlayRect.Bottom, Is.GreaterThan(graphicsRect.Top));
		double horizontalDensity = graphicsRect.Width / drawWidth;
		double verticalDensity = graphicsRect.Height / drawHeight;
		Assert.That(
			horizontalDensity,
			Is.EqualTo(verticalDensity).Within(0.05),
			$"The native and drawable rectangles did not establish a consistent density: horizontal={horizontalDensity:F3}, vertical={verticalDensity:F3}.");
		double density = (horizontalDensity + verticalDensity) / 2;
		Assert.That(density, Is.GreaterThan(0));

		byte[] screenshot = App.Screenshot()
			?? throw new InvalidOperationException("The rendered screen could not be captured.");
		using var image = new MagickImage(screenshot);
		image.Depth = 8;
		byte[] rgb = image.ToByteArray(MagickFormat.Rgb);
		int imageWidth = checked((int)image.Width);
		int imageHeight = checked((int)image.Height);
		Assert.That(rgb.Length, Is.EqualTo(imageWidth * imageHeight * 3), "The screenshot did not decode to RGB pixels.");

		AssertRectIsInImage(graphicsRect, imageWidth, imageHeight);
		double backgroundLuma = ValidateWhiteCellInteriors(rgb, imageWidth, imageHeight, graphicsRect, overlayRect);

		int sampleRadius = Math.Max(3, (int)Math.Ceiling(density * 1.5));
		var coverages = new List<double>();
		var peakCoverages = new List<double>();
		var apparentWidths = new List<double>();
		var descriptions = new List<string>();

		double sampleTop = Math.Max(overlayRect.Bottom + (12 * density), graphicsRect.Top + (graphicsRect.Height * 0.35));
		double[] verticalSampleYs =
		[
			sampleTop,
			graphicsRect.Top + (graphicsRect.Height * 0.68),
			graphicsRect.Top + (graphicsRect.Height * 0.86)
		];

		for (int column = 1; column < 6; column++)
		{
			double lineX = graphicsRect.Left + (graphicsRect.Width * column / 6.0);
			var lineSamples = verticalSampleYs
				.Where(y => y < graphicsRect.Bottom - sampleRadius)
				.Select(y => (
					Coverage: MeasureHorizontalCrossSection(rgb, imageWidth, imageHeight, lineX, y, sampleRadius, backgroundLuma),
					Peak: MeasureHorizontalPeakCoverage(rgb, imageWidth, imageHeight, lineX, y, sampleRadius, backgroundLuma),
					Width: MeasureHorizontalApparentWidth(rgb, imageWidth, imageHeight, lineX, y, sampleRadius, backgroundLuma)))
				.ToArray();
			Assert.That(lineSamples, Is.Not.Empty, $"No unobstructed samples were available for vertical line {column}.");
			double coverage = lineSamples.Average(sample => sample.Coverage);
			double peakCoverage = lineSamples.Average(sample => sample.Peak);
			double apparentWidth = lineSamples.Average(sample => sample.Width);
			coverages.Add(coverage);
			peakCoverages.Add(peakCoverage);
			apparentWidths.Add(apparentWidth);
			descriptions.Add($"V{column}=coverage:{coverage:F2},peak:{peakCoverage:F2},width:{apparentWidth:F2}");
		}

		for (int row = 1; row < 10; row++)
		{
			double lineY = graphicsRect.Top + (graphicsRect.Height * row / 10.0);
			if (lineY <= overlayRect.Bottom + sampleRadius)
				continue;

			double[] horizontalSampleXs =
			[
				graphicsRect.Left + (graphicsRect.Width * 0.18),
				graphicsRect.Left + (graphicsRect.Width * 0.47),
				graphicsRect.Left + (graphicsRect.Width * 0.82)
			];
			var lineSamples = horizontalSampleXs
				.Select(x => (
					Coverage: MeasureVerticalCrossSection(rgb, imageWidth, imageHeight, x, lineY, sampleRadius, backgroundLuma),
					Peak: MeasureVerticalPeakCoverage(rgb, imageWidth, imageHeight, x, lineY, sampleRadius, backgroundLuma),
					Width: MeasureVerticalApparentWidth(rgb, imageWidth, imageHeight, x, lineY, sampleRadius, backgroundLuma)))
				.ToArray();
			double coverage = lineSamples.Average(sample => sample.Coverage);
			double peakCoverage = lineSamples.Average(sample => sample.Peak);
			double apparentWidth = lineSamples.Average(sample => sample.Width);
			coverages.Add(coverage);
			peakCoverages.Add(peakCoverage);
			apparentWidths.Add(apparentWidth);
			descriptions.Add($"H{row}=coverage:{coverage:F2},peak:{peakCoverage:F2},width:{apparentWidth:F2}");
		}

		Assert.That(coverages.Count, Is.GreaterThanOrEqualTo(10), "Too few unobstructed grid lines were measured.");

		double tolerance = Math.Max(0.55, density * 0.22);
		double spread = coverages.Max() - coverages.Min();
		double peakSpread = peakCoverages.Max() - peakCoverages.Min();
		double widthSpread = apparentWidths.Max() - apparentWidths.Min();
		bool allLinesHaveOneDipCoverage = coverages.All(coverage => Math.Abs(coverage - density) <= tolerance);
		bool coverageIsUniform = spread <= Math.Max(0.45, density * 0.18);
		bool appearanceIsUniform = peakSpread <= 0.12 && widthSpread <= 0.25;

		Assert.That(
			allLinesHaveOneDipCoverage && coverageIsUniform && appearanceIsUniform,
			Is.True,
			$"Issue32465 grid stroke appearance was inconsistent: measured [{string.Join(", ", descriptions)}], expected {density:F2}+/-{tolerance:F2} pixels with coverage spread <= {Math.Max(0.45, density * 0.18):F2}, peak-darkness spread <= 0.12, and apparent-width spread <= 0.25 pixels; actual coverage spread={spread:F2}, peak-darkness spread={peakSpread:F2}, apparent-width spread={widthSpread:F2}, density={density:F3}, rect={graphicsRect}.");
	}

	static double ValidateWhiteCellInteriors(byte[] rgb, int width, int height, Rectangle rect, Rectangle overlay)
	{
		var samplePoints = new[]
		{
			(rect.Left + (rect.Width * 0.08), rect.Top + (rect.Height * 0.45)),
			(rect.Left + (rect.Width * 0.42), rect.Top + (rect.Height * 0.73)),
			(rect.Left + (rect.Width * 0.75), rect.Top + (rect.Height * 0.83))
		};

		var sampleLumas = new List<double>();
		foreach ((double x, double y) in samplePoints)
		{
			Assert.That(y, Is.GreaterThan(overlay.Bottom + 3), "A white-cell validation sample overlapped the page overlay.");
			double average = AverageLuma(rgb, width, height, (int)Math.Round(x), (int)Math.Round(y), 3);
			Assert.That(average, Is.GreaterThanOrEqualTo(WhiteLevel), $"Expected a white cell interior at ({x:F0},{y:F0}), but measured luma {average:F1}.");
			sampleLumas.Add(average);
		}

		return sampleLumas.Average();
	}

	static double MeasureHorizontalCrossSection(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerX - radius, (radius * 2) + 1)
			.Sum(sampleX => PixelCoverage(rgb, width, height, sampleX, centerY, backgroundLuma));
	}

	static double MeasureVerticalCrossSection(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerY - radius, (radius * 2) + 1)
			.Sum(sampleY => PixelCoverage(rgb, width, height, centerX, sampleY, backgroundLuma));
	}

	static double MeasureHorizontalPeakCoverage(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerX - radius, (radius * 2) + 1)
			.Max(sampleX => PixelCoverage(rgb, width, height, sampleX, centerY, backgroundLuma));
	}

	static double MeasureVerticalPeakCoverage(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerY - radius, (radius * 2) + 1)
			.Max(sampleY => PixelCoverage(rgb, width, height, centerX, sampleY, backgroundLuma));
	}

	static int MeasureHorizontalApparentWidth(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerX - radius, (radius * 2) + 1)
			.Count(sampleX => PixelCoverage(rgb, width, height, sampleX, centerY, backgroundLuma) >= 0.5);
	}

	static int MeasureVerticalApparentWidth(byte[] rgb, int width, int height, double x, double y, int radius, double backgroundLuma)
	{
		int centerX = (int)Math.Round(x);
		int centerY = (int)Math.Round(y);
		return Enumerable.Range(centerY - radius, (radius * 2) + 1)
			.Count(sampleY => PixelCoverage(rgb, width, height, centerX, sampleY, backgroundLuma) >= 0.5);
	}

	static double PixelCoverage(byte[] rgb, int width, int height, int x, int y, double backgroundLuma)
	{
		Assert.That(x, Is.InRange(0, width - 1));
		Assert.That(y, Is.InRange(0, height - 1));
		double luma = GetLuma(rgb, width, x, y);
		return Math.Clamp((backgroundLuma - luma) / (backgroundLuma - GreyLevel), 0, 1);
	}

	static double AverageLuma(byte[] rgb, int width, int height, int centerX, int centerY, int radius)
	{
		double total = 0;
		int count = 0;
		for (int y = centerY - radius; y <= centerY + radius; y++)
		{
			for (int x = centerX - radius; x <= centerX + radius; x++)
			{
				Assert.That(x, Is.InRange(0, width - 1));
				Assert.That(y, Is.InRange(0, height - 1));
				total += GetLuma(rgb, width, x, y);
				count++;
			}
		}

		return total / count;
	}

	static double GetLuma(byte[] rgb, int width, int x, int y)
	{
		int offset = ((y * width) + x) * 3;
		return (rgb[offset] * 0.2126) + (rgb[offset + 1] * 0.7152) + (rgb[offset + 2] * 0.0722);
	}

	static void AssertRectIsInImage(Rectangle rect, int width, int height)
	{
		Assert.That(rect.Left, Is.GreaterThanOrEqualTo(0));
		Assert.That(rect.Top, Is.GreaterThanOrEqualTo(0));
		Assert.That(rect.Right, Is.LessThanOrEqualTo(width));
		Assert.That(rect.Bottom, Is.LessThanOrEqualTo(height));
	}
}
#endif

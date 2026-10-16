#if IOS
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue17525 : _IssuesUITest
{
	public Issue17525(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Polygon Borders do not properly calculate their inner path";

	[Test]
	[Category(UITestCategories.Border)]
	public void PolygonContentIsClippedInsideItsStroke()
	{
		App.SetOrientationPortrait();

		string[] borderIds =
		[
			"CircleLabelBorder",
			"CircleImageBorder",
			"RoundRectangleLabelBorder",
			"RoundRectangleImageBorder",
			"TriangleLabelBorder",
			"TriangleImageBorder"
		];

		var borderRects = borderIds.ToDictionary(
			borderId => borderId,
			_ => new Rectangle(-1, -1, -1, -1));
		foreach (var borderId in borderIds)
		{
			var element = App.WaitForElement(borderId);
			Assert.That(element, Is.Not.Null, $"{borderId} should exist.");

			var rect = element.GetRect();
			Assert.That(rect.Width, Is.EqualTo(101).Within(2), $"{borderId} should retain its requested width.");
			Assert.That(rect.Height, Is.EqualTo(101).Within(2), $"{borderId} should retain its requested height.");
			borderRects[borderId] = rect;
		}

		var portraitSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(portraitSize.Height, Is.GreaterThan(portraitSize.Width),
			"The test window should be in the reported portrait orientation.");
		Assert.That(borderRects["CircleLabelBorder"].Y, Is.LessThan(borderRects["RoundRectangleLabelBorder"].Y));
		Assert.That(borderRects["RoundRectangleLabelBorder"].Y, Is.LessThan(borderRects["TriangleLabelBorder"].Y));
		Assert.That(borderRects["CircleImageBorder"].X, Is.GreaterThan(borderRects["CircleLabelBorder"].X));

		byte[] screenshotBytes = [];
		App.RetryAssert(() =>
		{
			screenshotBytes = App.Screenshot() ?? throw new InvalidOperationException("Appium returned no screenshot data.");
			var candidate = PixelImage.Decode(screenshotBytes);
			var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
			var scaleX = candidate.Width / (double)windowSize.Width;
			var scaleY = candidate.Height / (double)windowSize.Height;

			var labelSurfacePixels = CountPixels(
				candidate,
				borderRects["TriangleLabelBorder"],
				scaleX,
				scaleY,
				20,
				20,
				60,
				70,
				(color) => color.Red > color.Green + 50 && color.Red > color.Blue + 50);

			var imageColors = CollectImageColors(
				candidate,
				borderRects["TriangleImageBorder"],
				scaleX,
				scaleY);

			Assert.That(labelSurfacePixels, Is.GreaterThan(20), "The translucent red label content should be rendered.");
			Assert.That(imageColors.Count, Is.GreaterThan(5), "The packaged oasis image should be rendered with varied colors.");
		});

		Assert.That(screenshotBytes, Is.Not.Empty, "The post-layout screenshot transition should complete.");

		var screenshot = PixelImage.Decode(screenshotBytes);
		var screenSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var screenshotScaleX = screenshot.Width / (double)screenSize.Width;
		var screenshotScaleY = screenshot.Height / (double)screenSize.Height;

		var circleCoverage = MeasureEllipseStrokeCoverage(
			screenshot,
			borderRects["CircleLabelBorder"],
			screenshotScaleX,
			screenshotScaleY);
		var roundRectangleCoverage = MeasureRoundRectangleStrokeCoverage(
			screenshot,
			borderRects["RoundRectangleLabelBorder"],
			screenshotScaleX,
			screenshotScaleY);

		Assert.That(circleCoverage, Is.GreaterThanOrEqualTo(0.35),
			$"The healthy Ellipse reference should classify as LightGreen stroke; coverage={circleCoverage:P1}.");
		Assert.That(roundRectangleCoverage, Is.GreaterThanOrEqualTo(0.35),
			$"The healthy RoundRectangle reference should classify as LightGreen stroke; coverage={roundRectangleCoverage:P1}.");

		var labelEdgeCoverage = MeasurePolygonStrokeCoverage(
			screenshot,
			borderRects["TriangleLabelBorder"],
			screenshotScaleX,
			screenshotScaleY);
		var imageEdgeCoverage = MeasurePolygonStrokeCoverage(
			screenshot,
			borderRects["TriangleImageBorder"],
			screenshotScaleX,
			screenshotScaleY);

		const double minimumCoverage = 0.55;
		var minimumObservedCoverage = labelEdgeCoverage.Concat(imageEdgeCoverage).Min();
		var coverageDetails =
			$"label=[{string.Join(", ", labelEdgeCoverage.Select(value => value.ToString("P1")))}], " +
			$"image=[{string.Join(", ", imageEdgeCoverage.Select(value => value.ToString("P1")))}], " +
			$"expected minimum={minimumCoverage:P0}, " +
			$"samples=156 per edge, " +
			$"label frame={borderRects["TriangleLabelBorder"]}, image frame={borderRects["TriangleImageBorder"]}";

		Assert.That(minimumObservedCoverage, Is.GreaterThanOrEqualTo(minimumCoverage),
			$"Polygon Border inner clip covered its stroke: {coverageDetails}");
	}

	static double[] MeasurePolygonStrokeCoverage(
		PixelImage image,
		Rectangle frame,
		double scaleX,
		double scaleY)
	{
		(double X, double Y)[] vertices = [(40, 10), (70, 80), (10, 50)];
		var results = new double[3];

		for (var edgeIndex = 0; edgeIndex < vertices.Length; edgeIndex++)
		{
			var start = vertices[edgeIndex];
			var end = vertices[(edgeIndex + 1) % vertices.Length];
			var dx = end.X - start.X;
			var dy = end.Y - start.Y;
			var length = Math.Sqrt((dx * dx) + (dy * dy));
			var inwardX = -dy / length;
			var inwardY = dx / length;
			var green = 0;
			var samples = 0;

			for (var position = 0.18; position <= 0.82; position += 0.025)
			{
				for (var inset = 0.75; inset <= 3.25; inset += 0.5)
				{
					var x = start.X + (dx * position) + (inwardX * inset);
					var y = start.Y + (dy * position) + (inwardY * inset);
					if (IsLightGreen(image.GetScreenPixel(frame.X + x, frame.Y + y, scaleX, scaleY)))
						green++;
					samples++;
				}
			}

			Assert.That(samples, Is.GreaterThan(100), $"Polygon edge {edgeIndex} should have dense in-bounds samples.");
			results[edgeIndex] = green / (double)samples;
		}

		return results;
	}

	static double MeasureEllipseStrokeCoverage(
		PixelImage image,
		Rectangle frame,
		double scaleX,
		double scaleY)
	{
		var green = 0;
		var samples = 0;
		for (var degrees = 0; degrees < 360; degrees += 4)
		{
			var radians = degrees * Math.PI / 180;
			for (var inset = 1.0; inset <= 3.5; inset += 0.5)
			{
				var radius = 49.5 - inset;
				var x = 50.5 + (Math.Cos(radians) * radius);
				var y = 50.5 + (Math.Sin(radians) * radius);
				if (IsLightGreen(image.GetScreenPixel(frame.X + x, frame.Y + y, scaleX, scaleY)))
					green++;
				samples++;
			}
		}

		return green / (double)samples;
	}

	static double MeasureRoundRectangleStrokeCoverage(
		PixelImage image,
		Rectangle frame,
		double scaleX,
		double scaleY)
	{
		var green = 0;
		var samples = 0;
		for (var along = 15.0; along <= 86.0; along += 1.0)
		{
			for (var inset = 1.0; inset <= 3.5; inset += 0.5)
			{
				(double X, double Y)[] points =
				[
					(along, inset),
					(along, 101 - inset),
					(inset, along),
					(101 - inset, along)
				];

				foreach (var point in points)
				{
					if (IsLightGreen(image.GetScreenPixel(frame.X + point.X, frame.Y + point.Y, scaleX, scaleY)))
						green++;
					samples++;
				}
			}
		}

		return green / (double)samples;
	}

	static int CountPixels(
		PixelImage image,
		Rectangle frame,
		double scaleX,
		double scaleY,
		int left,
		int top,
		int right,
		int bottom,
		Func<PixelColor, bool> predicate)
	{
		var count = 0;
		for (var y = top; y <= bottom; y += 2)
		{
			for (var x = left; x <= right; x += 2)
			{
				if (predicate(image.GetScreenPixel(frame.X + x, frame.Y + y, scaleX, scaleY)))
					count++;
			}
		}

		return count;
	}

	static HashSet<int> CollectImageColors(
		PixelImage image,
		Rectangle frame,
		double scaleX,
		double scaleY)
	{
		var colors = new HashSet<int>();
		for (var y = 25; y <= 65; y += 3)
		{
			for (var x = 22; x <= 58; x += 3)
			{
				var color = image.GetScreenPixel(frame.X + x, frame.Y + y, scaleX, scaleY);
				if (!IsLightGreen(color) && !IsLightBlue(color))
					colors.Add(((color.Red / 16) << 8) | ((color.Green / 16) << 4) | (color.Blue / 16));
			}
		}

		return colors;
	}

	static bool IsLightGreen(PixelColor color) =>
		Math.Abs(color.Red - 144) <= 35 &&
		Math.Abs(color.Green - 238) <= 35 &&
		Math.Abs(color.Blue - 144) <= 35;

	static bool IsLightBlue(PixelColor color) =>
		Math.Abs(color.Red - 173) <= 25 &&
		Math.Abs(color.Green - 216) <= 25 &&
		Math.Abs(color.Blue - 230) <= 25;

	readonly record struct PixelColor(byte Red, byte Green, byte Blue);

	readonly record struct PixelImage(int Width, int Height, byte[] Rgba)
	{
		public static PixelImage Decode(byte[] png)
		{
			using var image = new MagickImage(png);
			var rgba = image.ToByteArray(MagickFormat.Rgba);
			Assert.That(rgba.Length, Is.EqualTo((long)image.Width * image.Height * 4),
				"The decoded screenshot should contain four bytes per pixel.");
			return new PixelImage((int)image.Width, (int)image.Height, rgba);
		}

		public PixelColor GetScreenPixel(double screenX, double screenY, double scaleX, double scaleY)
		{
			var x = (int)Math.Round(screenX * scaleX);
			var y = (int)Math.Round(screenY * scaleY);
			Assert.That(x, Is.InRange(0, Width - 1), $"Sample X={x} should be inside screenshot width {Width}.");
			Assert.That(y, Is.InRange(0, Height - 1), $"Sample Y={y} should be inside screenshot height {Height}.");
			var offset = ((y * Width) + x) * 4;
			return new PixelColor(Rgba[offset], Rgba[offset + 1], Rgba[offset + 2]);
		}
	}
}
#endif

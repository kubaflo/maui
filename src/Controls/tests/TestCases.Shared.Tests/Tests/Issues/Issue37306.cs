#if ANDROID
using System.Globalization;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37306 : _IssuesUITest
{
	public override string Issue => "ScrollView content is clipped at the Android bottom safe-area inset";

	public Issue37306(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ContentRendersThroughBottomSafeAreaWhileScrolling()
	{
		App.SetOrientationPortrait();
		Assert.That(
			App.GetOrientation().ToString(),
			Is.EqualTo("Portrait").IgnoreCase,
			"Issue 37306 requires the trusted runner to provide portrait orientation.");

		App.WaitForElement("Issue37306ScrollView");

		DragToMiddle();
		App.WaitForElement(
			AppiumQuery.ByXPath("//*[contains(@content-desc,'generation=reported;state=scrolled')]"),
			timeout: TimeSpan.FromSeconds(10));

		var reported = CaptureStableState("reported");
		ValidateState(reported.Measurement, reported.Width, reported.Height);
		var (reportedWhiteRatio, reportedSampleCount) = GetWhiteRatio(reported);

		Assert.That(
			reportedWhiteRatio,
			Is.GreaterThanOrEqualTo(0.8),
			$"Issue 37306: white item pixels were clipped from the Android bottom safe-area band while mid-scroll. Inset={reported.Measurement.Inset}, item={reported.Measurement.Item}, itemFrame=({reported.Measurement.ItemX},{reported.Measurement.ItemY},{reported.Measurement.ItemWidth},{reported.Measurement.ItemHeight}), scrollFrame=({reported.Measurement.ScrollX},{reported.Measurement.ScrollY},{reported.Measurement.ScrollWidth},{reported.Measurement.ScrollHeight}), offset={reported.Measurement.Offset:F2}/{reported.Measurement.MaximumOffset:F2}, samples={reportedSampleCount}, whiteRatio={reportedWhiteRatio:F3}, expectedRatio=0.800, tolerance=24.");
	}

	void DragToMiddle()
	{
		var scrollRect = App.WaitForElement("Issue37306ScrollView").GetRect();
		var centerX = scrollRect.X + (scrollRect.Width / 2);
		var startY = scrollRect.Y + (scrollRect.Height * 0.75f);
		var endY = scrollRect.Y + (scrollRect.Height * 0.25f);
		Assert.That(startY - endY, Is.GreaterThan(100), "The ScrollView was too short for the reported finger drag.");
		App.DragCoordinates(centerX, startY, centerX, endY);
	}

	(Measurement Measurement, byte[] Screenshot, int Width, int Height) CaptureStableState(string generation)
	{
		for (var attempt = 0; attempt < 10; attempt++)
		{
			var before = ReadMeasurement();
			var screenshot = App.Screenshot();
			Assert.That(screenshot, Is.Not.Null);
			if (screenshot is null)
				throw new InvalidOperationException("Appium returned no screenshot.");

			var after = ReadMeasurement();
			if (before.Generation == generation &&
				after.Generation == generation &&
				before.State == "scrolled" &&
				after.State == "scrolled" &&
				before.Item == after.Item &&
				Math.Abs(before.Offset - after.Offset) < 0.5)
			{
				using var image = new MagickImage(screenshot);
				return (after, screenshot, (int)image.Width, (int)image.Height);
			}
		}

		throw new InvalidOperationException($"The {generation} ScrollView did not settle during bounded screenshot capture.");
	}

	Measurement ReadMeasurement()
	{
		var element = App.WaitForElement("Issue37306ScrollView");
		var text = element.GetAttribute<string>("content-desc");
		Assert.That(text, Is.Not.Null);
		if (text is null)
			throw new InvalidOperationException("The native measurement description was unavailable.");

		var values = text
			.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(part => part.Split('=', 2))
			.Where(part => part.Length == 2)
			.ToDictionary(part => part[0], part => part[1], StringComparer.Ordinal);

		return new Measurement(
			GetString(values, "generation"),
			GetString(values, "state"),
			GetString(values, "edge"),
			GetInt(values, "count"),
			GetInt(values, "callback"),
			GetDouble(values, "offset"),
			GetDouble(values, "max"),
			GetInt(values, "inset"),
			GetDouble(values, "density"),
			GetInt(values, "screen"),
			GetInt(values, "item"),
			GetInt(values, "ix"),
			GetInt(values, "iy"),
			GetInt(values, "iw"),
			GetInt(values, "ih"),
			GetInt(values, "sx"),
			GetInt(values, "sy"),
			GetInt(values, "sw"),
			GetInt(values, "sh"),
			GetInt(values, "spb"));
	}

	static void ValidateState(Measurement measurement, int screenshotWidth, int screenshotHeight)
	{
		Assert.Multiple(() =>
		{
			Assert.That(measurement.Inset, Is.GreaterThan(0), "The trusted Android device must provide a real nonzero bottom system-bar inset.");
			Assert.That(measurement.Count, Is.EqualTo(30), "The reported hierarchy must contain all 30 items.");
			Assert.That(
				measurement.Edge,
				Is.EqualTo("Default"),
				"The reported ScrollView must retain its default SafeAreaEdges.");
			Assert.That(measurement.CallbackCount, Is.GreaterThan(0), "A post-drag Scrolled callback must occur.");
			Assert.That(measurement.Offset, Is.GreaterThan(0), "The drag must move the ScrollView from its initial offset.");
			Assert.That(measurement.Offset, Is.LessThan(measurement.MaximumOffset), "The drag must stop before the end of the list.");
			Assert.That(measurement.Item, Is.InRange(0, 29), "A known item must intersect the bottom inset band.");
			Assert.That(measurement.ItemHeight / measurement.Density, Is.EqualTo(56).Within(1), "The intersecting item must retain its reported 56-DIP height.");
			Assert.That(
				measurement.ScrollPaddingBottom,
				Is.EqualTo(measurement.Inset),
				"The real root inset must propagate as native ScrollView bottom padding.");
			Assert.That(measurement.ScreenHeight, Is.EqualTo(screenshotHeight).Within(2), "Native screen and screenshot heights must use the same coordinate space.");
			Assert.That(measurement.ScrollX, Is.GreaterThanOrEqualTo(0));
			Assert.That(measurement.ScrollY, Is.GreaterThanOrEqualTo(0));
			Assert.That(measurement.ScrollX + measurement.ScrollWidth, Is.LessThanOrEqualTo(screenshotWidth + 2));
			Assert.That(measurement.ScrollY + measurement.ScrollHeight, Is.GreaterThan(screenshotHeight - measurement.Inset), "The ScrollView must extend into the bottom inset band.");
		});

	}

	static (double Ratio, int SampleCount) GetWhiteRatio(
		(Measurement Measurement, byte[] Screenshot, int Width, int Height) captured)
	{
		var measurement = captured.Measurement;
		var bandTop = captured.Height - measurement.Inset;
		var sampleTop = Math.Max(bandTop + 2, measurement.ItemY + 2);
		var sampleBottom = Math.Min(captured.Height - 3, measurement.ItemY + measurement.ItemHeight - 3);
		Assert.That(sampleBottom, Is.GreaterThan(sampleTop), "The identified item must have a measurable intersection with the bottom inset band.");

		var insetFromEdge = Math.Max(4, measurement.ItemWidth / 8);
		var sampleXs = new[]
		{
			measurement.ItemX + insetFromEdge,
			measurement.ItemX + measurement.ItemWidth - insetFromEdge - 1
		};

		var white = 0;
		var samples = 0;
		for (var row = 0; row < 5; row++)
		{
			var y = sampleTop + ((sampleBottom - sampleTop) * row / 4);
			foreach (var x in sampleXs)
			{
				Assert.That(x, Is.InRange(0, captured.Width - 1));
				Assert.That(y, Is.InRange(0, captured.Height - 1));
				Assert.That(x, Is.InRange(measurement.ScrollX, measurement.ScrollX + measurement.ScrollWidth - 1));
				Assert.That(y, Is.InRange(measurement.ScrollY, measurement.ScrollY + measurement.ScrollHeight - 1));
				Assert.That(x, Is.InRange(measurement.ItemX, measurement.ItemX + measurement.ItemWidth - 1));
				Assert.That(y, Is.InRange(measurement.ItemY, measurement.ItemY + measurement.ItemHeight - 1));

				var color = ReadPixel(captured.Screenshot, x, y);
				if (Math.Abs(color.Red - 255) <= 24 &&
					Math.Abs(color.Green - 255) <= 24 &&
					Math.Abs(color.Blue - 255) <= 24)
				{
					white++;
				}

				samples++;
			}
		}

		return ((double)white / samples, samples);
	}

	static PixelColor ReadPixel(byte[] screenshot, int x, int y)
	{
		using var pixelImage = new MagickImage(screenshot);
		pixelImage.Crop(new MagickGeometry(x, y, 1, 1));
		pixelImage.ResetPage();
		var rgba = pixelImage.ToByteArray(MagickFormat.Rgba);
		if (rgba.Length < 3)
			throw new InvalidOperationException("ImageMagick did not return an RGBA pixel.");

		return new PixelColor(rgba[0], rgba[1], rgba[2]);
	}

	static string GetString(IReadOnlyDictionary<string, string> values, string key)
	{
		if (!values.TryGetValue(key, out var value))
			throw new InvalidOperationException($"Native measurement omitted '{key}'.");

		return value;
	}

	static int GetInt(IReadOnlyDictionary<string, string> values, string key) =>
		int.Parse(GetString(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture);

	static double GetDouble(IReadOnlyDictionary<string, string> values, string key) =>
		double.Parse(GetString(values, key), NumberStyles.Float, CultureInfo.InvariantCulture);

	readonly record struct Measurement(
		string Generation,
		string State,
		string Edge,
		int Count,
		int CallbackCount,
		double Offset,
		double MaximumOffset,
		int Inset,
		double Density,
		int ScreenHeight,
		int Item,
		int ItemX,
		int ItemY,
		int ItemWidth,
		int ItemHeight,
		int ScrollX,
		int ScrollY,
		int ScrollWidth,
		int ScrollHeight,
		int ScrollPaddingBottom);

	readonly record struct PixelColor(byte Red, byte Green, byte Blue);
}
#endif

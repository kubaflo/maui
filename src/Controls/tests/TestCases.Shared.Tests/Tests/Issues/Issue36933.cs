#if IOS
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36933 : _IssuesUITest
{
	const int ChannelTolerance = 12;

	public override string Issue => "DatePicker and TimePicker backgrounds are not cleared when set to null at runtime";

	public Issue36933(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.DatePicker)]
	public void PickerBackgroundsReturnToDefaultAfterBeingCleared()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"StateLabel",
				"Transition 0: default backgrounds are visible"),
			Is.True,
			"The picker page did not reach its initial state.");

		var datePickerRect = App.WaitForElement("AffectedDatePicker").GetRect();
		var timePickerRect = App.WaitForElement("AffectedTimePicker").GetRect();
		Assert.That(datePickerRect.Width, Is.GreaterThan(0), "The DatePicker must be visible.");
		Assert.That(datePickerRect.Height, Is.GreaterThan(0), "The DatePicker must be visible.");
		Assert.That(timePickerRect.Width, Is.GreaterThan(0), "The TimePicker must be visible.");
		Assert.That(timePickerRect.Height, Is.GreaterThan(0), "The TimePicker must be visible.");

		var initialScreenshot = CaptureScreenshot();
		var datePickerPoints = GetSamplePoints(datePickerRect);
		var timePickerPoints = GetSamplePoints(timePickerRect);
		var initialDateColors = Sample(initialScreenshot, datePickerPoints);
		var initialTimeColors = Sample(initialScreenshot, timePickerPoints);
		initialScreenshot.Image.Dispose();
		AssertNotRed("DatePicker", initialDateColors);
		AssertNotRed("TimePicker", initialTimeColors);

		App.Tap("ToggleBackgroundButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"StateLabel",
				"Transition 1: DatePicker Background=Red; TimePicker Background=Red"),
			Is.True,
			"The first tap did not complete the red Background assignments.");

		var datePickerTurnedRed = WaitForRedTransition(initialDateColors, datePickerPoints, out var redDateColors);
		var timePickerTurnedRed = WaitForRedTransition(initialTimeColors, timePickerPoints, out var redTimeColors);
		Assert.That(datePickerTurnedRed, Is.True, $"DatePicker did not render the applied red Background. Initial={Format(initialDateColors)}; actual={Format(redDateColors)}");
		Assert.That(timePickerTurnedRed, Is.True, $"TimePicker did not render the applied red Background. Initial={Format(initialTimeColors)}; actual={Format(redTimeColors)}");
		AssertRedTransition("DatePicker", initialDateColors, redDateColors);
		AssertRedTransition("TimePicker", initialTimeColors, redTimeColors);

		App.Tap("ToggleBackgroundButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"StateLabel",
				"Transition 2: DatePicker Background=null; TimePicker Background=null"),
			Is.True,
			"The second tap did not complete the null Background assignments.");

		var datePickerCleared = WaitForColors(initialDateColors, datePickerPoints, out var finalDateColors);
		var timePickerCleared = WaitForColors(initialTimeColors, timePickerPoints, out var finalTimeColors);

		Assert.Multiple(() =>
		{
			Assert.That(
				datePickerCleared,
				Is.True,
				$"DatePicker rendered background remained red after Background was set to null. Initial={Format(initialDateColors)}; red={Format(redDateColors)}; final={Format(finalDateColors)}");
			Assert.That(
				timePickerCleared,
				Is.True,
				$"TimePicker rendered background remained red after Background was set to null. Initial={Format(initialTimeColors)}; red={Format(redTimeColors)}; final={Format(finalTimeColors)}");
		});
	}

	static Point[] GetSamplePoints(Rectangle rect)
	{
		return
		[
			new(rect.X + (rect.Width / 4), rect.Y + (rect.Height * 3 / 4)),
			new(rect.X + (rect.Width * 3 / 4), rect.Y + (rect.Height * 3 / 4)),
		];
	}

	ScreenshotData CaptureScreenshot()
	{
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var image = new MagickImage(App.Screenshot());
		return new ScreenshotData(image, image.Width / (double)windowSize.Width, image.Height / (double)windowSize.Height);
	}

	static Rgb[] Sample(ScreenshotData screenshot, Point[] points)
	{
		var colors = new Rgb[points.Length];
		using var pixels = screenshot.Image.GetPixels();

		for (var i = 0; i < points.Length; i++)
		{
			var x = (int)Math.Round(points[i].X * screenshot.ScaleX);
			var y = (int)Math.Round(points[i].Y * screenshot.ScaleY);
			Assert.That(x, Is.InRange(0, (int)screenshot.Image.Width - 1), "Sample x-coordinate must be inside the screenshot.");
			Assert.That(y, Is.InRange(0, (int)screenshot.Image.Height - 1), "Sample y-coordinate must be inside the screenshot.");

			var color = pixels.GetPixel(x, y).ToColor()
				?? throw new InvalidOperationException($"Screenshot pixel ({x}, {y}) did not contain color data.");
			colors[i] = new Rgb(ToByte(color.R), ToByte(color.G), ToByte(color.B));
		}

		return colors;
	}

	static byte ToByte(byte channel) => channel;

	static void AssertRedTransition(string picker, Rgb[] initial, Rgb[] red)
	{
		for (var i = 0; i < red.Length; i++)
		{
			Assert.That(
				Distance(initial[i], red[i]),
				Is.GreaterThan(ChannelTolerance),
				$"{picker} sample {i} did not visibly change when the red Background was applied.");
			Assert.That(
				red[i].R - Math.Max(red[i].G, red[i].B),
				Is.GreaterThan(ChannelTolerance),
				$"{picker} sample {i} did not render the applied red Background.");
		}
	}

	static void AssertNotRed(string picker, Rgb[] colors)
	{
		for (var i = 0; i < colors.Length; i++)
		{
			Assert.That(
				colors[i].R - Math.Max(colors[i].G, colors[i].B),
				Is.LessThanOrEqualTo(ChannelTolerance),
				$"{picker} sample {i} must start with the clean default background.");
		}
	}

	bool WaitForRedTransition(Rgb[] initial, Point[] points, out Rgb[] actual)
	{
		var stopwatch = Stopwatch.StartNew();
		do
		{
			actual = CaptureColors(points);
			if (IsRedTransition(initial, actual))
				return true;
		}
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3));

		return false;
	}

	bool WaitForColors(Rgb[] expected, Point[] points, out Rgb[] actual)
	{
		var stopwatch = Stopwatch.StartNew();
		do
		{
			actual = CaptureColors(points);
			if (ColorsMatch(expected, actual))
				return true;
		}
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3));

		return false;
	}

	Rgb[] CaptureColors(Point[] points)
	{
		var screenshot = CaptureScreenshot();
		var colors = Sample(screenshot, points);
		screenshot.Image.Dispose();
		return colors;
	}

	static bool IsRedTransition(Rgb[] initial, Rgb[] actual)
	{
		for (var i = 0; i < initial.Length; i++)
		{
			if (Distance(initial[i], actual[i]) <= ChannelTolerance ||
				actual[i].R - Math.Max(actual[i].G, actual[i].B) <= ChannelTolerance)
				return false;
		}

		return true;
	}

	static bool ColorsMatch(Rgb[] expected, Rgb[] actual)
	{
		for (var i = 0; i < expected.Length; i++)
		{
			if (Distance(expected[i], actual[i]) > ChannelTolerance)
				return false;
		}

		return true;
	}

	static int Distance(Rgb first, Rgb second)
	{
		return Math.Max(
			Math.Abs(first.R - second.R),
			Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));
	}

	static string Format(Rgb[] colors) => string.Join(", ", colors.Select(color => $"({color.R},{color.G},{color.B})"));

	readonly record struct ScreenshotData(MagickImage Image, double ScaleX, double ScaleY);

	readonly record struct Rgb(byte R, byte G, byte B);
}
#endif

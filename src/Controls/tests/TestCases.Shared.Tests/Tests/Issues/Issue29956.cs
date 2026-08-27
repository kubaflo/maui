#if ANDROID
using System.Globalization;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29956 : _IssuesUITest
{
	public Issue29956(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "ImageButton border is clipped when AspectFill is selected";

	[Test]
	[Category(UITestCategories.ImageButton)]
	public void AspectFillKeepsFileImageButtonBorderOnEveryEdge()
	{
		App.SetOrientationPortrait();

		var imageElement = App.WaitForElement("AffectedImageButton");
		if (imageElement is null)
			throw new InvalidOperationException("The affected ImageButton was not found.");

		Assert.That(
			App.WaitForTextToBePresentInElement("CaptureToken", "0"),
			Is.True,
			"The file-backed ImageButton did not complete its initial render.");

		var initialFrame = imageElement.GetRect();
		var appiumApp = (AppiumApp)App;
		var viewport = appiumApp.Driver.Manage().Window.Size;
		Assert.That(viewport.Height, Is.GreaterThan(viewport.Width), "The test requires the reported portrait geometry.");

		var densityCapability = appiumApp.Driver.Capabilities.GetCapability("deviceScreenDensity");
		if (densityCapability is null)
			throw new InvalidOperationException("The Android deviceScreenDensity capability is required.");

		var density = Convert.ToDouble(densityCapability, CultureInfo.InvariantCulture) / 160d;
		var borderThickness = Math.Max(2, (int)Math.Round(8 * density));
		Assert.That(initialFrame.Width, Is.EqualTo((int)Math.Round(260 * density)).Within(2), "The issue-derived ImageButton width was not arranged.");
		Assert.That(initialFrame.Height, Is.EqualTo((int)Math.Round(180 * density)).Within(2), "The issue-derived ImageButton height was not arranged.");

		var initialCapture = App.Screenshot();
		if (initialCapture is null)
			throw new InvalidOperationException("The initial Android screenshot was not captured.");

		using var initialImage = new MagickImage(initialCapture);
		var initialCoverage = MeasureBorder(initialImage, initialFrame, borderThickness);
		Assert.That(initialCoverage.InteriorColors, Is.GreaterThanOrEqualTo(4), "The expected dotnet_bot image content was not rendered.");
		Assert.That(initialCoverage.Left, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFit left border missing: measured {initialCoverage.Left:P0}, required 70%.");
		Assert.That(initialCoverage.Top, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFit top border missing: measured {initialCoverage.Top:P0}, required 70%.");
		Assert.That(initialCoverage.Right, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFit right border missing: measured {initialCoverage.Right:P0}, required 70%.");
		Assert.That(initialCoverage.Bottom, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFit bottom border missing: measured {initialCoverage.Bottom:P0}, required 70%.");

		App.Tap("AspectFillRadioButton");
		Assert.That(App.WaitForTextToBePresentInElement("AspectState", "Current aspect: AspectFill"), Is.True, "AspectFill was not selected.");
		Assert.That(App.WaitForTextToBePresentInElement("TransitionCount", "1"), Is.True, "CheckedChanged did not run exactly once.");
		Assert.That(App.WaitForTextToBePresentInElement("CaptureToken", "1"), Is.True, "The post-trigger render did not complete.");

		var postTriggerElement = App.WaitForElement("AffectedImageButton");
		if (postTriggerElement is null)
			throw new InvalidOperationException("The affected ImageButton disappeared after selecting AspectFill.");

		var postTriggerFrame = postTriggerElement.GetRect();
		Assert.That(postTriggerFrame, Is.EqualTo(initialFrame), "The same ImageButton frame must remain arranged after the aspect change.");

		var postTriggerCapture = App.Screenshot();
		if (postTriggerCapture is null)
			throw new InvalidOperationException("The post-trigger Android screenshot was not captured.");

		using var postTriggerImage = new MagickImage(postTriggerCapture);
		var postTriggerCoverage = MeasureBorder(postTriggerImage, postTriggerFrame, borderThickness);
		Assert.That(postTriggerCoverage.InteriorColors, Is.GreaterThanOrEqualTo(4), "The file image content disappeared after selecting AspectFill.");

		App.Tap("RecordButton");
		Assert.That(App.WaitForTextToBePresentInElement("ResultStatus", "Recorded"), Is.True, "The record action did not observe the completed AspectFill transition.");

		Assert.That(postTriggerCoverage.Left, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFill left border missing: measured {postTriggerCoverage.Left:P0}, required 70%.");
		Assert.That(postTriggerCoverage.Top, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFill top border missing: measured {postTriggerCoverage.Top:P0}, required 70%.");
		Assert.That(postTriggerCoverage.Right, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFill right border missing: measured {postTriggerCoverage.Right:P0}, required 70%.");
		Assert.That(postTriggerCoverage.Bottom, Is.GreaterThanOrEqualTo(0.70), $"ImageButton AspectFill bottom border missing: measured {postTriggerCoverage.Bottom:P0}, required 70%.");
	}

	static (double Left, double Top, double Right, double Bottom, int InteriorColors) MeasureBorder(
		MagickImage image,
		System.Drawing.Rectangle frame,
		int borderThickness)
	{
		Assert.That(frame.Left, Is.GreaterThanOrEqualTo(0));
		Assert.That(frame.Top, Is.GreaterThanOrEqualTo(0));
		Assert.That(frame.Right, Is.LessThanOrEqualTo((int)image.Width));
		Assert.That(frame.Bottom, Is.LessThanOrEqualTo((int)image.Height));

		using var pixels = image.GetPixels();

		bool IsExpectedRed(int x, int y)
		{
			var pixel = pixels.GetPixel(x, y);
			if (pixel is null)
				throw new InvalidOperationException($"Screenshot pixel ({x}, {y}) was unavailable.");

			var color = pixel.ToColor();
			if (color is null)
				throw new InvalidOperationException($"Screenshot pixel ({x}, {y}) had no color.");

			var red = (int)color.R;
			var green = (int)color.G;
			var blue = (int)color.B;
			var alpha = (int)color.A;
			return alpha >= 220 && red >= 180 && green <= 100 && blue <= 100 && red - Math.Max(green, blue) >= 100;
		}

		double VerticalCoverage(int x)
		{
			var matches = 0;
			var samples = 0;
			for (var y = frame.Top + (2 * borderThickness); y < frame.Bottom - (2 * borderThickness); y += 2)
			{
				samples++;
				if (IsExpectedRed(x, y))
					matches++;
			}
			return (double)matches / samples;
		}

		double HorizontalCoverage(int y)
		{
			var matches = 0;
			var samples = 0;
			for (var x = frame.Left + (2 * borderThickness); x < frame.Right - (2 * borderThickness); x += 2)
			{
				samples++;
				if (IsExpectedRed(x, y))
					matches++;
			}
			return (double)matches / samples;
		}

		var interiorColors = new HashSet<int>();
		for (var y = frame.Top + (3 * borderThickness); y < frame.Bottom - (3 * borderThickness); y += borderThickness * 2)
		{
			for (var x = frame.Left + (3 * borderThickness); x < frame.Right - (3 * borderThickness); x += borderThickness * 2)
			{
				var pixel = pixels.GetPixel(x, y);
				if (pixel is null)
					throw new InvalidOperationException($"Screenshot pixel ({x}, {y}) was unavailable.");

				var color = pixel.ToColor();
				if (color is null)
					throw new InvalidOperationException($"Screenshot pixel ({x}, {y}) had no color.");

				var quantizedColor = (((int)color.R / 32) << 16) | (((int)color.G / 32) << 8) | ((int)color.B / 32);
				interiorColors.Add(quantizedColor);
			}
		}

		var inset = Math.Max(1, borderThickness / 2);
		return (
			VerticalCoverage(frame.Left + inset),
			HorizontalCoverage(frame.Top + inset),
			VerticalCoverage(frame.Right - 1 - inset),
			HorizontalCoverage(frame.Bottom - 1 - inset),
			interiorColors.Count);
	}
}
#endif

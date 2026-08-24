#if WINDOWS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public Issue34071(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell foreground color is not applied to ToolbarItems";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorColorsToolbarItemIcon()
	{
		var loadStatus = App.WaitForElement("LoadStatus").GetText();
		Assert.That(loadStatus, Is.Not.Null);
		Assert.That(loadStatus, Is.EqualTo("Loaded:Ready"));

		var referenceFrame = App.WaitForElement("ExpectedColorLabel").GetRect();
		var toolbarFrame = App.WaitForElement("AffectedToolbarItem").GetRect();
		Assert.That(referenceFrame.Width, Is.GreaterThan(0));
		Assert.That(referenceFrame.Height, Is.GreaterThan(0));
		Assert.That(toolbarFrame.Width, Is.GreaterThan(0));
		Assert.That(toolbarFrame.Height, Is.GreaterThan(0));

		PixelSample referenceSample = default;
		PixelSample toolbarSample = default;

		for (int attempt = 0; attempt < 5; attempt++)
		{
			using var image = new MagickImage(App.Screenshot());
			image.Depth = 8;
			var pixels = image.ToByteArray(MagickFormat.Rgba);
			var width = checked((int)image.Width);
			var height = checked((int)image.Height);

			AssertFrameIsInBounds(referenceFrame, width, height, "reference label");
			AssertFrameIsInBounds(toolbarFrame, width, height, "toolbar item");

			referenceSample = SampleFrame(pixels, width, referenceFrame);
			toolbarSample = SampleFrame(pixels, width, toolbarFrame);
			if (referenceSample.MagentaCount >= 8 && toolbarSample.MagentaCount >= 8)
				break;
		}

		Assert.That(referenceSample.MagentaCount, Is.GreaterThanOrEqualTo(8),
			$"Magenta reference rendered {referenceSample.MagentaCount} magenta pixels.");
		Assert.That(referenceSample.StrongestChannelSeparation, Is.GreaterThan(100),
			$"Magenta reference channel separation was {referenceSample.StrongestChannelSeparation}.");

		var requiredMagentaPixels = Math.Max(8, toolbarSample.NonBackgroundCount / 4);
		Assert.That(toolbarSample.MagentaCount, Is.GreaterThanOrEqualTo(requiredMagentaPixels),
			$"ToolbarItem rendered magenta pixel count was {toolbarSample.MagentaCount}; required {requiredMagentaPixels}. " +
			$"Observed {toolbarSample.NonBackgroundCount} non-background pixels averaging " +
			$"RGB({toolbarSample.AverageRed},{toolbarSample.AverageGreen},{toolbarSample.AverageBlue}); expected RGB(255,0,255).");
	}

	static void AssertFrameIsInBounds(System.Drawing.Rectangle frame, int width, int height, string description)
	{
		Assert.That(frame.X, Is.GreaterThanOrEqualTo(0), $"{description} frame starts outside the screenshot.");
		Assert.That(frame.Y, Is.GreaterThanOrEqualTo(0), $"{description} frame starts outside the screenshot.");
		Assert.That(frame.Right, Is.LessThanOrEqualTo(width), $"{description} frame exceeds screenshot width.");
		Assert.That(frame.Bottom, Is.LessThanOrEqualTo(height), $"{description} frame exceeds screenshot height.");
	}

	static PixelSample SampleFrame(byte[] pixels, int imageWidth, System.Drawing.Rectangle frame)
	{
		var inset = 2;
		var corners = new[]
		{
			ReadPixel(pixels, imageWidth, frame.Left + inset, frame.Top + inset),
			ReadPixel(pixels, imageWidth, frame.Right - inset - 1, frame.Top + inset),
			ReadPixel(pixels, imageWidth, frame.Left + inset, frame.Bottom - inset - 1),
			ReadPixel(pixels, imageWidth, frame.Right - inset - 1, frame.Bottom - inset - 1)
		};
		var background = (
			R: corners.Sum(pixel => pixel.R) / corners.Length,
			G: corners.Sum(pixel => pixel.G) / corners.Length,
			B: corners.Sum(pixel => pixel.B) / corners.Length);

		var magentaCount = 0;
		var nonBackgroundCount = 0;
		var strongestChannelSeparation = 0;
		var redTotal = 0;
		var greenTotal = 0;
		var blueTotal = 0;

		for (int y = frame.Top; y < frame.Bottom; y++)
		{
			for (int x = frame.Left; x < frame.Right; x++)
			{
				var pixel = ReadPixel(pixels, imageWidth, x, y);
				var backgroundDistance =
					Math.Abs(pixel.R - background.R) +
					Math.Abs(pixel.G - background.G) +
					Math.Abs(pixel.B - background.B);
				if (backgroundDistance > 45)
				{
					nonBackgroundCount++;
					redTotal += pixel.R;
					greenTotal += pixel.G;
					blueTotal += pixel.B;
				}

				var channelSeparation = Math.Min(pixel.R - pixel.G, pixel.B - pixel.G);
				if (pixel.R >= 185 && pixel.G <= 100 && pixel.B >= 185 && channelSeparation > 100)
				{
					magentaCount++;
					strongestChannelSeparation = Math.Max(strongestChannelSeparation, channelSeparation);
				}
			}
		}

		return new PixelSample(
			magentaCount,
			nonBackgroundCount,
			strongestChannelSeparation,
			nonBackgroundCount == 0 ? 0 : redTotal / nonBackgroundCount,
			nonBackgroundCount == 0 ? 0 : greenTotal / nonBackgroundCount,
			nonBackgroundCount == 0 ? 0 : blueTotal / nonBackgroundCount);
	}

	static (int R, int G, int B) ReadPixel(byte[] pixels, int imageWidth, int x, int y)
	{
		var offset = checked(((y * imageWidth) + x) * 4);
		return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
	}

	readonly record struct PixelSample(
		int MagentaCount,
		int NonBackgroundCount,
		int StrongestChannelSeparation,
		int AverageRed,
		int AverageGreen,
		int AverageBlue);
}
#endif

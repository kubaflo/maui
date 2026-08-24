#if WINDOWS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public override string Issue => "Shell foreground color is not applied to ToolbarItems";

	public Issue34071(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void ToolbarIconUsesShellForegroundColor()
	{
		var loadedStatus = App.WaitForElement("Issue34071LoadedStatus");
		Assert.That(loadedStatus.GetText(), Is.EqualTo("1"), "The ContentPage Loaded transition should complete before inspecting the toolbar.");

		var toolbarItem = App.WaitForElement("AffectedToolbarItem");
		var referenceLabel = App.WaitForElement("Issue34071PurpleReferenceLabel");
		Assert.That(toolbarItem.GetText(), Is.EqualTo("Calculator"), "The measured toolbar item should be the reported Calculator item.");

		var toolbarRect = toolbarItem.GetRect();
		var referenceLabelRect = referenceLabel.GetRect();
		const int swatchSize = 24;
		const int swatchSpacing = 10;
		var swatchRect = new System.Drawing.Rectangle(
			referenceLabelRect.X - swatchSpacing - swatchSize,
			referenceLabelRect.Y + (referenceLabelRect.Height - swatchSize) / 2,
			swatchSize,
			swatchSize);
		Assert.Multiple(() =>
		{
			Assert.That(toolbarRect.Width, Is.GreaterThan(0), "The Calculator toolbar item should have a nonempty rendered width.");
			Assert.That(toolbarRect.Height, Is.GreaterThan(0), "The Calculator toolbar item should have a nonempty rendered height.");
			Assert.That(referenceLabelRect.Width, Is.GreaterThan(0), "The Purple reference label should have a nonempty rendered width.");
			Assert.That(referenceLabelRect.Height, Is.GreaterThan(0), "The Purple reference label should have a nonempty rendered height.");
			Assert.That(swatchRect.Width, Is.GreaterThan(0), "The Purple reference swatch should have a nonempty rendered width.");
			Assert.That(swatchRect.Height, Is.GreaterThan(0), "The Purple reference swatch should have a nonempty rendered height.");
		});

		var screenshotBytes = App.Screenshot();
		using var screenshot = new MagickImage(screenshotBytes);
		using var pixels = screenshot.GetPixels();
		int imageWidth = checked((int)screenshot.Width);
		int imageHeight = checked((int)screenshot.Height);

		AssertRectInsideImage(swatchRect, imageWidth, imageHeight, "Purple reference swatch");
		AssertRectInsideImage(toolbarRect, imageWidth, imageHeight, "Calculator toolbar item");

		(byte R, byte G, byte B) ReadPixel(int x, int y)
		{
			var color = pixels.GetPixel(x, y).ToColor();
			if (color is null)
				throw new AssertionException($"ImageMagick did not return a color for screenshot pixel ({x}, {y}).");

			return (color.R, color.G, color.B);
		}

		var swatchPixels = new List<(byte R, byte G, byte B)>();
		int swatchLeft = swatchRect.X + swatchRect.Width / 4;
		int swatchTop = swatchRect.Y + swatchRect.Height / 4;
		int swatchRight = swatchRect.Right - swatchRect.Width / 4;
		int swatchBottom = swatchRect.Bottom - swatchRect.Height / 4;
		for (int y = swatchTop; y < swatchBottom; y += 2)
		{
			for (int x = swatchLeft; x < swatchRight; x += 2)
				swatchPixels.Add(ReadPixel(x, y));
		}

		Assert.That(swatchPixels.Count, Is.GreaterThan(8), "The displayed Purple reference swatch should provide a dense central sample.");
		var expected = MedianColor(swatchPixels);
		Assert.Multiple(() =>
		{
			Assert.That(expected.R, Is.EqualTo(128).Within(12), "The displayed reference swatch should render the arranged Purple red channel.");
			Assert.That(expected.G, Is.EqualTo(0).Within(12), "The displayed reference swatch should render the arranged Purple green channel.");
			Assert.That(expected.B, Is.EqualTo(128).Within(12), "The displayed reference swatch should render the arranged Purple blue channel.");
		});

		var backgroundPixels = new List<(byte R, byte G, byte B)>();
		int edgeInset = Math.Max(1, Math.Min(toolbarRect.Width, toolbarRect.Height) / 12);
		for (int x = toolbarRect.X + edgeInset; x < toolbarRect.Right - edgeInset; x += 2)
		{
			backgroundPixels.Add(ReadPixel(x, toolbarRect.Y + edgeInset));
			backgroundPixels.Add(ReadPixel(x, toolbarRect.Bottom - edgeInset - 1));
		}

		for (int y = toolbarRect.Y + edgeInset; y < toolbarRect.Bottom - edgeInset; y += 2)
		{
			backgroundPixels.Add(ReadPixel(toolbarRect.X + edgeInset, y));
			backgroundPixels.Add(ReadPixel(toolbarRect.Right - edgeInset - 1, y));
		}

		Assert.That(backgroundPixels.Count, Is.GreaterThan(0), "The toolbar item should provide background samples around its rendered glyph.");
		var background = MedianColor(backgroundPixels);
		var glyphPixels = new List<(byte R, byte G, byte B)>();
		int glyphLeft = toolbarRect.X + toolbarRect.Width / 10;
		int glyphTop = toolbarRect.Y + toolbarRect.Height / 4;
		int glyphRight = toolbarRect.X + toolbarRect.Width / 3;
		int glyphBottom = toolbarRect.Bottom - toolbarRect.Height / 4;
		int sampledPixelCount = 0;

		for (int y = glyphTop; y < glyphBottom; y++)
		{
			for (int x = glyphLeft; x < glyphRight; x++)
			{
				sampledPixelCount++;
				var pixel = ReadPixel(x, y);
				if (MaximumChannelDifference(pixel, background) >= 24)
					glyphPixels.Add(pixel);
			}
		}

		Assert.Multiple(() =>
		{
			Assert.That(glyphPixels.Count, Is.GreaterThan(8), "The shopping-cart glyph should have a bounded nonzero foreground-pixel population.");
			Assert.That(glyphPixels.Count, Is.LessThan(sampledPixelCount), "The glyph sample should also contain its surrounding background.");
		});

		var actual = MedianColor(glyphPixels);
		int matchingPixelCount = glyphPixels.Count(pixel => MaximumChannelDifference(pixel, expected) <= 32);
		int requiredMatchingPixelCount = Math.Max(4, glyphPixels.Count / 3);

		Assert.That(
			matchingPixelCount,
			Is.GreaterThanOrEqualTo(requiredMatchingPixelCount),
			$"Toolbar icon foreground should match the arranged Purple reference swatch. Expected RGB ({expected.R}, {expected.G}, {expected.B}); actual glyph median RGB ({actual.R}, {actual.G}, {actual.B}); matching pixels {matchingPixelCount} of {glyphPixels.Count} candidates.");
	}

	static void AssertRectInsideImage(System.Drawing.Rectangle rect, int imageWidth, int imageHeight, string elementName)
	{
		Assert.Multiple(() =>
		{
			Assert.That(rect.X, Is.GreaterThanOrEqualTo(0), $"{elementName} sample coordinates should start inside the screenshot.");
			Assert.That(rect.Y, Is.GreaterThanOrEqualTo(0), $"{elementName} sample coordinates should start inside the screenshot.");
			Assert.That(rect.Right, Is.LessThanOrEqualTo(imageWidth), $"{elementName} sample coordinates should end inside the screenshot.");
			Assert.That(rect.Bottom, Is.LessThanOrEqualTo(imageHeight), $"{elementName} sample coordinates should end inside the screenshot.");
		});
	}

	static (byte R, byte G, byte B) MedianColor(List<(byte R, byte G, byte B)> colors)
	{
		var red = colors.Select(color => color.R).Order().ToArray();
		var green = colors.Select(color => color.G).Order().ToArray();
		var blue = colors.Select(color => color.B).Order().ToArray();
		int middle = colors.Count / 2;
		return (red[middle], green[middle], blue[middle]);
	}

	static int MaximumChannelDifference((byte R, byte G, byte B) first, (byte R, byte G, byte B) second) =>
		Math.Max(Math.Abs(first.R - second.R), Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));
}
#endif

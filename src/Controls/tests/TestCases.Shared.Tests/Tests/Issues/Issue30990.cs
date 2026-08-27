#if ANDROID
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30990 : _IssuesUITest
{
	const int ColorTolerance = 90;
	const int MinimumForegroundPixels = 8;

	public Issue30990(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell toolbar ignores Shell.ForegroundColor";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ToolbarIconUsesShellForegroundColor()
	{
		App.SetOrientationPortrait();
		App.SetLightTheme();

		var textItem = App.WaitForElement(AppiumQuery.ByXPath("//*[@content-desc='Issue30990TextToolbarItem']"));
		var iconItem = App.WaitForElement(AppiumQuery.ByXPath("//*[@content-desc='Issue30990IconToolbarItem']"));
		var metadataElement = App.WaitForElement(AppiumQuery.ByXPath("//*[@text='Hello']"));

		var textBounds = textItem.GetRect();
		var iconBounds = iconItem.GetRect();
		var contentBounds = metadataElement.GetRect();
		var toolbarFramesReady = textBounds.Width > 0 && textBounds.Height > 0 &&
			iconBounds.Width > 0 && iconBounds.Height > 0;

		Assert.That(toolbarFramesReady, Is.True, "Both toolbar items should have nonempty native frames after Shell attachment.");
		Assert.That(textBounds.Y, Is.LessThan(contentBounds.Y), "The text item should be in the toolbar above the page content.");
		Assert.That(iconBounds.Y, Is.LessThan(contentBounds.Y), "The image item should be in the toolbar above the page content.");
		Assert.That(iconBounds.Width, Is.GreaterThanOrEqualTo(24), "The groceries icon toolbar item should occupy its normal minimum touch frame.");
		Assert.That(iconBounds.Height, Is.GreaterThanOrEqualTo(24), "The groceries icon toolbar item should occupy its normal minimum touch frame.");
		Assert.That(App.IsKeyboardShown(), Is.False, "The keyboard should remain closed.");

		var metadata = metadataElement.GetAttribute<string>("content-desc");
		Assert.That(metadata, Is.Not.Null);
		if (metadata is null)
		{
			Assert.Fail("Issue30990 automation metadata was not exposed.");
			return;
		}

		var expectedColor = ParseMetadata(metadata);
		Assert.That(expectedColor.Red, Is.EqualTo(255));
		Assert.That(expectedColor.Green, Is.EqualTo(0));
		Assert.That(expectedColor.Blue, Is.EqualTo(0));
		Assert.That(metadata, Does.Contain("Text=Text 1"));
		Assert.That(metadata, Does.Contain("Theme=Light"));

		var screenshotBytes = App.Screenshot();
		Assert.That(screenshotBytes, Is.Not.Null);
		if (screenshotBytes is null)
		{
			Assert.Fail("The rendered window screenshot was not captured.");
			return;
		}

		using var screenshot = new MagickImage(screenshotBytes);
		Assert.That(screenshot.Width, Is.LessThan(screenshot.Height), "The rendered window should be in portrait orientation.");

		var textPixels = ReadInsetPixels(screenshot, textBounds);
		var iconPixels = ReadInsetPixels(screenshot, iconBounds);
		var toolbarBackground = FindMostCommonColor(textPixels);
		var backgroundDistance = ColorDistance(toolbarBackground, expectedColor);
		var textForegroundPixels = textPixels.Count(pixel => ColorDistance(pixel, expectedColor) <= ColorTolerance);

		Assert.That(backgroundDistance, Is.GreaterThan(ColorTolerance), "The expected foreground color must differ from the toolbar background.");
		Assert.That(textForegroundPixels, Is.GreaterThanOrEqualTo(MinimumForegroundPixels),
			"The screenshot color oracle should detect Shell.ForegroundColor on the Text 1 toolbar item.");

		var iconForegroundPixels = iconPixels.Count(pixel => ColorDistance(pixel, expectedColor) <= ColorTolerance);
		Assert.That(iconForegroundPixels, Is.GreaterThanOrEqualTo(MinimumForegroundPixels),
			$"Issue30990 toolbar icon was not rendered with Shell.ForegroundColor. " +
			$"Observed {iconForegroundPixels} matching pixels out of {iconPixels.Count}; expected at least {MinimumForegroundPixels} " +
			$"for RGB({expectedColor.Red},{expectedColor.Green},{expectedColor.Blue}) within tolerance {ColorTolerance}; " +
			$"icon bounds were ({iconBounds.X},{iconBounds.Y},{iconBounds.Width},{iconBounds.Height}).");
	}

	static RgbColor ParseMetadata(string metadata)
	{
		const string Prefix = "Foreground=";
		var foregroundEntry = metadata.Split(';').Single(entry => entry.StartsWith(Prefix, StringComparison.Ordinal));
		var channels = foregroundEntry[Prefix.Length..].Split(',').Select(int.Parse).ToArray();

		Assert.That(channels, Has.Length.EqualTo(3), "Foreground metadata should contain three RGB channels.");
		return new RgbColor(channels[0], channels[1], channels[2]);
	}

	static List<RgbColor> ReadInsetPixels(MagickImage image, System.Drawing.Rectangle bounds)
	{
		const int Inset = 2;
		var left = bounds.X + Inset;
		var top = bounds.Y + Inset;
		var right = bounds.X + bounds.Width - Inset;
		var bottom = bounds.Y + bounds.Height - Inset;

		Assert.That(left, Is.GreaterThanOrEqualTo(0));
		Assert.That(top, Is.GreaterThanOrEqualTo(0));
		Assert.That(right, Is.LessThanOrEqualTo((int)image.Width));
		Assert.That(bottom, Is.LessThanOrEqualTo((int)image.Height));
		Assert.That(right, Is.GreaterThan(left));
		Assert.That(bottom, Is.GreaterThan(top));

		var result = new List<RgbColor>((right - left) * (bottom - top));
		using var pixels = image.GetPixels();

		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				var color = pixels.GetPixel(x, y).ToColor();
				if (color is null)
					throw new InvalidOperationException($"The rendered pixel at ({x},{y}) did not contain a color.");

				result.Add(new RgbColor(color.R, color.G, color.B));
			}
		}

		return result;
	}

	static RgbColor FindMostCommonColor(IEnumerable<RgbColor> pixels)
	{
		var mostCommonGroup = pixels.GroupBy(pixel => pixel).MaxBy(group => group.Count());
		if (mostCommonGroup is null)
			throw new InvalidOperationException("The toolbar sample did not contain any pixels.");

		return mostCommonGroup.Key;
	}

	static double ColorDistance(RgbColor first, RgbColor second)
	{
		var red = first.Red - second.Red;
		var green = first.Green - second.Green;
		var blue = first.Blue - second.Blue;
		return Math.Sqrt(red * red + green * green + blue * blue);
	}

	readonly record struct RgbColor(int Red, int Green, int Blue);
}
#endif

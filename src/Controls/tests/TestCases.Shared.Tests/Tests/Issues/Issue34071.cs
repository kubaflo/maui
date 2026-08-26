#if WINDOWS
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	const int ColorTolerance = 75;

	public Issue34071(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Shell foreground color is not applied to toolbar item icons";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ToolbarIconUsesShellForegroundColor()
	{
		var toolbarRect = App.WaitForElement("AffectedToolbarItem").GetRect();

		Assert.That(toolbarRect.Width, Is.GreaterThan(0), "The affected toolbar item should have a nonempty native rectangle.");
		Assert.That(toolbarRect.Height, Is.GreaterThan(0), "The affected toolbar item should have a nonempty native rectangle.");

		PixelCounts toolbarCounts = new(-1, -1, 0, 0, 0, 0, 0, 0);
		App.WaitForElement(() =>
		{
			using var screenshot = new MagickImage(App.Screenshot());
			toolbarCounts = CountGlyphPixels(screenshot, toolbarRect);
			return toolbarCounts.Eligible >= 5 ? App.FindElement("AffectedToolbarItem") : null;
		}, "Timed out waiting for the shopping-cart image to render.");

		Assert.That(toolbarCounts.Eligible, Is.GreaterThanOrEqualTo(5),
			$"The shopping-cart glyph should be present; eligible={toolbarCounts.Eligible}.");
		Assert.That(
			toolbarCounts.Fuchsia >= 5 && toolbarCounts.Fuchsia * 100 >= toolbarCounts.Eligible * 35,
			Is.True,
			$"Toolbar icon should render with Shell.ForegroundColor Fuchsia; {Describe(toolbarCounts)}; expected=#FF00FF, tolerance={ColorTolerance}.");
	}

	static PixelCounts CountGlyphPixels(MagickImage image, Rectangle nativeRect)
	{
		int imageWidth = checked((int)image.Width);
		int imageHeight = checked((int)image.Height);
		int left = Math.Clamp(nativeRect.Left, 0, imageWidth - 1);
		int top = Math.Clamp(nativeRect.Top, 0, imageHeight - 1);
		int right = Math.Clamp(nativeRect.Right, left + 1, imageWidth);
		int bottom = Math.Clamp(nativeRect.Bottom, top + 1, imageHeight);
		int horizontalInset = (right - left) / 5;
		int verticalInset = (bottom - top) / 5;
		left += horizontalInset;
		right -= horizontalInset;
		top += verticalInset;
		bottom -= verticalInset;

		byte[] rgba = image.ToByteArray(MagickFormat.Rgba);
		var background = AverageCorners(rgba, imageWidth, left, top, right, bottom);
		int eligible = 0;
		int fuchsia = 0;
		int minRed = 255;
		int maxRed = 0;
		int minGreen = 255;
		int maxGreen = 0;
		int minBlue = 255;
		int maxBlue = 0;

		for (int y = top; y < bottom; y++)
		{
			for (int x = left; x < right; x++)
			{
				int offset = ((y * imageWidth) + x) * 4;
				int red = rgba[offset];
				int green = rgba[offset + 1];
				int blue = rgba[offset + 2];
				int alpha = rgba[offset + 3];
				int difference = Math.Abs(red - background.Red) + Math.Abs(green - background.Green) + Math.Abs(blue - background.Blue);

				if (alpha < 64 || difference < 75)
					continue;

				eligible++;
				minRed = Math.Min(minRed, red);
				maxRed = Math.Max(maxRed, red);
				minGreen = Math.Min(minGreen, green);
				maxGreen = Math.Max(maxGreen, green);
				minBlue = Math.Min(minBlue, blue);
				maxBlue = Math.Max(maxBlue, blue);

				if (red >= 255 - ColorTolerance && green <= ColorTolerance && blue >= 255 - ColorTolerance)
					fuchsia++;
			}
		}

		return new PixelCounts(eligible, fuchsia, minRed, maxRed, minGreen, maxGreen, minBlue, maxBlue);
	}

	static (int Red, int Green, int Blue) AverageCorners(byte[] rgba, int imageWidth, int left, int top, int right, int bottom)
	{
		int[] offsets =
		{
			((top * imageWidth) + left) * 4,
			((top * imageWidth) + right - 1) * 4,
			(((bottom - 1) * imageWidth) + left) * 4,
			(((bottom - 1) * imageWidth) + right - 1) * 4
		};

		int red = 0;
		int green = 0;
		int blue = 0;

		foreach (int offset in offsets)
		{
			red += rgba[offset];
			green += rgba[offset + 1];
			blue += rgba[offset + 2];
		}

		return (red / offsets.Length, green / offsets.Length, blue / offsets.Length);
	}

	static string Describe(PixelCounts counts) =>
		$"matching={counts.Fuchsia}, eligible={counts.Eligible}, R={counts.MinRed}-{counts.MaxRed}, G={counts.MinGreen}-{counts.MaxGreen}, B={counts.MinBlue}-{counts.MaxBlue}";

	readonly record struct PixelCounts(
		int Eligible,
		int Fuchsia,
		int MinRed,
		int MaxRed,
		int MinGreen,
		int MaxGreen,
		int MinBlue,
		int MaxBlue);
}
#endif

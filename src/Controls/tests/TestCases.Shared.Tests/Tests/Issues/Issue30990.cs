#if ANDROID
using System.Drawing;
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30990 : _IssuesUITest
{
	public Issue30990(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Shell toolbar ignores Shell foreground color";

	[Test]
	[Category(UITestCategories.ToolbarItem)]
	public void ToolbarIconUsesShellForegroundColor()
	{
		Assert.That(App.WaitForTextToBePresentInElement("LoadedStatus", "2"), Is.True);

		var toolbarItemCount = App.WaitForElement("LoadedStatus").GetText();
		Assert.That(toolbarItemCount, Is.Not.Null);
		Assert.That(toolbarItemCount, Is.EqualTo("2"));

		var textAction = App.WaitForElement("Text 1");
		var textActionText = textAction.GetText();
		Assert.That(textActionText, Is.Not.Null);
		Assert.That(textActionText, Is.EqualTo("Text 1"));

		var iconAction = App.WaitForElement(AppiumQuery.ByAccessibilityId("ToolbarIcon"));
		var textRect = textAction.GetRect();
		var iconRect = iconAction.GetRect();

		Assert.That(textRect.Width, Is.GreaterThan(0));
		Assert.That(textRect.Height, Is.GreaterThan(0));
		Assert.That(iconRect.Width, Is.GreaterThan(0));
		Assert.That(iconRect.Height, Is.GreaterThan(0));
		Assert.That(textRect.IntersectsWith(iconRect), Is.False);

		var screenshot = App.Screenshot();
		using var image = new MagickImage(screenshot);

		Assert.That(image.Height, Is.GreaterThan(image.Width), "The issue requires portrait orientation.");
		AssertRectangleIsInsideImage(textRect, image);
		AssertRectangleIsInsideImage(iconRect, image);

		const byte channelTolerance = 96;
		var textRedPixels = CountRedPixels(screenshot, textRect, channelTolerance);
		var iconRedPixels = CountRedPixels(screenshot, iconRect, channelTolerance);
		var textSampledPixels = textRect.Width * textRect.Height;
		var iconSampledPixels = iconRect.Width * iconRect.Height;
		var textRequiredPixels = Math.Max(20, textSampledPixels / 100);
		var iconRequiredPixels = Math.Max(20, iconSampledPixels / 100);

		Assert.That(
			textRedPixels,
			Is.GreaterThanOrEqualTo(textRequiredPixels),
			$"The Text 1 reference did not render cleanly red: measured {textRedPixels} of {textSampledPixels} pixels with tolerance {channelTolerance}; required {textRequiredPixels}.");

		Assert.That(
			iconRedPixels,
			Is.GreaterThanOrEqualTo(iconRequiredPixels),
			$"Issue30990 toolbar icon did not render with the arranged red Shell foreground: measured {iconRedPixels} of {iconSampledPixels} pixels ({(double)iconRedPixels / iconSampledPixels:P2}) with tolerance {channelTolerance}; required {iconRequiredPixels}.");
	}

	static void AssertRectangleIsInsideImage(Rectangle rectangle, MagickImage image)
	{
		Assert.That(rectangle.X, Is.GreaterThanOrEqualTo(0));
		Assert.That(rectangle.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(rectangle.Right, Is.LessThanOrEqualTo((int)image.Width));
		Assert.That(rectangle.Bottom, Is.LessThanOrEqualTo((int)image.Height));
	}

	static long CountRedPixels(byte[] screenshot, Rectangle rectangle, byte tolerance)
	{
		using var region = new MagickImage(screenshot);
		region.Crop(new MagickGeometry(rectangle.X, rectangle.Y, (uint)rectangle.Width, (uint)rectangle.Height));

		long count = 0;
		foreach (var colorCount in region.Histogram())
		{
			var color = colorCount.Key;
			if (color.R >= byte.MaxValue - tolerance &&
				color.G <= tolerance &&
				color.B <= tolerance)
			{
				count += (long)colorCount.Value;
			}
		}

		return count;
	}
}
#endif

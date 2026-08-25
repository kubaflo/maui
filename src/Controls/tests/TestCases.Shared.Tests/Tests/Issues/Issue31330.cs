#if ANDROID
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31330 : _IssuesUITest
{
	public Issue31330(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "Rectangle renders as a thin line for small fractional heights";

	[Test]
	[Category(UITestCategories.Shape)]
	public void FractionalHeightRectangleRendersFilled()
	{
		int postTapControlCount = -1;

		App.SetOrientationPortrait();
		App.WaitForElement("AddRectangle");
		Assert.That(postTapControlCount, Is.EqualTo(-1));
		Assert.That(App.FindElements("RedBoxView"), Is.Empty);
		Assert.That(App.FindElements("BlueRectangle"), Is.Empty);

		App.Tap("AddRectangle");

		var redElement = App.WaitForElement("RedBoxView");
		var blueElement = App.WaitForElement("BlueRectangle");
		postTapControlCount = App.FindElements("RedBoxView").Count + App.FindElements("BlueRectangle").Count;
		Assert.That(postTapControlCount, Is.EqualTo(2));
		Assert.That(App.WaitForTextToBePresentInElement("Issue31330Status", "Controls added"), Is.True,
			"The controls were not added and scrolled into view.");

		var redFrame = redElement.GetRect();
		var blueFrame = blueElement.GetRect();
		Assert.That(redFrame.Width, Is.GreaterThanOrEqualTo(20));
		Assert.That(redFrame.Height, Is.GreaterThanOrEqualTo(1));
		Assert.That(blueFrame.Width, Is.EqualTo(redFrame.Width).Within(2));
		Assert.That(blueFrame.Height, Is.EqualTo(redFrame.Height).Within(2));
		Assert.That(blueFrame.X, Is.GreaterThan(redFrame.Right));

		int redArea = redFrame.Width * redFrame.Height;
		int blueArea = blueFrame.Width * blueFrame.Height;
		int requiredRedPixels = (int)Math.Ceiling(redArea * 0.6);
		int requiredBluePixels = (int)Math.Ceiling(blueArea * 0.6);
		int redPixels = -1;
		int bluePixels = -1;

		for (int captureAttempt = 0; captureAttempt < 3; captureAttempt++)
		{
			byte[] screenshot = App.Screenshot();
			using var image = new MagickImage(screenshot);
			Assert.That(image.Height, Is.GreaterThan(image.Width), "The Android surface should be portrait.");
			AssertFrameIsInsideImage(redFrame, image);
			AssertFrameIsInsideImage(blueFrame, image);

			using var pixels = image.GetPixels();
			redPixels = CountPixels(pixels, redFrame, IsRed);
			bluePixels = CountPixels(pixels, blueFrame, IsBlue);
			if (redPixels >= requiredRedPixels)
				break;
		}

		Assert.That(redPixels, Is.GreaterThanOrEqualTo(requiredRedPixels),
			$"Red BoxView fill coverage was {redPixels} pixels; required at least {requiredRedPixels} of {redArea}.");
		Assert.That(bluePixels, Is.GreaterThanOrEqualTo(requiredBluePixels),
			$"Blue Rectangle fill coverage was {bluePixels} pixels; required at least {requiredBluePixels} of {blueArea}.");
	}

	static void AssertFrameIsInsideImage(System.Drawing.Rectangle frame, MagickImage image)
	{
		Assert.That(frame.X, Is.GreaterThanOrEqualTo(0));
		Assert.That(frame.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(frame.Right, Is.LessThanOrEqualTo((int)image.Width));
		Assert.That(frame.Bottom, Is.LessThanOrEqualTo((int)image.Height));
	}

	static int CountPixels(IPixelCollection<byte> pixels, System.Drawing.Rectangle frame, Func<IMagickColor<byte>, bool> matches)
	{
		int count = 0;
		for (int y = frame.Top; y < frame.Bottom; y++)
		{
			for (int x = frame.Left; x < frame.Right; x++)
			{
				var color = pixels.GetPixel(x, y).ToColor();
				if (color is null)
					throw new InvalidOperationException($"Pixel color was unavailable at ({x}, {y}).");

				if (matches(color))
					count++;
			}
		}

		return count;
	}

	static bool IsRed(IMagickColor<byte> color) =>
		color.R >= 180 && color.G <= 80 && color.B <= 80;

	static bool IsBlue(IMagickColor<byte> color) =>
		color.B >= 180 && color.R <= 80 && color.G <= 80;
}
#endif

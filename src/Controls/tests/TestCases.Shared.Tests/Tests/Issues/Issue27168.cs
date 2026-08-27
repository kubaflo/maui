#if ANDROID
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27168 : _IssuesUITest
{
	const int MaximumLaunchFrames = 20;
	const ulong MinimumSplashPixels = 250;

	public Issue27168(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Unable to disable Android splash screen";

	[Test]
	[Category(UITestCategories.LifeCycle)]
	public void RestartDoesNotRenderRegisteredSplashArtwork()
	{
		App.SetOrientationPortrait();

		var page = App.WaitForElement("Issue27168Page");
		var layout = App.WaitForElement("Issue27168Layout");
		var title = App.WaitForElement("Issue27168Title");
		var description = App.WaitForElement("Issue27168Description");
		var status = App.WaitForElement("Issue27168Status");
		var button = App.WaitForElement("Issue27168ConfirmButton");

		Assert.Multiple(() =>
		{
			Assert.That(page.GetRect().Height, Is.GreaterThan(0));
			Assert.That(layout.GetRect().Height, Is.GreaterThan(0));
			Assert.That(title.GetRect().Height, Is.GreaterThan(0));
			Assert.That(description.GetRect().Height, Is.GreaterThan(0));
			Assert.That(status.GetRect().Height, Is.GreaterThan(0));
			Assert.That(button.GetRect().Height, Is.GreaterThan(0));
		});

		var settledFrame = App.Screenshot();
		using (var settledImage = new MagickImage(settledFrame))
		{
			Assert.That(settledImage.Height, Is.GreaterThan(settledImage.Width), "The launch test requires portrait native-window geometry.");
		}
		Assert.That(ContainsSplashArtwork(settledFrame), Is.False, "The settled page must be a clean negative control for splash detection.");

		var observedSplashFrames = -1;
		var capturedPostLaunchFrame = false;
		var appRenderedAfterLaunch = false;

		App.CloseApp();
		Assert.That(App.AppState, Is.EqualTo(ApplicationState.NotRunning));

		App.LaunchApp();
		observedSplashFrames = 0;

		for (var frameIndex = 0; frameIndex < MaximumLaunchFrames; frameIndex++)
		{
			var launchFrame = App.Screenshot();
			capturedPostLaunchFrame = true;

			if (ContainsSplashArtwork(launchFrame))
				observedSplashFrames++;

			if (App.FindElements("GoToTestButton").Count > 0)
			{
				appRenderedAfterLaunch = true;
				break;
			}
		}

		Assert.Multiple(() =>
		{
			Assert.That(capturedPostLaunchFrame, Is.True, "At least one native-window frame must be captured after activation.");
			Assert.That(appRenderedAfterLaunch, Is.True, "The HostApp issue selector must render after activation.");
		});

		App.EnterText("SearchBar", Issue);
		App.WaitForElement("GoToTestButton");
		App.Tap("GoToTestButton");
		App.WaitForElement("Issue27168Page");

		Assert.That(
			observedSplashFrames,
			Is.EqualTo(0),
			$"Android launch displayed a splash frame after splash disabling was requested; observedSplashFrames={observedSplashFrames}, expected=0.");
	}

	static bool ContainsSplashArtwork(byte[] screenshot)
	{
		using var image = new MagickImage(screenshot);

		var sampleWidth = image.Width / 2;
		var sampleHeight = image.Height / 2;
		image.Crop(new MagickGeometry(
			(int)((image.Width - sampleWidth) / 2),
			(int)((image.Height - sampleHeight) / 2),
			sampleWidth,
			sampleHeight));

		ulong splashPixels = 0;
		foreach (var colorCount in image.Histogram())
		{
			var color = colorCount.Key;
			var red = color.R;
			var green = color.G;
			var blue = color.B;

			if (red is >= 95 and <= 190 &&
				green is >= 25 and <= 145 &&
				blue is >= 120 and <= 205 &&
				red - green >= 25 &&
				blue - green >= 40 &&
				blue - red <= 65)
			{
				splashPixels += colorCount.Value;
			}
		}

		return splashPixels >= MinimumSplashPixels;
	}
}
#endif

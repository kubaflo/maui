#if IOS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37138 : _IssuesUITest
{
	public Issue37138(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Shell gradient color not working";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellBackgroundGradientRendersInBothNativeBars()
	{
		App.SetOrientationPortrait();
		Assert.That(App.WaitForElement("Issue37138Result").GetText(), Is.EqualTo("NO BUG:"));

		App.Tap("Issue37138ShowGradientShell");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue37138Result", "SHELL LOADED", TimeSpan.FromSeconds(20)),
			Is.True,
			"Shell.Loaded did not complete after replacing Window.Page.");

		var referenceRect = App.WaitForElement("Issue37138ExpectedGradientLabel").GetRect();
		var titleRect = App.WaitForElement("Gradient Shell").GetRect();
		var homeRect = App.WaitForElement("Home").GetRect();
		var secondRect = App.WaitForElement("Second").GetRect();
		var windowRect = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(referenceRect.Y, Is.GreaterThan(titleRect.Y + titleRect.Height), "The reference gradient must be below the toolbar title.");
			Assert.That(homeRect.Y, Is.GreaterThan(referenceRect.Y + referenceRect.Height), "The Home tab must be in the bottom bar.");
			Assert.That(secondRect.Y, Is.EqualTo(homeRect.Y).Within(2), "Both expected tabs must occupy the same bottom bar.");
			Assert.That(homeRect.X, Is.LessThan(secondRect.X), "Home and Second must retain their expected order.");
		});

		bool toolbarMatches = false;
		bool tabBarMatches = false;
		var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);

		do
		{
			using var screenshot = new MagickImage(App.Screenshot());
			Assert.That(screenshot.Width, Is.LessThan(screenshot.Height), "The rendered iOS screenshot must be portrait.");

			double scaleX = screenshot.Width / windowRect.Width;
			double scaleY = screenshot.Height / windowRect.Height;
			int regionWidth = Math.Max(3, (int)(windowRect.Width * 0.04 * scaleX));
			int regionHeight = Math.Max(3, (int)(6 * scaleY));
			int leadingX = (int)((windowRect.X + (windowRect.Width * 0.04)) * scaleX);
			int trailingX = (int)((windowRect.X + (windowRect.Width * 0.92)) * scaleX);
			int toolbarY = (int)((titleRect.Y + (titleRect.Height / 2) - 3) * scaleY);
			int tabBarY = (int)((homeRect.Y + 4) * scaleY);

			toolbarMatches =
				RegionMatches(screenshot, leadingX, toolbarY, regionWidth, regionHeight, MagickColors.DeepPink) &&
				RegionMatches(screenshot, trailingX, toolbarY, regionWidth, regionHeight, MagickColors.DeepSkyBlue);
			tabBarMatches =
				RegionMatches(screenshot, leadingX, tabBarY, regionWidth, regionHeight, MagickColors.DeepPink) &&
				RegionMatches(screenshot, trailingX, tabBarY, regionWidth, regionHeight, MagickColors.DeepSkyBlue);
		}
		while ((!toolbarMatches || !tabBarMatches) && DateTime.UtcNow < timeout);

		Assert.That(
			toolbarMatches,
			Is.True,
			"Shell toolbar gradient did not render DeepPink at the leading edge and DeepSkyBlue at the trailing edge");
		Assert.That(
			tabBarMatches,
			Is.True,
			"Shell tab bar gradient did not render DeepPink at the leading edge and DeepSkyBlue at the trailing edge");
	}

	static bool RegionMatches(MagickImage source, int x, int y, int width, int height, IMagickColor<byte> expectedColor)
	{
		using var actualRegion = source.Clone();
		actualRegion.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
		actualRegion.ResetPage();

		using var expectedRegion = new MagickImage(expectedColor, (uint)width, (uint)height);
		double difference = actualRegion.Compare(expectedRegion, ErrorMetric.RootMeanSquared, Channels.Red | Channels.Green | Channels.Blue);
		return difference < 0.22;
	}
}
#endif

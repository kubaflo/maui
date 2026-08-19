#if WINDOWS
using ImageMagick;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37149 : _IssuesUITest
{
	public Issue37149(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Shell Background does not apply to the TabBar on Windows";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellBackgroundGradientShouldRenderInTabBar()
	{
		App.WaitForElement("ResultLabel");
		Assert.That(App.FindElement("ResultLabel").GetText(), Is.EqualTo("NO BUG:"));

		App.WaitForElement("OpenShellButton");
		App.Tap("OpenShellButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "SHELL LOADED", TimeSpan.FromSeconds(15)),
			Is.True,
			"The replacement Shell did not complete its Loaded lifecycle transition.");
		Assert.That(App.FindElement("ResultLabel").GetText(), Is.EqualTo("SHELL LOADED"));
		Assert.That(
			App.WaitForElement("GradientDescription").GetText(),
			Is.EqualTo("Gradient stops: OrangeRed 0; Purple 1"));

		App.WaitForElement("navViewItem");
		var tabs = App.FindElements("navViewItem");
		Assert.That(tabs, Has.Count.EqualTo(2), "The Shell TabBar must expose exactly the Home and Details tabs.");
		var homeTab = tabs.ElementAt(0);
		var detailsTab = tabs.ElementAt(1);
		Assert.That(homeTab.GetText(), Is.EqualTo("Home"));
		Assert.That(detailsTab.GetText(), Is.EqualTo("Details"));

		var homeRect = homeTab.GetRect();
		var detailsRect = detailsTab.GetRect();
		Assert.That(homeRect.Width, Is.GreaterThan(0), "The Home tab must exist with a nonzero width.");
		Assert.That(detailsRect.Width, Is.GreaterThan(0), "The Details tab must exist with a nonzero width.");
		Assert.That(detailsRect.X, Is.GreaterThan(homeRect.X), "The Details tab must be located to the right of Home.");
		Assert.That(detailsRect.Y, Is.EqualTo(homeRect.Y).Within(1), "Home and Details must share the TabBar.");

		var descriptionRect = App.FindElement("GradientDescription").GetRect();
		Assert.That(descriptionRect.Width, Is.GreaterThan(0), "The loaded Shell content must have a nonzero width.");
		var screenshot = App.Screenshot();
		using var image = new MagickImage(screenshot);
		using var pixels = image.GetPixels();

		var tabY = homeRect.Y + (homeRect.Height / 2);
		const int tolerance = 55;

		var leftX = descriptionRect.X + (descriptionRect.Width / 4);
		var rightX = descriptionRect.X + ((descriptionRect.Width * 3) / 4);
		var leftExpected = (R: 224, G: 49, B: 39);
		var rightExpected = (R: 159, G: 22, B: 99);

		var leftTab = pixels.GetPixel(leftX, tabY).ToColor()
			?? throw new InvalidOperationException("Failed to read the left TabBar background pixel.");
		var rightTab = pixels.GetPixel(rightX, tabY).ToColor()
			?? throw new InvalidOperationException("Failed to read the right TabBar background pixel.");

		bool Matches(int red, int green, int blue, (int R, int G, int B) expected) =>
			Math.Abs(red - expected.R) <= tolerance &&
			Math.Abs(green - expected.G) <= tolerance &&
			Math.Abs(blue - expected.B) <= tolerance;

		Assert.That(
			Matches(leftTab.R, leftTab.G, leftTab.B, leftExpected) &&
			Matches(rightTab.R, rightTab.G, rightTab.B, rightExpected),
			Is.True,
			$"Shell TabBar background pixels did not render the configured gradient; " +
			$"TabBar left ({leftX},{tabY}) RGB=({leftTab.R},{leftTab.G},{leftTab.B}) expected=({leftExpected.R},{leftExpected.G},{leftExpected.B}), " +
			$"TabBar right ({rightX},{tabY}) RGB=({rightTab.R},{rightTab.G},{rightTab.B}) expected=({rightExpected.R},{rightExpected.G},{rightExpected.B}), tolerance={tolerance}.");
	}
}
#endif

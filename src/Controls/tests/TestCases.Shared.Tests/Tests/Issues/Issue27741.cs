using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27741 : _IssuesUITest
{
	public override string Issue => "Screen not using the full width by default when on locked device orientation";

	public Issue27741(TestDevice testDevice)
		: base(testDevice)
	{
	}

#if IOS
	[Test]
	[Category(UITestCategories.Layout)]
	public void AffectedSurfaceFillsWindowWhenLaunchedInLandscape()
	{
		App.SetOrientationPortrait();
		var portraitWindow = GetWindowRect();
		Assert.That(portraitWindow.Height, Is.GreaterThan(portraitWindow.Width),
			"Issue27741 requires the initial app window to be in portrait.");

		var portraitRoot = GetRequiredRect("RootLayout");
		var portraitAffected = GetRequiredRect("AffectedSurface");
		Assert.That(portraitRoot.Width, Is.EqualTo(portraitWindow.Width).Within(1),
			"Issue27741 portrait reference root did not fill the window.");
		Assert.That(portraitAffected.Width, Is.EqualTo(portraitRoot.Width).Within(1),
			"Issue27741 portrait reference surface did not fill its root.");
		Assert.That(portraitAffected.X + portraitAffected.Width,
			Is.EqualTo(portraitRoot.X + portraitRoot.Width).Within(1),
			"Issue27741 portrait reference surface did not reach its root's right edge.");

		App.SetOrientationLandscape();
		var rotatedWindow = GetWindowRect();
		Assert.That(rotatedWindow.Width, Is.GreaterThan(rotatedWindow.Height),
			"Issue27741 did not transition the native window to landscape.");

		App.CloseApp();
		App.LaunchApp();
		var landscapeWindow = GetWindowRect();
		Assert.That(landscapeWindow.Width, Is.GreaterThan(landscapeWindow.Height),
			"Issue27741 relaunch did not preserve landscape native window geometry.");

		App.WaitForGoToTestButtonWithRecovery(Issue);
		App.EnterText("SearchBar", Issue);
		App.WaitForElement("GoToTestButton");
		App.Tap("GoToTestButton");

		Assert.That(App.WaitForTextToBePresentInElement("LayoutGeneration", "Complete:"), Is.True,
			"Issue27741 did not observe a post-attachment layout callback after relaunch.");
		var generationElement = App.WaitForElement("LayoutGeneration");
		var generationText = generationElement.GetText();
		if (generationText is null)
			throw new AssertionException("Issue27741 layout generation text was null.");

		Assert.That(int.TryParse(generationText["Complete:".Length..], out var generation), Is.True,
			$"Issue27741 layout generation was not numeric: '{generationText}'.");
		Assert.That(generation, Is.GreaterThan(0),
			"Issue27741 did not complete a post-relaunch layout generation.");

		var landscapeRoot = GetRequiredRect("RootLayout");
		var landscapeAffected = GetRequiredRect("AffectedSurface");
		var measurements =
			$"window={landscapeWindow}; root={landscapeRoot}; affected={landscapeAffected}";

		Assert.That(landscapeRoot.Width, Is.EqualTo(landscapeWindow.Width).Within(1),
			$"Issue27741 landscape launch width mismatch: {measurements}");
		Assert.That(landscapeRoot.X, Is.GreaterThanOrEqualTo(landscapeWindow.X - 1));
		Assert.That(landscapeRoot.X + landscapeRoot.Width,
			Is.LessThanOrEqualTo(landscapeWindow.X + landscapeWindow.Width + 1));
		Assert.That(landscapeAffected.Height, Is.GreaterThan(0));
		Assert.That(landscapeAffected.Y, Is.GreaterThanOrEqualTo(landscapeWindow.Y - 1));
		Assert.That(landscapeAffected.Y + landscapeAffected.Height,
			Is.LessThanOrEqualTo(landscapeWindow.Y + landscapeWindow.Height + 1));

		Assert.That(landscapeAffected.Width,
			Is.EqualTo(landscapeRoot.Width).Within(1),
			$"Issue27741 landscape launch width mismatch: {measurements}");
		Assert.That(landscapeAffected.X + landscapeAffected.Width,
			Is.EqualTo(landscapeRoot.X + landscapeRoot.Width).Within(1),
			$"Issue27741 landscape launch width mismatch: {measurements}");
	}

	System.Drawing.Rectangle GetWindowRect()
	{
		var window = App.WaitForElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
		Assert.That(window, Is.Not.Null, "Issue27741 could not locate the active native window.");
		return window.GetRect();
	}

	System.Drawing.Rectangle GetRequiredRect(string automationId)
	{
		var element = App.WaitForElement(automationId);
		Assert.That(element, Is.Not.Null, $"Issue27741 could not locate '{automationId}'.");
		return element.GetRect();
	}
#endif
}

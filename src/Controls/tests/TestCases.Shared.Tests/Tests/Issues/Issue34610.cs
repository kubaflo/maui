#if IOS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34610 : _IssuesUITest
{
	const string HorizontalFrameFailure = "Shell TitleView horizontal frame must match the full-width page content";

	public override string Issue => "Shell TitleView on iOS has unremovable horizontal margins and vertical gap";

	public Issue34610(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellTitleViewFillsNavigationBarWithoutGap()
	{
		IUIElement WaitForRequiredElement(string automationId)
		{
			var element = App.WaitForElement(automationId);
			if (element is null)
				throw new AssertionException($"Expected element '{automationId}' was not found.");

			return element;
		}

		App.SetOrientationPortrait();

		WaitForRequiredElement("OpenShellScenario");
		App.Tap("OpenShellScenario");

		var windowElement = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
		if (windowElement is null)
			throw new AssertionException("The native application window was not found.");

		var viewportRect = windowElement.GetRect();
		Assert.That(viewportRect.Height, Is.GreaterThan(viewportRect.Width),
			$"The test requires portrait orientation, but the viewport was {viewportRect.Width}x{viewportRect.Height}.");

		int layoutGeneration = -1;
		App.RetryAssert(() =>
		{
			var layoutText = WaitForRequiredElement("LayoutGeneration").GetText();
			if (layoutText is null)
				throw new AssertionException("The layout generation element had no text.");

			const string prefix = "Layout generation: ";
			Assert.That(layoutText, Does.StartWith(prefix), "The Shell layout callback did not publish its generation.");
			Assert.That(int.TryParse(layoutText[prefix.Length..], out layoutGeneration), Is.True,
				$"The layout generation was not numeric: '{layoutText}'.");
			Assert.That(layoutGeneration, Is.GreaterThanOrEqualTo(0),
				"The Shell TitleView must raise SizeChanged after attachment.");
		});

		var titleLabel = WaitForRequiredElement("TitleLabel");
		var menuLabel = WaitForRequiredElement("MenuLabel");
		var settingsLabel = WaitForRequiredElement("SettingsLabel");
		Assert.That(titleLabel.GetText(), Is.EqualTo("MY APP TITLE"), "The intended center title label was not rendered.");

		Rectangle titleRect = new(-1, -1, -1, -1);
		Rectangle contentRect = new(-1, -1, -1, -1);
		Rectangle menuRect = new(-1, -1, -1, -1);
		Rectangle titleLabelRect = new(-1, -1, -1, -1);
		Rectangle settingsRect = new(-1, -1, -1, -1);
		Rectangle previousTitleRect = new(-2, -2, -2, -2);
		Rectangle previousContentRect = new(-2, -2, -2, -2);
		Rectangle previousMenuRect = new(-2, -2, -2, -2);
		Rectangle previousTitleLabelRect = new(-2, -2, -2, -2);
		Rectangle previousSettingsRect = new(-2, -2, -2, -2);

		App.RetryAssert(() =>
		{
			titleRect = WaitForRequiredElement("AffectedTitleView").GetRect();
			contentRect = WaitForRequiredElement("AffectedPageContent").GetRect();
			menuRect = menuLabel.GetRect();
			titleLabelRect = titleLabel.GetRect();
			settingsRect = settingsLabel.GetRect();

			Assert.Multiple(() =>
			{
				Assert.That(titleRect.Width, Is.GreaterThan(0), "The affected TitleView must have a nonempty native frame.");
				Assert.That(titleRect.Height, Is.GreaterThan(0), "The affected TitleView must have a nonempty native frame.");
				Assert.That(contentRect.Width, Is.GreaterThan(0), "The blue page content must have a nonempty native frame.");
				Assert.That(contentRect.Height, Is.GreaterThan(0), "The blue page content must have a nonempty native frame.");
				Assert.That(menuRect.Width, Is.GreaterThan(0), "The intended menu label must have a nonempty native frame.");
				Assert.That(titleLabelRect.Width, Is.GreaterThan(0), "The intended title label must have a nonempty native frame.");
				Assert.That(settingsRect.Width, Is.GreaterThan(0), "The intended settings label must have a nonempty native frame.");
			});

			bool framesAreStable =
				titleRect == previousTitleRect &&
				contentRect == previousContentRect &&
				menuRect == previousMenuRect &&
				titleLabelRect == previousTitleLabelRect &&
				settingsRect == previousSettingsRect;

			previousTitleRect = titleRect;
			previousContentRect = contentRect;
			previousMenuRect = menuRect;
			previousTitleLabelRect = titleLabelRect;
			previousSettingsRect = settingsRect;

			Assert.That(framesAreStable, Is.True, "The native accessibility frames must settle before geometry is evaluated.");
		});

		const int tolerance = 1;
		Assert.Multiple(() =>
		{
			Assert.That(contentRect.Left, Is.EqualTo(viewportRect.Left).Within(tolerance),
				$"The blue content must prove the viewport oracle is flush on the left; expected={viewportRect.Left}, actual={contentRect.Left}, tolerance={tolerance}.");
			Assert.That(contentRect.Right, Is.EqualTo(viewportRect.Right).Within(tolerance),
				$"The blue content must prove the viewport oracle is flush on the right; expected={viewportRect.Right}, actual={contentRect.Right}, tolerance={tolerance}.");
			Assert.That(contentRect.Width, Is.EqualTo(viewportRect.Width).Within(tolerance),
				$"The blue content must prove the viewport oracle has full width; expected={viewportRect.Width}, actual={contentRect.Width}, tolerance={tolerance}.");
		});

		Assert.Multiple(() =>
		{
			Assert.That(titleRect.Left, Is.EqualTo(contentRect.Left).Within(tolerance),
				$"{HorizontalFrameFailure}; left expected={contentRect.Left}, actual={titleRect.Left}, tolerance={tolerance}, title={titleRect}, content={contentRect}, viewport={viewportRect}.");
			Assert.That(titleRect.Right, Is.EqualTo(contentRect.Right).Within(tolerance),
				$"{HorizontalFrameFailure}; right expected={contentRect.Right}, actual={titleRect.Right}, tolerance={tolerance}, title={titleRect}, content={contentRect}, viewport={viewportRect}.");
			Assert.That(titleRect.Width, Is.EqualTo(contentRect.Width).Within(tolerance),
				$"{HorizontalFrameFailure}; width expected={contentRect.Width}, actual={titleRect.Width}, tolerance={tolerance}, title={titleRect}, content={contentRect}, viewport={viewportRect}.");
			Assert.That(titleRect.Bottom, Is.EqualTo(contentRect.Top).Within(tolerance),
				$"Shell TitleView must meet the page content with zero vertical gap; expected bottom={contentRect.Top}, actual={titleRect.Bottom}, tolerance={tolerance}, title={titleRect}, content={contentRect}.");
		});
	}
}
#endif

#if IOS && !MACCATALYST
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37423 : _IssuesUITest
{
	public override string Issue => "Shell TabBarBackgroundColor renders an opaque background behind the Liquid Glass tab bar";

	public Issue37423(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellTabBarUsesTheSystemLiquidGlassBackground()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue37423ShowShell");

		Assert.That(App.WaitForElement("Issue37423OS").GetText(), Is.EqualTo("iOS 26+"));
		Assert.That(App.WaitForElement("Issue37423Theme").GetText(), Is.EqualTo("Light"));
		Assert.That(App.WaitForElement("Issue37423Transition").GetText(), Is.EqualTo("-1"));

		var defaultAlpha = double.Parse(
			App.WaitForElement("Issue37423DefaultAlpha").GetText()!,
			CultureInfo.InvariantCulture);
		Assert.That(defaultAlpha, Is.EqualTo(0).Within(0.001), "The untouched iOS 26 UITabBar reference must be transparent.");

		App.Tap("Issue37423ShowShell");
		App.WaitForElement("Issue37423ShellReady");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue37423Transition", "1"),
			Is.True,
			"The Shell Loaded callback did not capture the native UITabBar state.");

		var actualAlpha = double.Parse(
			App.WaitForElement("Issue37423ActualAlpha").GetText()!,
			CultureInfo.InvariantCulture);
		var defaultAlphaText = defaultAlpha.ToString("0.###", CultureInfo.InvariantCulture);
		var actualAlphaText = actualAlpha.ToString("0.###", CultureInfo.InvariantCulture);

		Assert.That(
			actualAlpha,
			Is.EqualTo(defaultAlpha).Within(0.001),
			$"Issue 37423 expected the Shell UITabBar background alpha to match the captured iOS 26 default alpha {defaultAlphaText}, but measured alpha was {actualAlphaText}");
	}
}
#endif

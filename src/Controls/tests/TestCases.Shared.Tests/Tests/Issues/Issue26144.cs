#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26144 : _IssuesUITest
{
	const string NavigationMarker = "Issue26144NavigationMarker";
	const string HomeContent = "Issue26144HomeContent";

	public Issue26144(TestDevice device) : base(device) { }

	public override string Issue => "Shell TabBar content does not render after navigating away and back";

	[Test]
	[Category(UITestCategories.Shell)]
	public void NestedShellContentRendersAfterSecondDashboardVisit()
	{
		var navigationSequence = -1;
		var initialMarker = App.WaitForElement(NavigationMarker);
		navigationSequence = ReadNavigationSequence(initialMarker, "MainPage");

		AssertInWindow(App.WaitForElement("Issue26144MainContent"), "MainPage content");

		App.Tap("Issue26144OpenDashboard");
		navigationSequence = WaitForNavigation("DashboardPage", navigationSequence);
		AssertInWindow(App.WaitForElement(HomeContent), "first dashboard HomePage content");

		App.Tap("Issue26144BackToMain");
		navigationSequence = WaitForNavigation("MainPage", navigationSequence);
		App.WaitForElement("Issue26144MainContent");

		App.Tap("Issue26144OpenDashboard");
		navigationSequence = WaitForNavigation("DashboardPage", navigationSequence);
		Assert.That(navigationSequence, Is.GreaterThan(1), "The outer Shell did not complete the dashboard-main-dashboard navigation sequence");

		var finalHome = App.WaitForElement(
			HomeContent,
			"Issue26144 second dashboard visit did not render HomePage content");
		var finalElements = App.FindElements(HomeContent);
		Assert.That(finalElements, Has.Count.EqualTo(1),
			$"Issue26144 expected one HomePage native element after the second dashboard visit, but found {finalElements.Count}");
		AssertInWindow(finalHome, "second dashboard HomePage content");
	}

	int WaitForNavigation(string route, int previousSequence)
	{
		var marker = App.WaitForElement(() =>
		{
			var candidate = App.FindElement(NavigationMarker);
			if (candidate is null)
				return null;

			var text = candidate.GetText();
			return text is not null
				&& TryReadNavigationSequence(text, route, out var sequence)
				&& sequence > previousSequence
					? candidate
					: null;
		}, $"Timed out waiting for the outer Shell to navigate to {route}");

		return ReadNavigationSequence(marker, route);
	}

	static int ReadNavigationSequence(IUIElement marker, string route)
	{
		var text = marker.GetText();
		Assert.That(text, Is.Not.Null, $"The outer Shell navigation marker for {route} had no text");
		if (text is null)
			throw new InvalidOperationException($"The outer Shell navigation marker for {route} had no text");

		Assert.That(TryReadNavigationSequence(text, route, out var sequence), Is.True,
			$"Unexpected outer Shell navigation marker '{text}' while waiting for {route}");
		return sequence;
	}

	static bool TryReadNavigationSequence(string text, string route, out int sequence)
	{
		sequence = -1;
		var prefix = $"{route}:";
		return text.StartsWith(prefix, StringComparison.Ordinal)
			&& int.TryParse(text.AsSpan(prefix.Length), out sequence);
	}

	void AssertInWindow(IUIElement element, string description)
	{
		var frame = element.GetRect();
		var window = ((AppiumApp)App).Driver.Manage().Window.Size;

		Assert.Multiple(() =>
		{
			Assert.That(frame.Width, Is.GreaterThan(0), $"{description} native frame width was {frame.Width}");
			Assert.That(frame.Height, Is.GreaterThan(0), $"{description} native frame height was {frame.Height}");
			Assert.That(frame.X, Is.GreaterThanOrEqualTo(0), $"{description} native frame X was {frame.X}");
			Assert.That(frame.Y, Is.GreaterThanOrEqualTo(0), $"{description} native frame Y was {frame.Y}");
			Assert.That(frame.Right, Is.LessThanOrEqualTo(window.Width),
				$"{description} native frame right edge {frame.Right} exceeded window width {window.Width}");
			Assert.That(frame.Bottom, Is.LessThanOrEqualTo(window.Height),
				$"{description} native frame bottom edge {frame.Bottom} exceeded window height {window.Height}");
		});
	}
}
#endif

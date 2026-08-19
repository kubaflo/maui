#if WINTEST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37534 : _IssuesUITest
{
	public override string Issue => "WebView CanGoBack remains true after returning to the initial document";

	public Issue37534(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.WebView)]
	public void CanGoBackIsFalseAfterReturningFromFragmentNavigation()
	{
		Assert.That(Device, Is.EqualTo(TestDevice.Windows));
		Assert.That(App, Is.InstanceOf<AppiumApp>());
		var appiumApp = (AppiumApp)App;
		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.Multiple(() =>
		{
			Assert.That(windowSize.Width, Is.GreaterThan(0));
			Assert.That(windowSize.Height, Is.GreaterThan(0));
		});

		App.WaitForElement(AppiumQuery.ByXPath("//*[@Name='Help']"));
		App.Tap(AppiumQuery.ByXPath("//*[@Name='Help']"));

		App.WaitForTextToBePresentInElement("Issue37534NavigationState", "Page=Help");
		App.WaitForTextToBePresentInElement("Issue37534NavigationState", "WindowVisible=True");
		App.WaitForElement(AppiumQuery.ByXPath("//*[@Name='Help']"));
		App.WaitForElement(AppiumQuery.ByXPath("//*[@Name='Show index']"));

		string initialState = App.WaitForElement("Issue37534NavigationState").GetText()!;
		string identity = initialState[(initialState.LastIndexOf("Identity=", StringComparison.Ordinal) + "Identity=".Length)..];
		int initialSequence = ReadNavigationSequence(initialState);

		App.Tap(AppiumQuery.ByXPath("//*[@Name='Show index']"));
		App.WaitForTextToBePresentInElement("Issue37534NavigationState", "Page=Index");

		string indexState = App.WaitForElement("Issue37534NavigationState").GetText()!;
		Assert.Multiple(() =>
		{
			Assert.That(ReadNavigationSequence(indexState), Is.GreaterThan(initialSequence));
			Assert.That(indexState, Does.EndWith($"Identity={identity}"));
		});

		int indexSequence = ReadNavigationSequence(indexState);
		App.Tap("Issue37534BackButton");
		App.WaitForTextToBePresentInElement("Issue37534HistoryState", "First back completed");

		string returnedState = App.WaitForElement("Issue37534NavigationState").GetText()!;
		Assert.Multiple(() =>
		{
			Assert.That(returnedState, Does.Contain("Page=Help"));
			Assert.That(ReadNavigationSequence(returnedState), Is.GreaterThan(indexSequence));
			Assert.That(returnedState, Does.EndWith($"Identity={identity}"));
		});

		string firstBackState = App.WaitForElement("Issue37534HistoryState").GetText()!;
		Assert.That(firstBackState, Does.EndWith($"Identity={identity}"));

		App.Tap("Issue37534BackButton");
		App.WaitForTextToBePresentInElement("Issue37534RepeatedBackState", "Repeated back observed");

		string repeatedBackState = App.WaitForElement("Issue37534RepeatedBackState").GetText()!;
		Assert.That(
			repeatedBackState,
			Is.EqualTo("Repeated back observed CanGoBack=False"),
			"Second Help back observed CanGoBack=True; expected CanGoBack=False after returning from Index to Help.");
	}

	static int ReadNavigationSequence(string state)
	{
		int separator = state.IndexOf(';', StringComparison.Ordinal);
		return int.Parse(state["Navigation=".Length..separator]);
	}
}
#endif

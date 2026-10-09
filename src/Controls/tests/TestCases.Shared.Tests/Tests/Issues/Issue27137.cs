#if IOS
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27137 : _IssuesUITest
{
	const string EmptyViewText = "No items match your filter.";

	public Issue27137(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView EmptyView is hidden behind the iOS keyboard";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void EmptyViewRemainsVisibleAboveKeyboardAfterFiltering()
	{
		App.SetOrientationPortrait();

		var appiumApp = App as AppiumApp;
		Assert.That(appiumApp, Is.Not.Null, "The iOS geometry check requires the Appium driver.");
		if (appiumApp is null)
			return;

		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test must run in portrait orientation.");

		foreach (var expectedItem in new[] { "Apple", "Banana", "Cherry", "Orange" })
		{
			var item = App.WaitForElement(expectedItem);
			Assert.That(item.GetText(), Is.EqualTo(expectedItem));
			var itemFrame = item.GetRect();
			Assert.That(itemFrame.Width, Is.GreaterThan(0));
			Assert.That(itemFrame.Height, Is.GreaterThan(0));
		}

		Assert.That(App.FindElements(EmptyViewText), Is.Empty, "The EmptyView must not be present while items exist.");
		Assert.That(App.WaitForElement("FilterStatusLabel").GetText(), Is.EqualTo("waiting"));

		App.Tap("FilterSearchBar");
		App.EnterText("FilterSearchBar", "abcd");

		Assert.That(App.IsKeyboardShown(), Is.True, "The iOS software keyboard must remain open.");
		Assert.That(App.WaitForElement("FilterStatusLabel").GetText(), Is.EqualTo("filtered-count: 0"));

		var keyboard = appiumApp.Driver.FindElement(MobileBy.ClassName("UIAKeyboard"));
		var keyboardLocation = keyboard.Location;
		var keyboardSize = keyboard.Size;
		Assert.That(keyboardSize.Height, Is.GreaterThan(0), "The native keyboard must have a positive frame.");
		Assert.That(keyboardLocation.Y, Is.LessThan(windowSize.Height));

		var statusFrame = App.WaitForElement("FilterStatusLabel").GetRect();
		Assert.That(statusFrame.Width, Is.GreaterThan(0));
		Assert.That(statusFrame.Height, Is.GreaterThan(0));
		Assert.That(statusFrame.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(statusFrame.Y + statusFrame.Height, Is.LessThanOrEqualTo(keyboardLocation.Y));

		App.WaitForElement(EmptyViewText);
		var emptyView = appiumApp.Driver.FindElement(MobileBy.Name(EmptyViewText));
		Assert.That(emptyView.Text, Is.EqualTo(EmptyViewText));

		var emptyViewLocation = emptyView.Location;
		var emptyViewSize = emptyView.Size;
		Assert.That(emptyViewSize.Width, Is.GreaterThan(0));
		Assert.That(emptyViewSize.Height, Is.GreaterThan(0));
		Assert.That(emptyViewLocation.X, Is.GreaterThanOrEqualTo(0));
		Assert.That(emptyViewLocation.Y, Is.GreaterThanOrEqualTo(0));
		Assert.That(emptyViewLocation.X + emptyViewSize.Width, Is.LessThanOrEqualTo(windowSize.Width + 1));
		Assert.That(emptyViewLocation.Y + emptyViewSize.Height, Is.LessThanOrEqualTo(windowSize.Height + 1));
		Assert.That(
			emptyViewLocation.Y + emptyViewSize.Height,
			Is.LessThanOrEqualTo(keyboardLocation.Y + 1),
			$"EmptyView text must remain fully visible above the iOS keyboard after filtering; observed empty-view frame {emptyViewLocation} {emptyViewSize} and keyboard frame {keyboardLocation} {keyboardSize}.");
	}
}
#endif

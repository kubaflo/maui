#if WINDOWS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35301 : _IssuesUITest
{
	public Issue35301(TestDevice device) : base(device) { }

	public override string Issue => "Windows CollectionView applies WinUI styling by default";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DefaultSingleSelectionDoesNotAddWinUISelectionChrome()
	{
		var appiumApp = (AppiumApp)App;
		appiumApp.Driver.Manage().Window.Size = new Size(1280, 720);
		var windowSize = appiumApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Width, Is.EqualTo(1280), "The reproduction requires a 1280-pixel-wide window.");
		Assert.That(windowSize.Height, Is.EqualTo(720), "The reproduction requires a 720-pixel-high window.");

		var collectionRect = App.WaitForElement("Issue35301Collection").GetRect();
		var appleRect = App.WaitForElement("Issue35301Apple").GetRect();
		var bananaRect = App.WaitForElement("Issue35301Banana").GetRect();
		var cherryRect = App.WaitForElement("Issue35301Cherry").GetRect();
		Assert.That(App.WaitForTextToBePresentInElement(
			"Issue35301Status",
			"InitialCallbacks=-1;Callbacks=0;Selected=<none>;ChromeStyle=<not-inspected>",
			TimeSpan.FromSeconds(5)), Is.True, "The attached page must arm selection observation from its -1 sentinel.");
		var initialStatus = App.WaitForElement("Issue35301Status").GetText();

		Assert.That(initialStatus, Is.EqualTo("InitialCallbacks=-1;Callbacks=0;Selected=<none>;ChromeStyle=<not-inspected>"));
		Assert.That(appleRect.Y, Is.LessThan(bananaRect.Y), "Apple must be the first rendered item.");
		Assert.That(bananaRect.Y, Is.LessThan(cherryRect.Y), "Banana must be the second rendered item.");
		Assert.That(collectionRect.Contains(appleRect), Is.True, "Apple must be inside the CollectionView.");
		Assert.That(collectionRect.Contains(bananaRect), Is.True, "Banana must be inside the CollectionView.");
		Assert.That(collectionRect.Contains(cherryRect), Is.True, "Cherry must be inside the CollectionView.");

		App.Tap("Issue35301Apple");
		Assert.That(App.WaitForTextToBePresentInElement(
			"Issue35301Status",
			"InitialCallbacks=-1;Callbacks=1;Selected=Apple;ChromeStyle=",
			TimeSpan.FromSeconds(5)), Is.True, "SelectionChanged must complete once for Apple.");
		var selectedStatus = App.FindElement("Issue35301Status").GetText();
		Assert.That(selectedStatus, Does.Not.Contain("InspectionFailed"),
			"The selected native ListViewItem and its default style must be inspectable.");
		Assert.That(selectedStatus, Is.EqualTo("InitialCallbacks=-1;Callbacks=1;Selected=Apple;ChromeStyle=Absent"),
			$"Selected Apple gained unexpected WinUI selection chrome: native state was '{selectedStatus}'.");
	}
}
#endif

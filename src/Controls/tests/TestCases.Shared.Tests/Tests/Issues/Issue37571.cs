#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37571 : _IssuesUITest
{
	public Issue37571(TestDevice device) : base(device) { }

	public override string Issue => "Looped CarouselView stops responding after one traversal";

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void LoopedCarouselContinuesThroughTwoTraversals()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires portrait orientation.");

		var carousel = App.WaitForElement("TheCarouselView");
		var carouselRect = carousel.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(App.WaitForElement("ResultStatus").GetText(), Is.EqualTo("NO BUG:"));
			Assert.That(App.WaitForElement("lblPosition").GetText(), Is.EqualTo("3"));
			Assert.That(App.WaitForElement("lblCurrentItem").GetText(), Is.EqualTo("3"));
			Assert.That(App.WaitForElement("lblSelected").GetText(), Is.EqualTo("3"));
			Assert.That(carouselRect.Width, Is.GreaterThan(windowSize.Width * 0.9));
			Assert.That(carouselRect.Height, Is.GreaterThan(100));
		});
		App.WaitForElement("CarouselItem3");

		var startX = windowSize.Width * 0.8f;
		var endX = windowSize.Width * 0.2f;
		var centerY = carouselRect.CenterY();
		for (var transition = 0; transition < 10; transition++)
			App.DragCoordinates(startX, centerY, endX, centerY);

		App.Tap("CheckNavigation");
		var checkProcessed = App.WaitForTextToBePresentInElement(
			"CheckNavigation", "Checked", TimeSpan.FromSeconds(5));
		var navigationCompleted = checkProcessed &&
			App.FindElement("ResultStatus").GetText() == "NO BUG:";
		Assert.That(navigationCompleted, Is.True,
			"Looped CarouselView did not complete both forward looped traversals.");
	}
}
#endif

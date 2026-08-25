#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35775 : _IssuesUITest
{
	public Issue35775(TestDevice device) : base(device)
	{
	}

	public override string Issue => "IndicatorView leaks when linked to a CarouselView with a shared ObservableCollection";

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void PoppedLinkedControlsShouldBeCollectedWithRootedSharedSource()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775FeedCount", "Shared feed count: 120"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775GcState", "GC state: Not checked"),
			Is.True);

		App.Tap("Issue35775OpenButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775LeakPageReady", "Leak page loaded"),
			Is.True);
		App.Back();
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775VisitCount", "Completed visits: 1"),
			Is.True);

		App.Tap("Issue35775OpenButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775LeakPageReady", "Leak page loaded"),
			Is.True);
		App.Back();
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775VisitCount", "Completed visits: 2"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775LinkedState",
				"Linked controls: True; shared source: True"),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35775FeedCount", "Shared feed count: 120"),
			Is.True);

		App.Tap("Issue35775CollectButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775GcState",
				"GC state: Complete",
				timeout: TimeSpan.FromSeconds(15)),
			Is.True,
			"The bounded garbage collection callback did not complete.");

		var carouselElement = App.WaitForElement("Issue35775CarouselCount");
		var indicatorElement = App.WaitForElement("Issue35775IndicatorCount");
		if (carouselElement is null || indicatorElement is null)
		{
			Assert.Fail("Retained control count labels were not found.");
			return;
		}

		var carouselText = carouselElement.GetText();
		var indicatorText = indicatorElement.GetText();
		Assert.That(carouselText, Is.Not.Null);
		Assert.That(indicatorText, Is.Not.Null);
		if (carouselText is null || indicatorText is null)
		{
			Assert.Fail("Retained control counts were not reported.");
			return;
		}

		const string carouselPrefix = "Retained CarouselViews: ";
		const string indicatorPrefix = "Retained IndicatorViews: ";
		Assert.That(carouselText, Does.StartWith(carouselPrefix));
		Assert.That(indicatorText, Does.StartWith(indicatorPrefix));

		var carouselParts = carouselText[carouselPrefix.Length..].Split('/');
		var indicatorParts = indicatorText[indicatorPrefix.Length..].Split('/');
		Assert.That(carouselParts, Has.Length.EqualTo(2));
		Assert.That(indicatorParts, Has.Length.EqualTo(2));
		Assert.That(int.TryParse(carouselParts[0], out var retainedCarousels), Is.True);
		Assert.That(int.TryParse(indicatorParts[0], out var retainedIndicators), Is.True);
		Assert.That(int.TryParse(carouselParts[1], out var totalCarousels), Is.True);
		Assert.That(int.TryParse(indicatorParts[1], out var totalIndicators), Is.True);
		Assert.That(totalCarousels, Is.EqualTo(2));
		Assert.That(totalIndicators, Is.EqualTo(2));

		Assert.That(
			retainedCarousels == 0 && retainedIndicators == 0,
			Is.True,
			$"Popped CarouselView and IndicatorView controls should be collected after GC; " +
			$"expected CarouselViews 0/2 and IndicatorViews 0/2, but found " +
			$"CarouselViews {retainedCarousels}/{totalCarousels} and " +
			$"IndicatorViews {retainedIndicators}/{totalIndicators}.");
	}
}
#endif

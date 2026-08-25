#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35775 : _IssuesUITest
{
	public Issue35775(TestDevice device) : base(device) { }

	public override string Issue => "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection";

	[Test]
	[Category(UITestCategories.IndicatorView)]
	public void PoppedCarouselViewsUsingSharedObservableCollectionAreCollected()
	{
		IUIElement GetRequiredElement(string automationId)
		{
			var element = App.WaitForElement(automationId);
			if (element is null)
				throw new AssertionException($"Expected to find {automationId}.");

			return element;
		}

		string GetRequiredText(string automationId)
		{
			var element = GetRequiredElement(automationId);
			var text = element.GetText();
			if (text is null)
				throw new AssertionException($"Expected {automationId} to expose text.");

			return text;
		}

		var initialResult = GetRequiredText("Issue35775Result");
		Assert.That(initialResult, Is.EqualTo("Collection check not run"));
		for (int visit = 1; visit <= 5; visit++)
		{
			GetRequiredElement("Issue35775OpenPage");
			App.Tap("Issue35775OpenPage");

			var loadedMarker = GetRequiredText("Issue35775LoadedMarker");
			Assert.That(loadedMarker, Is.EqualTo("Shared observable feed controls"));

			App.Back();

			GetRequiredElement("Issue35775OpenPage");
			var visitCount = GetRequiredText("Issue35775VisitCount");
			Assert.That(visitCount, Is.EqualTo($"Pages visited: {visit}"));
		}

		GetRequiredElement("Issue35775CheckCollection");
		App.Tap("Issue35775CheckCollection");

		var gcCompleted = App.WaitForTextToBePresentInElement(
			"Issue35775GcStatus",
			"GC completed",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(gcCompleted, Is.True, "The bounded garbage-collection check did not complete.");

		var trackedCount = GetRequiredText("Issue35775TrackedCount");
		Assert.That(
			trackedCount,
			Is.EqualTo("Tracked: IndicatorViews=5, CarouselViews=5, behaviors=10"),
			"Every pushed page must contribute one IndicatorView, one CarouselView, and two payload behaviors.");

		var indicatorAlive = GetRequiredText("Issue35775IndicatorAlive");
		var carouselAlive = GetRequiredText("Issue35775CarouselAlive");
		var behaviorAlive = GetRequiredText("Issue35775BehaviorAlive");

		Assert.Multiple(() =>
		{
			Assert.That(
				indicatorAlive,
				Is.EqualTo("IndicatorViews alive: 0"),
				$"IndicatorView leak after five page pop transitions and GC: alive IndicatorViews={indicatorAlive}, expected=IndicatorViews alive: 0.");
			Assert.That(
				carouselAlive,
				Is.EqualTo("CarouselViews alive: 0"),
				$"CarouselView leak after five page pop transitions and GC: alive CarouselViews={carouselAlive}, expected=CarouselViews alive: 0.");
			Assert.That(
				behaviorAlive,
				Is.EqualTo("Payload behaviors alive: 0"),
				$"Payload behavior leak after five page pop transitions and GC: alive behaviors={behaviorAlive}, expected=Payload behaviors alive: 0.");
		});
	}
}
#endif

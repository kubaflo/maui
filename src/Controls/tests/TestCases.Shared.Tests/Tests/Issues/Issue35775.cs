#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35775 : _IssuesUITest
{
	public Issue35775(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection";

	[Test]
	[Category(UITestCategories.IndicatorView)]
	public void PoppedLinkedControlsAreCollectedBeforeSharedFeedMutation()
	{
		Assert.That(App.WaitForTextToBePresentInElement("Issue35775MutationGeneration", "-1"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue35775SourceCount", "120"), Is.True);

		for (int visit = 1; visit <= 4; visit++)
		{
			App.WaitForElement("Issue35775CreateButton");
			App.Tap("Issue35775CreateButton");
			Assert.That(App.WaitForTextToBePresentInElement("Issue35775Ready", "Leak page ready"), Is.True);
			App.Tap("Issue35775PopButton");
			Assert.That(
				App.WaitForTextToBePresentInElement("Issue35775CompletedVisits", visit.ToString(CultureInfo.InvariantCulture)),
				Is.True);
		}

		Assert.That(App.WaitForTextToBePresentInElement("Issue35775MutationGeneration", "-1"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue35775SourceCount", "120"), Is.True);

		App.Tap("Issue35775UpdateButton");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775MutationGeneration",
				"1",
				timeout: TimeSpan.FromSeconds(30)),
			Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue35775SourceCount", "121"), Is.True);
		Assert.That(App.WaitForTextToBePresentInElement("Issue35775CompletedVisits", "4"), Is.True);

		var carouselElement = App.FindElement("Issue35775CarouselAlive");
		var indicatorElement = App.FindElement("Issue35775IndicatorAlive");
		var retiredUpdatesElement = App.FindElement("Issue35775RetiredUpdates");
		var carouselText = carouselElement.GetText();
		var indicatorText = indicatorElement.GetText();
		var retiredUpdatesText = retiredUpdatesElement.GetText();
		if (carouselText is null || indicatorText is null || retiredUpdatesText is null)
			throw new AssertionException("The collection measurements must all contain numeric text.");

		if (!int.TryParse(carouselText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int carouselAlive) ||
			!int.TryParse(indicatorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int indicatorAlive) ||
			!int.TryParse(retiredUpdatesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int retiredUpdates))
			throw new AssertionException("The collection measurements must all be numeric.");

		Assert.That(
			carouselAlive == 0 && indicatorAlive == 0 && retiredUpdates == 0,
			Is.True,
			$"Popped linked controls remained alive after full GC: CarouselViews={carouselAlive}/4, " +
			$"IndicatorViews={indicatorAlive}/4, retiredUpdates={retiredUpdates}; expected " +
			"CarouselViews=0/4, IndicatorViews=0/4, retiredUpdates=0.");
	}
}
#endif

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
	public void PoppedLinkedControlsAreCollectible()
	{
		string GetRequiredText(string automationId)
		{
			var element = App.FindElement(automationId);
			if (element is null)
			{
				Assert.Fail($"Could not find required element '{automationId}'.");
				return string.Empty;
			}

			var text = element.GetText();
			if (text is null)
			{
				Assert.Fail($"Required element '{automationId}' did not expose text.");
				return string.Empty;
			}

			return text;
		}

		App.WaitForElement("Issue35775CreateButton");
		Assert.That(
			GetRequiredText("Issue35775CollectionResultLabel"),
			Is.EqualTo("Collection check pending"));

		for (var visit = 1; visit <= 4; visit++)
		{
			App.Tap("Issue35775CreateButton");
			Assert.That(
				App.WaitForTextToBePresentInElement(
					"Issue35775LoadedLabel",
					$"Loaded linked controls: {visit}",
					timeout: TimeSpan.FromSeconds(10)),
				Is.True,
				$"Linked controls for visit {visit} did not load.");
			App.Tap("Issue35775PopButton");
			App.WaitForElement("Issue35775CreateButton");
		}

		Assert.That(
			GetRequiredText("Issue35775VisitsLabel"),
			Is.EqualTo("Completed visits: 4 of 4"));

		App.Tap("Issue35775CheckButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775CollectionResultLabel",
				"Collection check running",
				timeout: TimeSpan.FromSeconds(2)),
			Is.True,
			"The collection check did not leave its sentinel state.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775CollectionResultLabel",
				"IndicatorViews",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The bounded collection check did not publish a completed result.");

		var collectionResult = GetRequiredText("Issue35775CollectionResultLabel");
		Assert.That(
			collectionResult,
			Is.EqualTo("IndicatorViews 0/4; CarouselViews 0/4"),
			$"Popped linked controls remained alive after bounded GC: {collectionResult}");
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30118 : _IssuesUITest
{
	public override string Issue => "IndicatorView does not visually update when the ItemsSource count changes";

	public Issue30118(TestDevice device)
		: base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void IndicatorViewUpdatesAfterItemsSourceCountChanges()
	{
		var initialCountElement = App.WaitForElement("CountStatus");
		Assert.That(initialCountElement, Is.Not.Null);
		var initialCount = initialCountElement!.GetText();
		Assert.That(initialCount, Is.EqualTo("PAGES: 1"));

		var initialPositionElement = App.WaitForElement("ResultStatus");
		Assert.That(initialPositionElement, Is.Not.Null);
		var initialPosition = initialPositionElement!.GetText();
		Assert.That(initialPosition, Is.EqualTo("POSITION: 0; CALLBACK: -1"));

		App.Tap("IncreaseButton");

		var countUpdated = App.WaitForTextToBePresentInElement(
			"CountStatus",
			"PAGES: 8",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(countUpdated, Is.True);

		App.Tap("IndicatorTouchTarget");

		var targetElement = App.WaitForElement("TargetStatus");
		Assert.That(targetElement, Is.Not.Null);
		var targetText = targetElement!.GetText();
		Assert.That(targetText, Is.EqualTo("TARGET RECEIVED:"));

		var positionChanged = App.WaitForTextToBePresentInElement(
			"ResultStatus",
			"POSITION: 1; CALLBACK: 1",
			timeout: TimeSpan.FromSeconds(10));

		var countElement = App.WaitForElement("CountStatus");
		Assert.That(countElement, Is.Not.Null);
		var countText = countElement!.GetText();
		var positionElement = App.WaitForElement("ResultStatus");
		Assert.That(positionElement, Is.Not.Null);
		var positionText = positionElement!.GetText();

		Assert.That(
			positionChanged,
			Is.True,
			$"Issue30118 IndicatorView position did not advance after the linked ItemsSource grew from 1 to 8. Observed position: {positionText}; expected position: 1; count: {countText}; target: {targetText}");
		Assert.That(
			positionText,
			Is.EqualTo("POSITION: 1; CALLBACK: 1"),
			$"Observed position: {positionText}; expected position: 1; count: {countText}; target: {targetText}");
	}
}
#endif

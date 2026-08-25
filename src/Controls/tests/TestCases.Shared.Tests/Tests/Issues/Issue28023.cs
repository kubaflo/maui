using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if IOS
public class Issue28023 : _IssuesUITest
{
	const double GapTolerance = 2;
	const double RowHeight = 54;

	public Issue28023(TestDevice device) : base(device) { }

	public override string Issue => "ItemSpacing is retained after re-entering a CollectionView page";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ReEnteringVerticalListResetsItemSpacing()
	{
		App.WaitForElement("VerticalListCell");
		App.Tap("VerticalListCell");
		WaitForText("PageInstance", "Page instance: 1");
		Assert.That(App.WaitForElement("SpacingEntry").GetText(), Is.EqualTo("0"));

		var initialGap = GetVerifiedRowGap();
		Assert.That(initialGap, Is.EqualTo(0).Within(GapTolerance), "The initial vertical CollectionView item gap should be 0.");

		App.ClearText("SpacingEntry");
		App.EnterText("SpacingEntry", "90");
		App.Tap("UpdateSpacingButton");

		WaitForText("CurrentSpacing", "Current spacing: 90");
		App.RetryAssert(() =>
		{
			Assert.That(GetVerifiedRowGap(), Is.EqualTo(90).Within(GapTolerance),
				"The attached vertical CollectionView should render the updated 90-point item gap.");
		});

		App.TapBackArrow();
		App.WaitForElement("VerticalListCell");
		App.Tap("VerticalListCell");

		WaitForText("PageInstance", "Page instance: 2");
		Assert.That(App.WaitForElement("SpacingEntry").GetText(), Is.EqualTo("0"),
			"The newly constructed page should restore the default Entry value.");

		App.RetryAssert(() =>
		{
			var reenteredGap = GetVerifiedRowGap();
			Assert.That(reenteredGap, Is.EqualTo(0).Within(GapTolerance),
				$"Re-entered vertical CollectionView item gap should reset to 0; actual gap was {reenteredGap:0.##}.");
		});
	}

	void WaitForText(string automationId, string expectedText)
	{
		App.RetryAssert(() =>
		{
			var element = App.WaitForElement(automationId);
			Assert.That(element.GetText(), Is.EqualTo(expectedText));
		});
	}

	double GetVerifiedRowGap()
	{
		var collectionRect = App.WaitForElement("MonkeyCollection").GetRect();
		var firstRowLabel = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeStaticText[@label='Baboon - Africa']"));
		var secondRowLabel = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeStaticText[@label='Capuchin Monkey - South America']"));
		var firstRowLabelRect = firstRowLabel.GetRect();
		var secondRowLabelRect = secondRowLabel.GetRect();

		Assert.Multiple(() =>
		{
			Assert.That(firstRowLabel.GetText(), Is.EqualTo("Baboon - Africa"));
			Assert.That(secondRowLabel.GetText(), Is.EqualTo("Capuchin Monkey - South America"));
			Assert.That(collectionRect.Width, Is.GreaterThan(0));
			Assert.That(collectionRect.Height, Is.GreaterThan(0));
			Assert.That(firstRowLabelRect.Width, Is.GreaterThan(0));
			Assert.That(firstRowLabelRect.Height, Is.GreaterThan(0));
			Assert.That(secondRowLabelRect.Width, Is.GreaterThan(0));
			Assert.That(secondRowLabelRect.Height, Is.GreaterThan(0));
			Assert.That(firstRowLabelRect.Left, Is.GreaterThanOrEqualTo(collectionRect.Left));
			Assert.That(firstRowLabelRect.Top, Is.GreaterThanOrEqualTo(collectionRect.Top));
			Assert.That(firstRowLabelRect.Right, Is.LessThanOrEqualTo(collectionRect.Right));
			Assert.That(firstRowLabelRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom));
			Assert.That(secondRowLabelRect.Left, Is.GreaterThanOrEqualTo(collectionRect.Left));
			Assert.That(secondRowLabelRect.Top, Is.GreaterThan(firstRowLabelRect.Top));
			Assert.That(secondRowLabelRect.Right, Is.LessThanOrEqualTo(collectionRect.Right));
			Assert.That(secondRowLabelRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom));
		});

		return secondRowLabelRect.Top - firstRowLabelRect.Top - RowHeight;
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28023 : _IssuesUITest
{
	const string FirstItemId = "Issue28023Item_Baboon";
	const string SecondItemId = "Issue28023Item_Capuchin Monkey";
	const int GapTolerance = 2;

	public Issue28023(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView retains ItemSpacing after navigating back and re-entering";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ItemSpacingResetsAfterNavigatingBackAndReEntering()
	{
		App.WaitForElement("Issue28023VerticalListCell");
		App.Tap("Issue28023VerticalListCell");

		var initialEntry = App.WaitForElement("Issue28023SpacingEntry");
		if (initialEntry is null)
		{
			Assert.Fail("Issue28023 initial spacing Entry was not found.");
			return;
		}

		var initialEntryText = initialEntry.GetText();
		if (initialEntryText is null)
		{
			Assert.Fail("Issue28023 initial spacing Entry text was null.");
			return;
		}

		Assert.That(initialEntryText, Is.EqualTo("0"));

		var initialFirstItem = App.WaitForElement(FirstItemId);
		var initialSecondItem = App.WaitForElement(SecondItemId);
		if (initialFirstItem is null || initialSecondItem is null)
		{
			Assert.Fail("Issue28023 initial adjacent monkey items were not found.");
			return;
		}

		var initialFirstRect = initialFirstItem.GetRect();
		var initialSecondRect = initialSecondItem.GetRect();
		Assert.That(initialSecondRect.Y, Is.GreaterThan(initialFirstRect.Y),
			"Issue28023 Capuchin Monkey item should be below the Baboon item.");
		int initialGap = initialSecondRect.Y - (initialFirstRect.Y + initialFirstRect.Height);
		Assert.That(initialGap, Is.EqualTo(0).Within(GapTolerance),
			$"Issue28023 initial item edge gap was {initialGap}px instead of 0px.");

		App.ClearText("Issue28023SpacingEntry");
		App.EnterText("Issue28023SpacingEntry", "90");
		App.Tap("Issue28023UpdateButton");

		var updatedEntry = App.WaitForElement("Issue28023SpacingEntry");
		if (updatedEntry is null)
		{
			Assert.Fail("Issue28023 updated spacing Entry was not found.");
			return;
		}

		var updatedEntryText = updatedEntry.GetText();
		if (updatedEntryText is null)
		{
			Assert.Fail("Issue28023 updated spacing Entry text was null.");
			return;
		}

		Assert.That(updatedEntryText, Is.EqualTo("90"));

		int updatedGap = int.MinValue;
		App.RetryAssert(() =>
		{
			var updatedFirstItem = App.WaitForElement(FirstItemId);
			var updatedSecondItem = App.WaitForElement(SecondItemId);
			if (updatedFirstItem is null || updatedSecondItem is null)
			{
				Assert.Fail("Issue28023 updated adjacent monkey items were not found.");
				return;
			}

			var firstRect = updatedFirstItem.GetRect();
			var secondRect = updatedSecondItem.GetRect();
			Assert.That(secondRect.Y, Is.GreaterThan(firstRect.Y),
				"Issue28023 updated Capuchin Monkey item should be below the Baboon item.");
			updatedGap = secondRect.Y - (firstRect.Y + firstRect.Height);
			Assert.That(updatedGap, Is.GreaterThan(GapTolerance));
			Assert.That(updatedGap, Is.Not.EqualTo(initialGap));
		});
		Assert.That(updatedGap, Is.Not.EqualTo(int.MinValue), "Issue28023 updated item gap was not observed.");

		this.Back();
		App.WaitForElement("Issue28023VerticalListCell");
		App.Tap("Issue28023VerticalListCell");

		string secondVisitEntryText = "<not observed>";
		int secondVisitGap = int.MinValue;
		var secondVisitEntry = App.WaitForElement("Issue28023SpacingEntry");
		if (secondVisitEntry is null)
		{
			Assert.Fail("Issue28023 second-visit spacing Entry was not found.");
			return;
		}

		var observedEntryText = secondVisitEntry.GetText();
		if (observedEntryText is null)
		{
			Assert.Fail("Issue28023 second-visit spacing Entry text was null.");
			return;
		}

		secondVisitEntryText = observedEntryText;
		Assert.That(secondVisitEntryText, Is.EqualTo("0"));

		App.RetryAssert(() =>
		{
			var secondVisitFirstItem = App.WaitForElement(FirstItemId);
			var secondVisitSecondItem = App.WaitForElement(SecondItemId);
			if (secondVisitFirstItem is null || secondVisitSecondItem is null)
			{
				Assert.Fail("Issue28023 second-visit adjacent monkey items were not found.");
				return;
			}

			var firstRect = secondVisitFirstItem.GetRect();
			var secondRect = secondVisitSecondItem.GetRect();
			Assert.That(secondRect.Y, Is.GreaterThan(firstRect.Y),
				"Issue28023 re-entered Capuchin Monkey item should be below the Baboon item.");
			secondVisitGap = secondRect.Y - (firstRect.Y + firstRect.Height);
			Assert.That(secondVisitGap, Is.EqualTo(0).Within(GapTolerance),
				$"Issue28023 re-entered item edge gap was {secondVisitGap}px instead of 0px.");
		});
		Assert.That(secondVisitGap, Is.Not.EqualTo(int.MinValue), "Issue28023 second-visit item gap was not observed.");
	}
}
#endif

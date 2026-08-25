#if IOS
using System.Drawing;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28023 : _IssuesUITest
{
	public Issue28023(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ItemSpacing is retained when opening a fresh CollectionView";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FreshVerticalListUsesDefaultItemSpacing()
	{
		const double tolerance = 2;

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(OpenQA.Selenium.ScreenOrientation.Portrait));

		App.WaitForElement("ItemSpacingGallery");
		App.Tap("OpenVerticalList");

		var firstVisit = App.WaitForElement("VisitMarker");
		var firstVisitText = firstVisit.GetText();
		Assert.That(firstVisitText, Is.Not.Null);
		Assert.That(firstVisitText, Is.EqualTo("Visit: 1"));

		var firstEntry = App.WaitForElement("SpacingEntry");
		var firstEntryText = firstEntry.GetText();
		Assert.That(firstEntryText, Is.Not.Null);
		Assert.That(firstEntryText, Is.EqualTo("0"));

		var collectionRect = App.WaitForElement("MonkeyCollection").GetRect();
		var firstRect = App.WaitForElement("Monkey 1").GetRect();
		var secondRect = App.WaitForElement("Monkey 2").GetRect();
		AssertItemIsRenderedInCollection(firstRect, collectionRect, tolerance);
		AssertItemIsRenderedInCollection(secondRect, collectionRect, tolerance);
		Assert.That(GetGap(firstRect, secondRect), Is.EqualTo(0).Within(tolerance),
			$"Initial Monkey spacing was not zero. Monkey 1 rectangle {firstRect}; Monkey 2 rectangle {secondRect}.");

		App.ClearText("SpacingEntry");
		App.EnterText("SpacingEntry", "90");
		var updatedEntryText = App.WaitForElement("SpacingEntry").GetText();
		Assert.That(updatedEntryText, Is.Not.Null);
		Assert.That(updatedEntryText, Is.EqualTo("90"));
		App.Tap("UpdateSpacing");
		WaitForGap(90, tolerance);

		var updatedCollectionRect = App.WaitForElement("MonkeyCollection").GetRect();
		var updatedFirstRect = App.WaitForElement("Monkey 1").GetRect();
		var updatedSecondRect = App.WaitForElement("Monkey 2").GetRect();
		AssertItemIsRenderedInCollection(updatedFirstRect, updatedCollectionRect, tolerance);
		AssertItemIsRenderedInCollection(updatedSecondRect, updatedCollectionRect, tolerance);
		Assert.That(GetGap(updatedFirstRect, updatedSecondRect), Is.EqualTo(90).Within(tolerance));

		App.Tap("ReturnToGallery");
		App.WaitForElement("ItemSpacingGallery");
		App.Tap("OpenVerticalList");

		var secondVisit = App.WaitForElement("VisitMarker");
		var secondVisitText = secondVisit.GetText();
		Assert.That(secondVisitText, Is.Not.Null);
		Assert.That(secondVisitText, Is.EqualTo("Visit: 2"));

		var freshEntry = App.WaitForElement("SpacingEntry");
		var freshEntryText = freshEntry.GetText();
		Assert.That(freshEntryText, Is.Not.Null);
		Assert.That(freshEntryText, Is.EqualTo("0"));

		var freshCollectionRect = App.WaitForElement("MonkeyCollection").GetRect();
		var freshFirstRect = App.WaitForElement("Monkey 1").GetRect();
		var freshSecondRect = App.WaitForElement("Monkey 2").GetRect();
		AssertItemIsRenderedInCollection(freshFirstRect, freshCollectionRect, tolerance);
		AssertItemIsRenderedInCollection(freshSecondRect, freshCollectionRect, tolerance);

		double freshGap = GetGap(freshFirstRect, freshSecondRect);
		Assert.That(freshGap, Is.EqualTo(0).Within(tolerance),
			$"Fresh vertical Monkey list expected zero spacing; measured native gap {freshGap}; expected gap 0; Monkey 1 rectangle {freshFirstRect}; Monkey 2 rectangle {freshSecondRect}; tolerance {tolerance}.");
	}

	void WaitForGap(double expectedGap, double tolerance)
	{
		App.WaitForElement(() =>
		{
			var firstItem = App.FindElement("Monkey 1");
			var secondItem = App.FindElement("Monkey 2");
			if (firstItem is null || secondItem is null)
			{
				return null;
			}

			double gap = GetGap(firstItem.GetRect(), secondItem.GetRect());
			return Math.Abs(gap - expectedGap) <= tolerance ? secondItem : null;
		}, $"Timed out waiting for the Monkey item gap to become {expectedGap}.");
	}

	static void AssertItemIsRenderedInCollection(Rectangle itemRect, Rectangle collectionRect, double tolerance)
	{
		Assert.Multiple(() =>
		{
			Assert.That(itemRect.Height, Is.EqualTo(100).Within(tolerance));
			Assert.That(itemRect.Left, Is.GreaterThanOrEqualTo(collectionRect.Left - tolerance));
			Assert.That(itemRect.Right, Is.LessThanOrEqualTo(collectionRect.Right + tolerance));
			Assert.That(itemRect.Top, Is.GreaterThanOrEqualTo(collectionRect.Top - tolerance));
			Assert.That(itemRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom + tolerance));
		});
	}

	static double GetGap(Rectangle firstRect, Rectangle secondRect)
	{
		return secondRect.Top - firstRect.Bottom;
	}
}
#endif

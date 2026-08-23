#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36545 : _IssuesUITest
{
	const double ExpectedSpacing = 30;
	const double Tolerance = 1;

	public Issue36545(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Grouped CollectionView with GridItemsLayout omits spacing after group header";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void GroupHeaderUsesConfiguredVerticalItemSpacing()
	{
		App.SetOrientationPortrait();

		var initialRoot = App.WaitForElement("InitialRoot").GetRect();
		Assert.That(initialRoot.Height, Is.GreaterThan(initialRoot.Width), "The issue must be exercised in portrait orientation.");
		Assert.That(App.FindElements("TestCollectionView").Count, Is.Zero, "The CollectionView must be created by the post-attachment button action.");
		double measuredHeaderGap = -1;

		App.Tap("ShowIssueButton");

		var collectionRect = App.WaitForElement("TestCollectionView").GetRect();
		var headerRect = App.WaitForElement("Group100s").GetRect();
		var item100Rect = App.WaitForElement("Item100").GetRect();
		var item200Rect = App.WaitForElement("Item200").GetRect();
		var item300Rect = App.WaitForElement("Item300").GetRect();
		var item400Rect = App.WaitForElement("Item400").GetRect();
		var item500Rect = App.WaitForElement("Item500").GetRect();
		var item600Rect = App.WaitForElement("Item600").GetRect();

		var renderedRects = new[]
		{
			headerRect,
			item100Rect,
			item200Rect,
			item300Rect,
			item400Rect,
			item500Rect,
			item600Rect,
		};
		foreach (var rect in renderedRects)
		{
			Assert.That(rect.Width, Is.GreaterThan(0));
			Assert.That(rect.Height, Is.GreaterThan(0));
			Assert.That(rect.X, Is.GreaterThanOrEqualTo(collectionRect.X - Tolerance));
			Assert.That(rect.Y, Is.GreaterThanOrEqualTo(collectionRect.Y - Tolerance));
			Assert.That(rect.X + rect.Width, Is.LessThanOrEqualTo(collectionRect.X + collectionRect.Width + Tolerance));
			Assert.That(rect.Y + rect.Height, Is.LessThanOrEqualTo(collectionRect.Y + collectionRect.Height + Tolerance));
		}

		Assert.That(item200Rect.Y, Is.EqualTo(item100Rect.Y).Within(Tolerance));
		Assert.That(item300Rect.Y, Is.EqualTo(item100Rect.Y).Within(Tolerance));
		Assert.That(item400Rect.Y, Is.EqualTo(item100Rect.Y).Within(Tolerance));
		Assert.That(item500Rect.Y, Is.EqualTo(item100Rect.Y).Within(Tolerance));
		Assert.That(item600Rect.Y, Is.GreaterThan(item100Rect.Y + item100Rect.Height));

		var measuredRowGap = item600Rect.Y - (item100Rect.Y + item100Rect.Height);
		Assert.That(measuredRowGap, Is.EqualTo(ExpectedSpacing).Within(Tolerance),
			$"The ordinary grid row gap was {measuredRowGap:F2}; expected {ExpectedSpacing:F2} +/- {Tolerance:F2}.");

		measuredHeaderGap = item100Rect.Y - (headerRect.Y + headerRect.Height);
		Assert.That(measuredHeaderGap, Is.EqualTo(ExpectedSpacing).Within(Tolerance),
			$"Issue36545 header-to-first-row gap: observed {measuredHeaderGap:F2}, expected {ExpectedSpacing:F2} +/- {Tolerance:F2}; header={headerRect}, item100={item100Rect}.");
	}
}
#endif

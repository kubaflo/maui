#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33401 : _IssuesUITest
{
	public Issue33401(TestDevice device) : base(device)
	{
	}

	public override string Issue => "CollectionView SelectionChanged is not fired on iOS when inside a Grid with a TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappingItemRaisesSelectionChangedWithAncestorTapGestureRecognizer()
	{
		if (App is not AppiumIOSApp iosApp || !HelperExtensions.IsIOS26OrHigher(iosApp))
			return;

		int gridTapCount = -1;
		int selectionChangedCount = -1;

		App.WaitForElement("AlphaItem");
		gridTapCount = ReadCount("GridTapCountLabel", "Grid taps: ");
		selectionChangedCount = ReadCount("SelectionChangedCountLabel", "SelectionChanged: ");
		Assert.That(gridTapCount, Is.Zero, "The Grid tap count must start at zero.");
		Assert.That(selectionChangedCount, Is.Zero, "The SelectionChanged count must start at zero.");

		App.Tap("AlphaItem");

		App.WaitForTextToBePresentInElement("GridTapCountLabel", "Grid taps: 1", timeout: TimeSpan.FromSeconds(3));
		gridTapCount = ReadCount("GridTapCountLabel", "Grid taps: ");
		Assert.That(gridTapCount, Is.EqualTo(1), "The ancestor Grid must receive the Alpha item tap exactly once.");

		App.WaitForTextToBePresentInElement("SelectionChangedCountLabel", "SelectionChanged: 1", timeout: TimeSpan.FromSeconds(3));
		selectionChangedCount = ReadCount("SelectionChangedCountLabel", "SelectionChanged: ");
		Assert.That(
			selectionChangedCount,
			Is.EqualTo(1),
			$"SelectionChanged count after tapping Alpha was {selectionChangedCount}; Grid tap count was {gridTapCount}.");
	}

	int ReadCount(string automationId, string prefix)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"The text for {automationId} was null.");

		if (!text.StartsWith(prefix, StringComparison.Ordinal) ||
			!int.TryParse(text.AsSpan(prefix.Length), out int count))
		{
			throw new AssertionException($"The text for {automationId} was '{text}', not '{prefix}<count>'.");
		}

		return count;
	}
}
#endif

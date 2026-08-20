#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33401 : _IssuesUITest
{
	public Issue33401(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView SelectionChanged is not fired inside a Grid with a TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappingItemRaisesParentTapAndSelectionChanged()
	{
		if (App is AppiumIOSApp iosApp && !HelperExtensions.IsIOS26OrHigher(iosApp))
			return;

		App.WaitForElement("First item");
		App.WaitForElement("ParentTapCount");
		App.WaitForElement("SelectionChangedCount");

		int initialParentTapCount = -1;
		int initialSelectionChangedCount = -1;
		initialParentTapCount = GetCount("ParentTapCount");
		initialSelectionChangedCount = GetCount("SelectionChangedCount");

		Assert.Multiple(() =>
		{
			Assert.That(initialParentTapCount, Is.Zero, "The parent tap count should start at 0.");
			Assert.That(initialSelectionChangedCount, Is.Zero, "The selection changed count should start at 0.");
		});

		App.Tap("First item");

		_ = App.WaitForTextToBePresentInElement("ParentTapCount", "Parent tap count: 1", TimeSpan.FromSeconds(5));
		_ = App.WaitForTextToBePresentInElement("SelectionChangedCount", "Selection changed count: 1", TimeSpan.FromSeconds(5));

		int parentTapCount = -1;
		int selectionChangedCount = -1;
		parentTapCount = GetCount("ParentTapCount");
		selectionChangedCount = GetCount("SelectionChangedCount");

		Assert.Multiple(() =>
		{
			Assert.That(parentTapCount, Is.EqualTo(1), $"Parent tap count after first-item tap was {parentTapCount}; expected 1.");
			Assert.That(
				selectionChangedCount,
				Is.EqualTo(1),
				$"CollectionView selection count after first-item tap was {selectionChangedCount}; expected 1 after parent tap count reached {parentTapCount}.");
		});
	}

	int GetCount(string automationId)
	{
		var text = App.FindElement(automationId).GetText();
		var separatorIndex = text?.LastIndexOf(':') ?? -1;

		Assert.That(separatorIndex, Is.GreaterThanOrEqualTo(0), $"{automationId} should contain a ':' separator.");
		return int.Parse(text![(separatorIndex + 1)..].Trim(), CultureInfo.InvariantCulture);
	}
}
#endif

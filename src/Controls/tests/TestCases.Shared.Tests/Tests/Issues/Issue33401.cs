#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33401 : _IssuesUITest
{
	public Issue33401(TestDevice device) : base(device) { }

	public override string Issue => "CollectionView SelectionChanged is suppressed by an ancestor TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappingItemRaisesSelectionChangedWithAncestorTapGestureRecognizer()
	{
		App.WaitForElement("First item");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33401ReadyStatus", "Ready; Selected item: <null>"),
			Is.True,
			"The CollectionView should initially have no selected item.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33401InteractionStatus", "Parent taps: 0; Selection changes: 0"),
			Is.True,
			"The interaction counts should initially be zero.");

		int parentTapCount = -1;
		int selectionChangedCount = -1;

		App.Tap("First item");

		bool parentTapTransitioned = App.WaitForTextToBePresentInElement(
			"Issue33401InteractionStatus",
			"Parent taps: 1",
			timeout: TimeSpan.FromSeconds(10));
		bool selectionChangedTransitioned = App.WaitForTextToBePresentInElement(
			"Issue33401InteractionStatus",
			"Selection changes: 1",
			timeout: TimeSpan.FromSeconds(10));

		var interactionStatus = App.FindElement("Issue33401InteractionStatus").GetText()
			?? throw new InvalidOperationException("The interaction status text was unavailable.");
		parentTapCount = ReadCount(interactionStatus, "Parent taps: ");
		selectionChangedCount = ReadCount(interactionStatus, "Selection changes: ");

		Assert.That(parentTapTransitioned, Is.True, $"Parent Grid tap count was {parentTapCount}; expected 1.");
		Assert.That(parentTapCount, Is.EqualTo(1), $"Parent Grid tap count was {parentTapCount}; expected 1.");
		Assert.That(
			selectionChangedTransitioned && selectionChangedCount == 1,
			Is.True,
			$"SelectionChanged count after tapping first CollectionView item was {selectionChangedCount}; expected 1.");
	}

	static int ReadCount(string status, string marker)
	{
		int valueStart = status.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
		int valueEnd = status.IndexOf(';', valueStart);
		string value = valueEnd < 0 ? status[valueStart..] : status[valueStart..valueEnd];
		return int.Parse(value);
	}
}
#endif

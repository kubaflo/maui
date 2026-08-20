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

	public override string Issue => "CollectionView SelectionChanged is not fired inside a Grid with a TapGestureRecognizer";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ItemTapRaisesSelectionChanged()
	{
		if (App is not AppiumIOSApp iosApp || !HelperExtensions.IsIOS26OrHigher(iosApp))
		{
			return;
		}

		App.SetOrientationPortrait();

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33401AttachedState", "0", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The CollectionView page did not reach its attached state.");

		var item = App.WaitForElement("Issue33401Item");
		Assert.That(item.GetText(), Is.EqualTo("Collection item"));
		Assert.That(App.FindElement("Issue33401GridTapCount").GetText(), Is.EqualTo("0"));
		Assert.That(App.FindElement("Issue33401SelectionChangedCount").GetText(), Is.EqualTo("0"));

		App.Tap("Issue33401Item");

		bool gridTapObserved = App.WaitForTextToBePresentInElement(
			"Issue33401GridTapCount",
			"1",
			timeout: TimeSpan.FromSeconds(5));
		Assert.That(gridTapObserved, Is.True, "Parent Grid tap was not observed after tapping the CollectionView item.");

		_ = App.WaitForTextToBePresentInElement(
			"Issue33401SelectionChangedCount",
			"1",
			timeout: TimeSpan.FromSeconds(3));

		int gridTapCount = int.Parse(App.FindElement("Issue33401GridTapCount").GetText() ?? "-1");
		int selectionChangedCount = int.Parse(App.FindElement("Issue33401SelectionChangedCount").GetText() ?? "-1");
		Assert.That(
			selectionChangedCount,
			Is.EqualTo(1),
			$"SelectionChanged count after item tap was {selectionChangedCount}; expected 1 after Grid tap count reached {gridTapCount}");
	}
}
#endif

#if IOS
using System.Diagnostics;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27347 : _IssuesUITest
{
	public override string Issue => "MultiBinding converters are not triggered on ObservableCollection changes";

	public Issue27347(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void AddButtonHidesAfterFinalAttachmentIsDeleted()
	{
		var initialCountElement = App.WaitForElement("CollectionCount");
		if (initialCountElement is null)
			throw new AssertionException("The collection count sentinel was not found.");

		Assert.That(initialCountElement.GetText(), Is.EqualTo("-1"));
		Assert.That(App.FindElements("AddButton").Count, Is.EqualTo(0));

		App.Tap("ToggleDataButton");
		App.WaitForElement("Attachment 1");
		App.WaitForElement("Attachment 2");
		Assert.That(App.FindElements("DeleteButton").Count, Is.EqualTo(2));

		App.Tap("ToggleEditModeButton");
		App.WaitForElement("AddButton");
		Assert.That(App.FindElements("AddButton").Count, Is.EqualTo(1));

		App.Tap("DeleteButton");

		var countAfterFirstDelete = App.FindElement("CollectionCount");
		if (countAfterFirstDelete is null)
			throw new AssertionException("The collection count was not found after the first deletion.");

		Assert.That(countAfterFirstDelete.GetText(), Is.EqualTo("1"));
		Assert.That(App.FindElements("DeleteButton").Count, Is.EqualTo(1));

		App.Tap("DeleteButton");

		var finalCountElement = App.FindElement("CollectionCount");
		if (finalCountElement is null)
			throw new AssertionException("The collection count was not found after the final deletion.");

		Assert.That(finalCountElement.GetText(), Is.EqualTo("0"));
		App.WaitForElement("EmptyStateLabel");

		var stopwatch = Stopwatch.StartNew();
		int visibleAddButtonCount;
		do
		{
			visibleAddButtonCount = App.FindElements("AddButton").Count;
			if (visibleAddButtonCount == 0)
				break;
		}
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(2));

		Assert.That(
			visibleAddButtonCount,
			Is.EqualTo(0),
			$"Issue27347 Add button remained visible after the final attachment was deleted; observed count={visibleAddButtonCount}, expected count=0, callback count={finalCountElement.GetText()}");
	}
}
#endif

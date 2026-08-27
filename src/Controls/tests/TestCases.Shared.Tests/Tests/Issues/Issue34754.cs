#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34754 : _IssuesUITest
{
	public Issue34754(TestDevice device) : base(device)
	{
	}

	public override string Issue => "WinUI drag and drop and CanMixGroups support was not available";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void CanDragAnItemBetweenGroups()
	{
		string GetText(string automationId)
		{
			var element = App.WaitForElement(automationId);
			if (element is null)
				throw new AssertionException($"Element '{automationId}' was not found.");

			var text = element.GetText();
			if (text is null)
				throw new AssertionException($"Element '{automationId}' did not contain text.");

			return text;
		}

		App.WaitForElement("Issue34754GroupedCollectionView");
		App.WaitForElement("Issue34754Alpha");
		App.WaitForElement("Issue34754Beta");
		App.WaitForElement("Issue34754Gamma");
		App.WaitForElement("Issue34754Delta");

		Assert.That(GetText("Issue34754GroupOneSequence"), Is.EqualTo("Group One: Alpha,Beta"));
		Assert.That(GetText("Issue34754GroupTwoSequence"), Is.EqualTo("Group Two: Gamma,Delta"));
		Assert.That(GetText("Issue34754CollectionChangeCount"), Is.EqualTo("Collection changes: -1"));

		App.Tap("Issue34754Alpha");
		var inputObserved = App.WaitForTextToBePresentInElement(
			"Issue34754InputStatus",
			"INPUT CONFIRMED: Alpha tapped",
			timeout: TimeSpan.FromSeconds(3));
		Assert.That(inputObserved, Is.True, "Alpha did not receive the pointer input.");

		App.DragAndDrop("Issue34754Alpha", "Issue34754Delta");

		var callbackObserved = App.WaitForTextToBePresentInElement(
			"Issue34754CollectionChangeCount",
			"post-drag callback observed",
			timeout: TimeSpan.FromSeconds(3));
		var observedGroupTwo = GetText("Issue34754GroupTwoSequence");
		Assert.That(callbackObserved, Is.True,
			$"Cross-group drag did not move Alpha into Group Two. Expected 'Group Two: Gamma,Delta,Alpha', observed '{observedGroupTwo}'.");

		App.WaitForTextToBePresentInElement(
			"Issue34754GroupTwoSequence",
			"Group Two: Gamma,Delta,Alpha",
			timeout: TimeSpan.FromSeconds(3));
		observedGroupTwo = GetText("Issue34754GroupTwoSequence");
		Assert.That(observedGroupTwo, Is.EqualTo("Group Two: Gamma,Delta,Alpha"),
			$"Cross-group drag did not move Alpha into Group Two. Expected 'Group Two: Gamma,Delta,Alpha', observed '{observedGroupTwo}'.");
	}
}
#endif

using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

#if WINDOWS
public class Issue34754 : _IssuesUITest
{
	public Issue34754(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "WinUI drag and drop and CanMixGroups support was not available";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DraggingAnItemBetweenGroupsMovesTheSourceItem()
	{
		var sourceBeforeDrag = App.WaitForElement("SourceItem").GetRect();
		var targetBeforeDrag = App.WaitForElement("TargetItem").GetRect();
		var groupBHeader = App.WaitForElement("Group B").GetRect();

		Assert.That(sourceBeforeDrag.Y, Is.LessThan(groupBHeader.Y), "SOURCE ITEM should initially be rendered in Group A.");
		Assert.That(targetBeforeDrag.Y, Is.GreaterThan(groupBHeader.Y), "TARGET ITEM should initially be rendered in Group B.");

		App.DragAndDrop("SourceItem", "TargetItem");

		var sourceAfterDrag = App.WaitForElement("SourceItem").GetRect();
		Assert.That(
			sourceAfterDrag.Y,
			Is.GreaterThan(groupBHeader.Y),
			"SOURCE ITEM should be rendered below the Group B header after cross-group drag.");
	}
}
#endif

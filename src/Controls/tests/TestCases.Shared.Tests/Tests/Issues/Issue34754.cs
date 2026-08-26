#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34754 : _IssuesUITest
{
	public Issue34754(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "WinUI Drag and Drop and CanMixGroups support was not available";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void GroupedDragMovesItemBetweenGroups()
	{
		var window = App.FindElement(AppiumQuery.ByXPath("/*"));
		var collection = App.WaitForElement("Issue34754CollectionView");
		var alpha = App.WaitForElement("Issue34754Alpha");
		var gamma = App.WaitForElement("Issue34754Gamma");

		var alphaText = alpha.GetText();
		var gammaText = gamma.GetText();
		Assert.That(alphaText, Is.Not.Null);
		Assert.That(gammaText, Is.Not.Null);
		Assert.That(alphaText, Is.EqualTo("Alpha"));
		Assert.That(gammaText, Is.EqualTo("Gamma"));

		var windowRect = window.GetRect();
		var alphaRect = alpha.GetRect();
		var gammaRect = gamma.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(alphaRect.Left, Is.GreaterThanOrEqualTo(windowRect.Left));
			Assert.That(alphaRect.Top, Is.GreaterThanOrEqualTo(windowRect.Top));
			Assert.That(alphaRect.Right, Is.LessThanOrEqualTo(windowRect.Right));
			Assert.That(alphaRect.Bottom, Is.LessThanOrEqualTo(windowRect.Bottom));
			Assert.That(gammaRect.Left, Is.GreaterThanOrEqualTo(windowRect.Left));
			Assert.That(gammaRect.Top, Is.GreaterThan(alphaRect.Top));
			Assert.That(gammaRect.Right, Is.LessThanOrEqualTo(windowRect.Right));
			Assert.That(gammaRect.Bottom, Is.LessThanOrEqualTo(windowRect.Bottom));
			Assert.That(collection.GetRect().Height, Is.GreaterThan(0));
		});

		var handlerReady = App.WaitForTextToBePresentInElement(
			"Issue34754HandlerPath",
			"Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler",
			TimeSpan.FromSeconds(5));
		Assert.That(handlerReady, Is.True, "The Windows CollectionView handler was not attached.");

		Assert.Multiple(() =>
		{
			Assert.That(App.WaitForElement("Issue34754Group1State").GetText(), Is.EqualTo("Group 1=[Alpha,Beta]"));
			Assert.That(App.WaitForElement("Issue34754Group1Count").GetText(), Is.EqualTo("Group 1 Count=2"));
			Assert.That(App.WaitForElement("Issue34754Group2State").GetText(), Is.EqualTo("Group 2=[Gamma,Delta]"));
			Assert.That(App.WaitForElement("Issue34754Group2Count").GetText(), Is.EqualTo("Group 2 Count=2"));
			Assert.That(App.WaitForElement("Issue34754PointerCount").GetText(), Is.EqualTo("Alpha Pointer Count=0"));
			Assert.That(App.WaitForElement("Issue34754Hierarchy").GetText(), Is.EqualTo("Hierarchy=ContentPage>Grid>CollectionView"));
			Assert.That(App.WaitForElement("Issue34754HandlerPath").GetText(),
				Is.EqualTo("Handler=Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler"));
		});

		App.DragAndDrop("Issue34754Alpha", "Issue34754Gamma");

		var pointerReachedAlpha = App.WaitForTextToBePresentInElement(
			"Issue34754PointerCount", "Alpha Pointer Count=1", TimeSpan.FromSeconds(5));
		Assert.That(pointerReachedAlpha, Is.True, "The drag pointer input did not reach Alpha.");

		App.WaitForTextToBePresentInElement(
			"Issue34754Group1State", "Group 1=[Beta]", TimeSpan.FromSeconds(3));
		App.WaitForTextToBePresentInElement(
			"Issue34754Group2State", "Alpha", TimeSpan.FromSeconds(3));

		var firstState = App.WaitForElement("Issue34754Group1State").GetText();
		var firstCount = App.WaitForElement("Issue34754Group1Count").GetText();
		var secondState = App.WaitForElement("Issue34754Group2State").GetText();
		var secondCount = App.WaitForElement("Issue34754Group2Count").GetText();
		if (firstState is null || firstCount is null || secondState is null || secondCount is null)
		{
			Assert.Fail("The grouped source-state labels must all expose text.");
			return;
		}

		bool movedAcrossGroups =
			firstState == "Group 1=[Beta]" &&
			firstCount == "Group 1 Count=1" &&
			secondState.Contains("Alpha", StringComparison.Ordinal) &&
			secondCount == "Group 2 Count=3";

		Assert.That(movedAcrossGroups, Is.True,
			$"Grouped drag should move Alpha from Group 1 to Group 2; observed {firstState}, {firstCount}, {secondState}, {secondCount}");
	}
}
#endif

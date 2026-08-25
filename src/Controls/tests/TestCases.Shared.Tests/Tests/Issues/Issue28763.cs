#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28763 : _IssuesUITest
{
	public override string Issue => "Multiple notifications for SelectionChanged in a CollectionView when the view model is added with addSingleton";

	public Issue28763(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectionChangedCommandExecutesOnceAfterReturningToSingletonViewModel()
	{
		string GetText(string automationId)
		{
			var element = App.WaitForElement(automationId);
			if (element is null)
				throw new AssertionException($"Element '{automationId}' was not found.");

			var text = element.GetText();
			if (text is null)
				throw new AssertionException($"Element '{automationId}' did not expose text.");

			return text;
		}

		App.WaitForElement("Task1Text");
		App.Tap("Task1Text");
		App.WaitForElement("DetailItem1");
		App.Tap("DetailItem1");

		App.RetryAssert(() =>
			Assert.That(GetText("CommandCount"), Is.EqualTo("SelectionChangedCommand calls: 1")));

		App.Tap("BackToTasksButton");
		App.WaitForElement("Task2Text");
		App.Tap("Task2Text");
		App.WaitForElement("DetailItem2");

		Assert.That(GetText("SelectedItemStatus"), Is.EqualTo("Selected item: Detail item 1"),
			"The singleton view model should retain the first page's selected item.");
		Assert.That(GetText("CommandCount"), Is.EqualTo("SelectionChangedCommand calls: 1"),
			"Opening the second detail page should not invoke the command.");

		App.Tap("ArmTriggerButton");
		var baselineText = GetText("CommandCount");
		Assert.That(GetText("SelectionDelta"), Is.EqualTo("SelectionChangedCommand delta: -1"),
			"The armed callback delta should remain at its sentinel before the selection.");

		App.Tap("DetailItem2");

		App.RetryAssert(() =>
			Assert.That(GetText("SelectedItemStatus"), Is.EqualTo("Selected item: Detail item 2")));
		App.RetryAssert(() =>
			Assert.That(GetText("SelectionDelta"), Is.Not.EqualTo("SelectionChangedCommand delta: -1")));

		var deltaText = GetText("SelectionDelta");
		const string deltaPrefix = "SelectionChangedCommand delta: ";
		Assert.That(deltaText, Does.StartWith(deltaPrefix));
		Assert.That(int.TryParse(deltaText[deltaPrefix.Length..], out var delta), Is.True);

		var finalCountText = GetText("CommandCount");
		Assert.That(delta, Is.EqualTo(1),
			$"SelectionChangedCommand should execute once after the second-page selection; observed delta={delta}, baseline={baselineText}, final count={finalCountText}");
	}
}
#endif

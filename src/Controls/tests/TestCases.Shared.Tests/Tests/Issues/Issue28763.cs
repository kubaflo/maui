#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28763 : _IssuesUITest
{
	public Issue28763(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView SelectionChangedCommand runs multiple times with a singleton view model";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void SelectionChangedCommandRunsOncePerSelectionAfterRecreatingPage()
	{
		App.WaitForElement("TaskEntry");
		App.EnterText("TaskEntry", "Task one");
		App.Tap("AddTaskButton");
		App.EnterText("TaskEntry", "Task two");
		App.Tap("AddTaskButton");

		App.Tap("Task-1");
		App.WaitForElement("DetailHeading");

		var firstPageInstance = GetRequiredText("PageInstance");
		var firstViewModelInstance = GetRequiredText("ViewModelInstance");
		Assert.That(GetRequiredText("CallbackToken"), Is.EqualTo("Callback token: -1"));

		App.Tap("Detail-A");
		App.WaitForElement("Callback token: observed");
		App.WaitForElement("Selected item: Detail item A");
		Assert.That(
			GetRequiredText("CallbackCount"),
			Is.EqualTo("Callbacks this visit: 1"),
			"The first detail-page visit should establish one callback for one selection.");

		this.Back();
		App.WaitForElement("Task-2");
		App.Tap("Task-2");
		App.WaitForElement("DetailHeading");

		var secondPageInstance = GetRequiredText("PageInstance");
		var secondViewModelInstance = GetRequiredText("ViewModelInstance");
		Assert.That(secondPageInstance, Is.Not.EqualTo(firstPageInstance),
			"Back navigation followed by a new task should create a new detail page.");
		Assert.That(secondViewModelInstance, Is.EqualTo(firstViewModelInstance),
			"Both transient detail pages should use the same view model instance.");
		Assert.That(GetRequiredText("CallbackToken"), Is.EqualTo("Callback token: -1"),
			"The second visit should start before any new selection callback.");

		App.Tap("Detail-B");
		App.WaitForElement("Callback token: observed");
		App.WaitForElement("Selected item: Detail item B");

		var actualCount = GetRequiredText("CallbackCount");
		Assert.That(
			actualCount,
			Is.EqualTo("Callbacks this visit: 1"),
			$"SelectionChangedCommand should run exactly once per selection. Expected 1 callback; observed '{actualCount}'.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}
}
#endif

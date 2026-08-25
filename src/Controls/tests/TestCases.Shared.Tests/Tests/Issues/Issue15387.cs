using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15387 : _IssuesUITest
{
	public Issue15387(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "ScrollToAsync does not complete during initial page appearance";

#if ANDROID
	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrollToAsyncCompletesDuringInitialOnAppearing()
	{
		var scrollView = App.WaitForElement("Issue15387ScrollView");
		Assert.That(scrollView, Is.Not.Null, "The reported ScrollView should be present.");

		var firstItem = App.WaitForElement("Item 1");
		Assert.That(firstItem, Is.Not.Null, "The first BindableLayout item should be present.");

		var lastItem = App.WaitForElement("Item 12");
		Assert.That(lastItem, Is.Not.Null, "The last BindableLayout item should be present.");

		var lifecycleToken = App.WaitForElement("Issue15387LifecycleToken");
		var lifecycleText = lifecycleToken is null ? "<missing>" : lifecycleToken.GetText() ?? "<null>";
		Assert.That(lifecycleText, Is.EqualTo("Lifecycle: -1->0"),
			"The first appearance should transition from the sentinel state before scrolling.");

		var completionObserved = App.WaitForTextToBePresentInElement(
			"Issue15387CompletionLog",
			"After ScrollToAsync",
			TimeSpan.FromSeconds(5));

		var stateElement = App.FindElement("Issue15387CompletionState");
		var completionState = stateElement is null ? "<missing>" : stateElement.GetText() ?? "<null>";
		var logElement = App.FindElement("Issue15387CompletionLog");
		var completionLog = logElement is null ? "<missing>" : logElement.GetText() ?? "<null>";

		Assert.That(
			completionObserved &&
			completionState == "Completion state: 1" &&
			completionLog.Contains("Before ScrollToAsync", StringComparison.Ordinal) &&
			completionLog.Contains("After ScrollToAsync", StringComparison.Ordinal),
			Is.True,
			$"ScrollToAsync did not complete during initial OnAppearing; state={completionState}; log={completionLog}");
	}
#endif
}

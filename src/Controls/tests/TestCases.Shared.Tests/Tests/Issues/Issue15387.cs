#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15387 : _IssuesUITest
{
	const string CompletionStateId = "Issue15387CompletionState";
	const string CompletionSucceeded = "ScrollToAsync completed after initial OnAppearing";

	public Issue15387(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ScrollToAsync called from initial OnAppearing does not complete";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrollToAsyncCompletesDuringInitialOnAppearing()
	{
		var lifecycleState = App.WaitForElement("Issue15387LifecycleState");
		Assert.That(lifecycleState, Is.Not.Null);
		Assert.That(lifecycleState.GetText(), Is.EqualTo("OnAppearing callback 1 reached ScrollToAsync"));

		var scrollView = App.WaitForElement("Issue15387ScrollView");
		Assert.That(scrollView, Is.Not.Null);

		var itemCount = App.WaitForElement("Issue15387ItemCount");
		Assert.That(itemCount, Is.Not.Null);
		Assert.That(itemCount.GetText(), Is.EqualTo("Item count: 60"));

		var firstItem = App.WaitForElement("Item 01");
		Assert.That(firstItem, Is.Not.Null);
		Assert.That(firstItem.GetText(), Is.EqualTo("Item 01"));

		var completed = App.WaitForTextToBePresentInElement(
			CompletionStateId,
			CompletionSucceeded,
			TimeSpan.FromSeconds(5));

		var completionState = App.WaitForElement(CompletionStateId);
		Assert.That(completionState, Is.Not.Null);
		var observedCompletionState = completionState.GetText();

		Assert.That(
			completed,
			Is.True,
			$"Issue15387 ScrollToAsync did not complete after initial OnAppearing. Observed completion state: '{observedCompletionState}', expected: '{CompletionSucceeded}'.");
		Assert.That(observedCompletionState, Is.EqualTo(CompletionSucceeded));
	}
}
#endif

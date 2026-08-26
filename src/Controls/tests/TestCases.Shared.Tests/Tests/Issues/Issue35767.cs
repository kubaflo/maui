#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35767 : _IssuesUITest
{
	public Issue35767(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "SearchHandler.ShowsResults does not work correctly";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ResultsRemainHiddenAfterShowsResultsIsDisabled()
	{
		var searchField = AppiumQuery.ByClass("TextBox");
		App.WaitForElement(searchField);
		App.Tap(searchField);
		App.EnterText(searchField, "first");

		var initialResult = App.WaitForElement("SearchResultItem");
		Assert.That(initialResult, Is.Not.Null);
		var initialResultText = initialResult.GetText();
		Assert.That(initialResultText, Is.Not.Null);
		Assert.That(initialResultText, Is.EqualTo("Issue 35767 result"));

		App.Tap("DisableResultsButton");
		App.RetryAssert(() =>
		{
			var showsResultsStateElement = App.FindElement("ShowsResultsState");
			Assert.That(showsResultsStateElement, Is.Not.Null);
			var showsResultsState = showsResultsStateElement.GetText();
			Assert.That(showsResultsState, Is.Not.Null);
			Assert.That(showsResultsState, Is.EqualTo("ShowsResults: False"));
		});

		App.ClearText(searchField);
		App.Tap(searchField);
		App.EnterText(searchField, "second");

		App.RetryAssert(() =>
		{
			var observedQueryElement = App.FindElement("QueryObserved");
			Assert.That(observedQueryElement, Is.Not.Null);
			var observedQuery = observedQueryElement.GetText();
			Assert.That(observedQuery, Is.Not.Null);
			Assert.That(observedQuery, Is.EqualTo("second"));
		});

		var visibleResultCount = App.FindElements("SearchResultItem").Count;
		Assert.That(
			visibleResultCount,
			Is.EqualTo(0),
			$"Search results remained visible after ShowsResults changed to false; observed visible result count={visibleResultCount}.");
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35667 : _IssuesUITest
{
	public Issue35667(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "TextTransform.Uppercase does not work on Shell SearchHandler";

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerDisplaysUppercaseText()
	{
		const string queryNotObserved = "Query event not received";

		App.WaitForElement("Issue35667Configuration");
		var observedQuery = App.WaitForElement("Issue35667ObservedQuery");
		Assert.That(observedQuery.GetText(), Is.EqualTo(queryNotObserved));

		var nativeSearchFieldQuery = AppiumQuery.ByXPath("//XCUIElementTypeSearchField");
		var nativeSearchField = App.WaitForElement(nativeSearchFieldQuery);
		var initialText = nativeSearchField.GetText();

		App.EnterText(nativeSearchFieldQuery, "maui");

		App.RetryAssert(() =>
		{
			var updatedQuery = App.WaitForElement("Issue35667ObservedQuery").GetText();
			Assert.That(updatedQuery, Is.EqualTo("maui"),
				$"SearchHandler.Query should report the entered lowercase text. Observed: \"{updatedQuery}\"; Expected: \"maui\".");
		});
		App.RetryAssert(() =>
		{
			var updatedSearchField = App.WaitForElement(nativeSearchFieldQuery);
			Assert.That(updatedSearchField.GetText(), Is.Not.EqualTo(initialText));
		});

		var displayedText = App.WaitForElement(nativeSearchFieldQuery).GetText();
		Assert.That(displayedText, Is.EqualTo("MAUI"),
			$"Shell SearchHandler native text should display uppercase after lowercase input. Observed: \"{displayedText}\"; Expected: \"MAUI\".");
	}
}
#endif

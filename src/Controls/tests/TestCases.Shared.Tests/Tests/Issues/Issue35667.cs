#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35667 : _IssuesUITest
{
	public override string Issue => "TextTransform.Uppercase does not work on Shell SearchHandler";

	public Issue35667(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Shell)]
	public void SearchHandlerDisplaysUppercaseText()
	{
		var readyText = App.WaitForElement("Issue35667Ready").GetText();
		if (readyText is null)
		{
			Assert.Fail("Issue35667 ready text was not available.");
		}

		Assert.That(readyText, Does.StartWith("TextTransform=Uppercase"));

		var initialQueryStatus = App.WaitForElement("Issue35667QueryStatus").GetText();
		if (initialQueryStatus is null)
		{
			Assert.Fail("Issue35667 query status text was not available before input.");
		}

		Assert.That(initialQueryStatus, Is.EqualTo("QUERY_NOT_OBSERVED"));

		var searchFieldQuery = AppiumQuery.ByXPath("//XCUIElementTypeSearchField");
		var searchField = App.WaitForElement(searchFieldQuery);
		var initialText = searchField.GetText();
		if (initialText is null)
		{
			Assert.Fail("Issue35667 native search-field text was not available before input.");
		}

		Assert.That(initialText == string.Empty || initialText == "Type lowercase text", Is.True,
			"Issue35667 native search field should contain no query before input.");

		App.Tap(searchFieldQuery);
		App.EnterText(searchFieldQuery, "maui");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue35667QueryStatus", "QUERY_OBSERVED:"),
			Is.True,
			"Issue35667 SearchHandler Query callback was not observed.");

		var queryStatus = App.WaitForElement("Issue35667QueryStatus").GetText();
		if (queryStatus is null)
		{
			Assert.Fail("Issue35667 query status text was not available after input.");
		}

		Assert.That(queryStatus, Does.StartWith("QUERY_OBSERVED:"));

		App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeSearchField[@value='maui' or @value='MAUI']"),
			timeout: TimeSpan.FromSeconds(15));

		var nativeText = App.WaitForElement(searchFieldQuery).GetText();
		if (nativeText is null)
		{
			Assert.Fail("Issue35667 native search-field text was not available after input.");
		}

		Assert.That(nativeText, Is.Not.Empty);
		Assert.That(nativeText, Is.EqualTo("MAUI"),
			$"Issue35667 Shell SearchHandler text transform mismatch: expected 'MAUI', observed '{nativeText}'.");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35767 : _IssuesUITest
{
	public Issue35767(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "SearchHandler.ShowsResults does not work correctly";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShowsResultsFalseHidesSuggestionsAfterRuntimeChange()
	{
		App.WaitForElement("DisableResultsButton");

		var searchHandler = App.GetShellSearchHandler();
		searchHandler.Tap();
		searchHandler.SendKeys("alpha");
		App.WaitForElement("SearchResult");
		App.Tap("SearchResult");

		App.Tap("DisableResultsButton");
		App.WaitForElement("ShowsResults transition: True to False");

		searchHandler.Tap();
		searchHandler.Clear();
		searchHandler.SendKeys("beta");

		App.WaitForElement("Beta query processed; no result selected");

		bool? searchResultWasVisible = null;
		try
		{
			App.WaitForElement("SearchResult", timeout: TimeSpan.FromSeconds(5));
			searchResultWasVisible = true;
		}
		catch (TimeoutException)
		{
			// Absence is the expected behavior after ShowsResults is disabled.
			searchResultWasVisible = false;
		}

		Assert.That(searchResultWasVisible, Is.Not.Null, "Native result observation did not complete.");

		if (searchResultWasVisible is true)
		{
			App.Tap("SearchResult");
			App.WaitForElement("Beta result selected while ShowsResults was false");
		}

		Assert.That(
			App.FindElement("QueryStatus").GetText(),
			Is.EqualTo("Beta query processed; no result selected"),
			"Search results remained visible after ShowsResults was set to false.");
	}
}
#endif

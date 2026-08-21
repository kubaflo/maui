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
		var alphaResultQuery = AppiumQuery.ByXPath("//Pane[@Name='PopupHost']//ListItem[Text[@Name='alpha result']]");
		var betaResultQuery = AppiumQuery.ByXPath("//Pane[@Name='PopupHost']//ListItem[Text[@Name='beta result']]");

		App.WaitForElement("Issue35767QueryState");
		App.WaitForElement("TextBox");
		App.Tap("TextBox");
		App.EnterText("TextBox", "alpha");

		App.WaitForTextToBePresentInElement("Issue35767QueryState", "Query: alpha");
		App.WaitForTextToBePresentInElement("Issue35767SourceState", "Source: alpha result");
		App.WaitForElement(alphaResultQuery);
		Assert.That(App.FindElements(alphaResultQuery).Count, Is.EqualTo(1),
			"Expected exactly one selectable alpha result while ShowsResults was true.");

		App.ClearText("TextBox");
		App.Tap("Issue35767DisableResults");
		App.WaitForTextToBePresentInElement("Issue35767PropertyState", "ShowsResults: False");
		Assert.That(App.FindElements(betaResultQuery).Count, Is.Zero,
			"A beta result existed before the post-disable query.");

		App.Tap("TextBox");
		App.EnterText("TextBox", "beta");
		App.WaitForTextToBePresentInElement("Issue35767QueryState", "Query: beta");
		App.WaitForTextToBePresentInElement("Issue35767SourceState", "Source: beta result");

		var visibleResultCount = App.FindElements(betaResultQuery).Count;
		Assert.That(visibleResultCount, Is.Zero,
			$"Windows exposed {visibleResultCount} selectable 'beta result'; expected 0 while ShowsResults was false");
	}
}
#endif

#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35767 : _IssuesUITest
{
	public Issue35767(TestDevice device) : base(device) { }

	public override string Issue => "SearchHandler.ShowsResults does not work correctly";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShowsResultsFalseSuppressesResultsAfterRuntimeChange()
	{
		var initialState = App.WaitForElement("ShowsResultsState").GetText();
		Assert.That(initialState, Is.EqualTo("ShowsResults: True"));

		var searchHandler = App.GetShellSearchHandler();
		searchHandler.Tap();
		searchHandler.SendKeys("Alpha");
		App.WaitForElement("Alpha result");

		searchHandler.Clear();
		App.WaitForNoElement("Alpha result");

		App.Tap("DisableResultsButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("ShowsResultsState", "ShowsResults: False"),
			Is.True,
			"The ShowsResults transition should complete before entering the second query.");

		var transitionedState = App.FindElement("ShowsResultsState");
		Assert.That(transitionedState, Is.Not.Null);
		var transitionedStateText = transitionedState.GetText();
		Assert.That(transitionedStateText, Is.EqualTo("ShowsResults: False"));

		searchHandler.Tap();
		searchHandler.SendKeys("Beta");

		var betaVisible = HelperExtensions.IsElementVisible(App, "Beta result");
		Assert.That(
			betaVisible,
			Is.False,
			"ShowsResults=False should suppress Beta result; observed native result visibility=True, expected=False.");
	}
}
#endif

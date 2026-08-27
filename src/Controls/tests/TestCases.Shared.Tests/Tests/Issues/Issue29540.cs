#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29540 : _IssuesUITest
{
	public override string Issue => "TabbedViewHandler implementation incomplete on iOS";

	public Issue29540(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void CustomTabbedViewHandlerDisplaysTabs()
	{
		const string firstTabContent = "FirstTabContent";
		const string navigationResult = "NavigationResult";

		var initialFirstTabCount = App.FindElements(firstTabContent).Count;
		Assert.That(initialFirstTabCount, Is.EqualTo(0),
			$"First tab content should be absent before navigation. Expected 0 elements but observed {initialFirstTabCount}.");

		var pendingResult = App.WaitForElement(navigationResult);
		Assert.That(pendingResult.GetText(), Is.EqualTo("Navigation pending"));

		App.Tap("NavigateButton");

		var completedResult = App.WaitForElement(() =>
		{
			var firstTabElements = App.FindElements(firstTabContent);
			if (firstTabElements.Count == 1)
			{
				return firstTabElements.First();
			}

			var results = App.FindElements(navigationResult);
			if (results.Count != 1)
			{
				return null;
			}

			var result = results.First();
			return result.GetText() == "Navigation pending" ? null : result;
		});

		Assert.That(completedResult.GetText(), Is.EqualTo("First tab"),
			"Custom TabbedViewHandler navigation did not display the first tab");
	}
}
#endif

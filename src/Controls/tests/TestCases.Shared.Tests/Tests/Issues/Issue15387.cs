#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15387 : _IssuesUITest
{
	public Issue15387(TestDevice device) : base(device) { }

	public override string Issue => "ScrollToAsync does not return from initial OnAppearing";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrollToAsyncCompletesFromInitialOnAppearing()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires portrait orientation.");

		App.WaitForElement("CompletionState");
		App.Back();
		App.WaitForGoToTestButtonWithRecovery(Issue);
		App.ClearText("SearchBar");
		App.EnterText("SearchBar", Issue);
		App.WaitForElement("GoToTestButton");
		App.Tap("GoToTestButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("AppearingState", "STARTED", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The initial OnAppearing callback did not reach ScrollToAsync.");

		App.WaitForTextToBePresentInElement("CompletionState", "COMPLETED", timeout: TimeSpan.FromSeconds(5));

		var completionElement = App.FindElement("CompletionState");
		if (completionElement is null)
		{
			Assert.Fail("The ScrollToAsync completion state element was not found.");
			return;
		}

		var completionState = completionElement.GetText() ?? string.Empty;
		Assert.That(
			completionState,
			Is.EqualTo("COMPLETED"),
			$"Issue15387 ScrollToAsync completion state was {completionState}; expected COMPLETED.");
	}
}
#endif

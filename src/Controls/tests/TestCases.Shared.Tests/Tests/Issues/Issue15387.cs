#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15387 : _IssuesUITest
{
	public Issue15387(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ScrollToAsync does not complete during initial OnAppearing";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrollToAsyncCompletesDuringInitialOnAppearing()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue15387AppearanceCount", "1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The page should appear exactly once.");
		Assert.That(App.FindElement("Issue15387AppearanceCount").GetText(), Is.EqualTo("1"));
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue15387ScrollStarted", "Pending", TimeSpan.FromSeconds(10)),
			Is.True,
			"OnAppearing should invoke ScrollToAsync.");

		var firstItemBounds = App.WaitForElement("Constructor item 01").GetRect();
		var scrollViewBounds = App.WaitForElement("Issue15387ScrollView").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(scrollViewBounds.Width, Is.GreaterThan(0), "The ScrollView should have nonzero width.");
			Assert.That(scrollViewBounds.Height, Is.GreaterThan(0), "The ScrollView should have nonzero height.");
			Assert.That(firstItemBounds.Y, Is.GreaterThanOrEqualTo(scrollViewBounds.Y),
				"The first constructor-supplied item should be inside the ScrollView.");
			Assert.That(firstItemBounds.Y + firstItemBounds.Height,
				Is.LessThanOrEqualTo(scrollViewBounds.Y + scrollViewBounds.Height),
				"The first constructor-supplied item should be visible inside the ScrollView.");
		});

		var completed = App.WaitForTextToBePresentInElement(
			"Issue15387CompletionState",
			"Completed",
			TimeSpan.FromSeconds(10));
		var completionState = App.FindElement("Issue15387CompletionState").GetText();

		Assert.That(
			completed,
			Is.True,
			$"ScrollToAsync completion state was '{completionState}' after 10 seconds; expected 'Completed'.");
	}
}
#endif

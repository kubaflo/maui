#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue15387 : _IssuesUITest
{
	public Issue15387(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "ScrollToAsync does not return during initial OnAppearing";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void ScrollToAsyncCompletesDuringInitialOnAppearing()
	{
		App.WaitForElement("Issue15387ScrollView");
		App.WaitForElement("Bindable item 1");

		var started = App.WaitForTextToBePresentInElement(
			"Issue15387Started",
			"Started: 1",
			TimeSpan.FromSeconds(5));
		var completed = App.WaitForTextToBePresentInElement(
			"Issue15387Completed",
			"Completed: 1",
			TimeSpan.FromSeconds(5));

		Assert.That(started, Is.True, "Initial OnAppearing did not start");

		var startedText = App.WaitForElement("Issue15387Started").GetText();
		var completedText = App.WaitForElement("Issue15387Completed").GetText();

		Assert.That(
			completed,
			Is.True,
			$"Initial OnAppearing ScrollToAsync did not complete; started={startedText}, completed={completedText}");
		Assert.That(completedText, Is.EqualTo("Completed: 1"));
	}
}
#endif

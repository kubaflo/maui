#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27344 : _IssuesUITest
{
	const string OpenModalButtonId = "Issue27344OpenModalButton";
	const string PostDeleteReadsLabelId = "Issue27344PostDeleteReads";
	const string PersonActionButtonId = "Issue27344PersonActionButton";
	const string DeleteToolbarItemText = "Delete";

	public Issue27344(TestDevice device) : base(device)
	{
	}

	public override string Issue => "PopModalAsync accesses a deleted page BindingContext";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void PopModalDoesNotReadBindingContextAfterDeletion()
	{
		var initialResult = App.WaitForElement(PostDeleteReadsLabelId);
		if (initialResult is null)
			throw new AssertionException("The post-delete read result was not found.");

		Assert.That(initialResult.GetText(), Is.EqualTo("-1"));

		App.Tap(OpenModalButtonId);

		var personAction = App.WaitForElement(PersonActionButtonId);
		if (personAction is null)
			throw new AssertionException("The bound person action button was not found.");

		Assert.That(personAction.IsEnabled(), Is.True, "The CanAct binding should initially enable the person action.");

		App.WaitForElement(DeleteToolbarItemText);
		App.Tap(DeleteToolbarItemText);

		App.WaitForElement(OpenModalButtonId);
		App.WaitForNoElement(PersonActionButtonId);
		App.WaitForNoElement(DeleteToolbarItemText);

		Assert.That(
			App.WaitForTextToBePresentInElement(PostDeleteReadsLabelId, "Completed:"),
			Is.True,
			"PopModalAsync did not complete and publish the post-delete read count.");

		var completedResult = App.WaitForElement(PostDeleteReadsLabelId);
		if (completedResult is null)
			throw new AssertionException("The completed post-delete read result was not found.");

		var completedText = completedResult.GetText();
		Assert.That(
			completedText,
			Is.EqualTo("Completed: 0"),
			$"PopModalAsync accessed PersonViewModel.CanAct after Delete. Observed '{completedText}', expected 'Completed: 0'.");
	}
}
#endif

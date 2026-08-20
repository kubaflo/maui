#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	public Issue33607(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void ApplyingCollectionChangeToLayoutAfterWindowDestroyDoesNotUseDisposedServices()
	{
		Assert.That(App.WaitForElement("Issue33607RunState").GetText(), Is.EqualTo("not-started"));
		Assert.That(App.WaitForElement("Issue33607Completed").GetText(), Is.EqualTo("completed=-1"));
		Assert.That(App.WaitForElement("Issue33607PagesLoaded").GetText(), Is.EqualTo("pagesLoaded=-1"));
		Assert.That(App.WaitForElement("Issue33607AttachedHierarchies").GetText(), Is.EqualTo("attachedHierarchies=-1"));
		Assert.That(App.WaitForElement("Issue33607InitialItemsRendered").GetText(), Is.EqualTo("initialItemsRendered=-1"));
		Assert.That(App.WaitForElement("Issue33607DestroyingCallbacks").GetText(), Is.EqualTo("destroyingCallbacks=-1"));
		Assert.That(App.WaitForElement("Issue33607AttemptedUpdates").GetText(), Is.EqualTo("attemptedUpdates=-1"));
		Assert.That(App.WaitForElement("Issue33607ObjectDisposedExceptions").GetText(), Is.EqualTo("objectDisposedExceptions=-1"));

		App.Tap("Issue33607Run");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33607RunState", "started", timeout: TimeSpan.FromSeconds(10)),
			Is.True);
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue33607Completed", "completed=3", timeout: TimeSpan.FromSeconds(30)),
			Is.True);

		Assert.That(App.FindElement("Issue33607PagesLoaded").GetText(), Is.EqualTo("pagesLoaded=3"));
		Assert.That(App.FindElement("Issue33607AttachedHierarchies").GetText(), Is.EqualTo("attachedHierarchies=3"));
		Assert.That(App.FindElement("Issue33607InitialItemsRendered").GetText(), Is.EqualTo("initialItemsRendered=3"));
		Assert.That(App.FindElement("Issue33607DestroyingCallbacks").GetText(), Is.EqualTo("destroyingCallbacks=3"));
		Assert.That(App.FindElement("Issue33607AttemptedUpdates").GetText(), Is.EqualTo("attemptedUpdates=3"));

		var exceptionText = App.FindElement("Issue33607ObjectDisposedExceptions").GetText()!;
		var exceptionCount = exceptionText.Substring("objectDisposedExceptions=".Length);
		Assert.That(
			exceptionCount,
			Is.EqualTo("0"),
			$"Post-destroy ILayout collection changes must not throw ObjectDisposedException; expected=0, observed={exceptionCount}");
	}
}
#endif

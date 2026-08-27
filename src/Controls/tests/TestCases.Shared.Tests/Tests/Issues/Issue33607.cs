#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public override string Issue => "ObjectDisposedException when ILayout is updated after closing a Window";

	public Issue33607(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Window)]
	public void CollectionChangeCanBeAppliedToILayoutAfterWindowIsDestroyed()
	{
		var loadedWindowCount = -1;
		var destroyedWindowCount = -1;
		var successfulMutationCount = -1;

		var loadedWindowCountElement = App.WaitForElement("LoadedWindowCount");
		if (loadedWindowCountElement is null)
			throw new AssertionException("The loaded-window counter was not found.");

		var destroyedWindowCountElement = App.WaitForElement("DestroyedWindowCount");
		if (destroyedWindowCountElement is null)
			throw new AssertionException("The destroyed-window counter was not found.");

		var successfulMutationCountElement = App.WaitForElement("SuccessfulMutationCount");
		if (successfulMutationCountElement is null)
			throw new AssertionException("The successful-mutation counter was not found.");

		loadedWindowCount = ReadCounter(loadedWindowCountElement, "loaded-window");
		destroyedWindowCount = ReadCounter(destroyedWindowCountElement, "destroyed-window");
		successfulMutationCount = ReadCounter(successfulMutationCountElement, "successful-mutation");
		Assert.That(loadedWindowCount, Is.Zero);
		Assert.That(destroyedWindowCount, Is.Zero);
		Assert.That(successfulMutationCount, Is.Zero);

		App.Tap("RunCyclesButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("LoadedWindowCount", "2", TimeSpan.FromSeconds(30)),
			Is.True,
			"Both secondary ILayout hosts should load.");
		Assert.That(
			App.WaitForTextToBePresentInElement("DestroyedWindowCount", "2", TimeSpan.FromSeconds(30)),
			Is.True,
			"Both secondary windows should be destroyed.");
		Assert.That(
			App.WaitForTextToBePresentInElement("CycleCompletionLabel", "Completed 2 cycles", TimeSpan.FromSeconds(30)),
			Is.True,
			"Both post-close collection mutation attempts should finish.");

		loadedWindowCount = ReadCounter(loadedWindowCountElement, "loaded-window");
		destroyedWindowCount = ReadCounter(destroyedWindowCountElement, "destroyed-window");
		Assert.That(loadedWindowCount, Is.EqualTo(2));
		Assert.That(destroyedWindowCount, Is.EqualTo(2));

		successfulMutationCount = ReadCounter(successfulMutationCountElement, "successful-mutation");
		Assert.That(
			successfulMutationCount,
			Is.EqualTo(2),
			$"Post-close ILayout mutations completed: measured {successfulMutationCount}, expected 2.");

		static int ReadCounter(IUIElement element, string counterName)
		{
			var text = element.GetText();
			if (text is null)
				throw new AssertionException($"The {counterName} counter had no text.");

			return int.Parse(text);
		}
	}
}
#endif

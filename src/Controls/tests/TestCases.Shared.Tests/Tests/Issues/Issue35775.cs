#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35775 : _IssuesUITest
{
	public override string Issue => "IndicatorView leaks when linked to a CarouselView using a shared ObservableCollection";

	public Issue35775(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.CarouselView)]
	public void PoppedLinkedControlsShouldNotBeRetainedBySharedObservableCollection()
	{
		var snapshotInitial = App.WaitForElement("Issue35775SnapshotAlive");
		Assert.That(snapshotInitial, Is.Not.Null);
		Assert.That(snapshotInitial.GetText(), Is.EqualTo("Snapshot alive: -1"));

		var sharedInitial = App.WaitForElement("Issue35775SharedAlive");
		Assert.That(sharedInitial, Is.Not.Null);
		Assert.That(sharedInitial.GetText(), Is.EqualTo("Shared alive: -1"));

		App.Tap("Issue35775SnapshotButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775SnapshotCompletion",
				"Snapshot pushes: 3; pops: 3; references: 6"),
			Is.True,
			"Snapshot navigation visits did not complete.");

		App.Tap("Issue35775SharedButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775SharedCompletion",
				"Shared pushes: 3; pops: 3; references: 6"),
			Is.True,
			"Shared observable navigation visits did not complete.");

		App.Tap("Issue35775RetentionButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue35775RetentionCompletion",
				"Retention checked: snapshot references 6; shared references 6; updates 250; navigation root True"),
			Is.True,
			"Retention checks, feed updates, or return navigation did not complete.");

		var snapshotResult = App.WaitForElement("Issue35775SnapshotAlive");
		Assert.That(snapshotResult, Is.Not.Null);
		Assert.That(
			snapshotResult.GetText(),
			Is.EqualTo("Snapshot alive: 0/6"),
			"Snapshot controls did not collect, so the shared-source comparison is inconclusive.");

		var sharedResult = App.WaitForElement("Issue35775SharedAlive");
		Assert.That(sharedResult, Is.Not.Null);
		Assert.That(
			sharedResult.GetText(),
			Is.EqualTo("Shared alive: 0/6"),
			"Shared observable popped controls remained alive after GC");
	}
}
#endif

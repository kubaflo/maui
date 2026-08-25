#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30776 : _IssuesUITest
{
	const string ExpectedLoadCount = "Project and task data loads: 1";
	const string ExpectedTransition = "transition:return-completed";

	public override string Issue => "Project and task data reloads after returning from detail";

	public Issue30776(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.LifeCycle)]
	public void ProjectAndTaskDataLoadsOnceAfterReturningFromDetail()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("Issue30776HomeHeading");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue30776LoadCountLabel", ExpectedLoadCount),
			Is.True,
			"Initial project and task data load did not complete.");

		var initialLoadCount = App.WaitForElement("Issue30776LoadCountLabel").GetText();
		Assert.That(initialLoadCount, Is.Not.Null, "Initial load-count text must be available.");
		Assert.That(initialLoadCount, Is.EqualTo(ExpectedLoadCount),
			$"Expected initial text '{ExpectedLoadCount}', observed '{initialLoadCount ?? "<null>"}'.");

		App.ScrollDown("Issue30776ScrollView", ScrollStrategy.Gesture, swipePercentage: 0.75);
		App.WaitForElement("Issue30776ProjectBalanceButton");
		App.Tap("Issue30776ProjectBalanceButton");

		App.WaitForElement("Issue30776ProjectDetailHeading");
		App.WaitForElement("Issue30776CloseProjectButton");
		App.Tap("Issue30776CloseProjectButton");

		App.WaitForElement("Issue30776HomeHeading");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue30776TransitionLabel", ExpectedTransition),
			Is.True,
			"Navigation.PopAsync did not complete after returning to the home page.");

		var transition = App.WaitForElement("Issue30776TransitionLabel").GetText();
		Assert.That(transition, Is.Not.Null, "Return-completed transition text must be available.");
		Assert.That(transition, Is.EqualTo(ExpectedTransition),
			$"Expected transition text '{ExpectedTransition}', observed '{transition ?? "<null>"}'.");

		var observedLoadCount = App.WaitForElement("Issue30776LoadCountLabel").GetText();
		Assert.That(observedLoadCount, Is.Not.Null,
			$"Project/task data should load once after returning from detail. Expected '{ExpectedLoadCount}', observed '<null>'.");
		Assert.That(observedLoadCount, Is.EqualTo(ExpectedLoadCount),
			$"Project/task data should load once after returning from detail. Expected '{ExpectedLoadCount}', observed '{observedLoadCount ?? "<null>"}'.");
	}
}
#endif

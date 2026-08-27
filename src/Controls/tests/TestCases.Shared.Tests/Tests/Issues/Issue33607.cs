#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33607 : _IssuesUITest
{
	public Issue33607(TestDevice device) : base(device) { }

	public override string Issue => "[Windows] ObjectDisposedException after closing window";

	[Test]
	[Category(UITestCategories.Window)]
	public void ApplyingCollectionChangeToRetainedILayoutAfterClosingWindowDoesNotUseDisposedServices()
	{
		var runButton = App.WaitForElement("RunCycleButton");
		Assert.That(runButton, Is.Not.Null);
		var stateElement = App.WaitForElement("StateLabel");
		Assert.That(stateElement, Is.Not.Null);
		var initialState = stateElement.GetText();
		Assert.That(initialState, Is.EqualTo("Retained=0;Cycle=-1;Lifecycle=NotStarted;Exception=NotObserved;ObjectName=NotObserved;Mutation=False;InitialCount=-1;InitialText=None;FinalCount=-1;SecondText=None;Identity=False"));

		for (var cycle = 0; cycle < 2; cycle++)
		{
			App.Tap("RunCycleButton");
			var completed = App.WaitForTextToBePresentInElement(
				"CompletionLabel",
				$"Cycle {cycle} complete",
				timeout: TimeSpan.FromSeconds(15));
			Assert.That(completed, Is.True, $"Cycle {cycle} did not complete its open-load-close-mutate transition.");

			stateElement = App.WaitForElement("StateLabel");
			Assert.That(stateElement, Is.Not.Null);
			var state = stateElement.GetText();
			Assert.That(state, Does.Contain($"Retained={cycle + 1};Cycle={cycle};Lifecycle=Loaded,Destroying"),
				$"Cycle {cycle} did not retain the expected page or observe Loaded and Destroying. Observed: {state}");
			Assert.That(state, Does.Contain("Mutation=True;InitialCount=1;InitialText=Initial ILayout item"),
				$"Cycle {cycle} did not apply the collection mutation after rendering the initial Label. Observed: {state}");
			Assert.That(state, Does.Contain("Identity=True"),
				$"Cycle {cycle} did not preserve the expected retained page identity. Observed: {state}");
			Assert.That(
				state,
				Does.Contain("Exception=None;ObjectName=None")
					.And.Contain("FinalCount=2")
					.And.Contain($"SecondText=Post-close item {cycle + 1}"),
				$"Post-close ILayout collection Apply should complete without ObjectDisposedException. Cycle {cycle} observed: {state}");
		}
	}
}
#endif

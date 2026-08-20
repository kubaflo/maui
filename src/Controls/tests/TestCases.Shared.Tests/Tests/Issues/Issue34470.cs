#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34470 : _IssuesUITest
{
	public Issue34470(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Modal with NavigationPage creates memory leaks";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void PreviousButtonHandlerIsCollectedAfterModalNavigationPageLoads()
	{
		const string stateId = "CollectionState";
		const string pendingState = "CallbackToken=0; IsAlive=Pending";

		App.WaitForElement("NavigateButton");
		Assert.That(App.WaitForElement(stateId).GetText(), Is.EqualTo(pendingState));

		App.Tap("NavigateButton");

		App.WaitForElement("ModalPageMarker");
		Assert.That(
			App.WaitForTextToBePresentInElement(stateId, "CallbackToken=1;", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The collection callback did not run after the modal NavigationPage loaded.");

		var collectionState = App.FindElement(stateId).GetText()
			?? throw new InvalidOperationException("The collection state did not expose text.");
		Assert.That(collectionState, Is.AnyOf(
			"CallbackToken=1; IsAlive=True",
			"CallbackToken=1; IsAlive=False"));

		bool isAlive = collectionState.EndsWith("IsAlive=True", StringComparison.Ordinal);
		Assert.That(
			isAlive,
			Is.False,
			$"Previous ButtonHandler collection failed after modal NavigationPage loaded: observed IsAlive={isAlive}; expected IsAlive=False");
	}
}
#endif

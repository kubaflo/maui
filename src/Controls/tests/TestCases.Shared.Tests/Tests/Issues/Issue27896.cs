#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27896 : _IssuesUITest
{
	public Issue27896(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Android Back does not invoke an activity OnBackPressedDispatcher callback when dismissing a modal page";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void AndroidBackInvokesActivityCallbackBeforeModalDismissal()
	{
		const string expectedInitialState = "Activity back callback: waiting";
		const string expectedReceivedState = "Activity back callback: received";

		var initialState = App.WaitForElement("CallbackStateLabel").GetText();
		Assert.That(initialState, Is.EqualTo(expectedInitialState));

		App.Tap("OpenModalButton");
		App.WaitForElement("ModalReadyLabel");

		App.Back();
		App.WaitForElement("OpenModalButton");
		App.WaitForTextToBePresentInElement(
			"CallbackStateLabel",
			expectedReceivedState,
			timeout: TimeSpan.FromSeconds(5));

		var actualState = App.WaitForElement("CallbackStateLabel").GetText();
		Assert.That(
			actualState,
			Is.EqualTo(expectedReceivedState),
			$"Android Back callback state mismatch after modal dismissal. Expected '{expectedReceivedState}', observed '{actualState}'.");
	}
}
#endif

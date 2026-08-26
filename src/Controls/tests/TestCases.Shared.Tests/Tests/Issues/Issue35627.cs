using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35627 : _IssuesUITest
{
	public Issue35627(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Loaded does not fire after disconnecting a gesture control and re-entering a TabbedPage tab";

#if IOS
	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void GestureControlRaisesLoadedAfterNativeTabReentry()
	{
		App.WaitForElement("OpenLifecycleTabs");
		App.Tap("OpenLifecycleTabs");

		var gestureControl = App.WaitForElement("GestureControl");
		Assert.That(gestureControl.IsDisplayed(), Is.True, "The gesture control should be visible on the initial tab.");

		var initialState = App.WaitForElement("LifecycleEvents").GetText();
		Assert.That(initialState, Is.EqualTo("Loaded=1; Unloaded=0"), "The initial attachment must establish the lifecycle baseline.");
		var initialIdentity = App.WaitForElement("ControlIdentity").GetText();
		Assert.That(initialIdentity, Is.Not.Null);
		Assert.That(initialIdentity, Does.StartWith("Instance="));
		Assert.That(App.WaitForElement("RecognizerCount").GetText(), Is.EqualTo("Recognizers=1"));

		App.TapTab("Other");
		App.WaitForElement("OtherReady");
		Assert.That(
			App.WaitForTextToBePresentInElement("OtherReady", "Unloaded=1", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The gesture control must unload before the handler is disconnected.");
		Assert.That(App.WaitForElement("OtherReady").GetText(), Is.EqualTo("OtherReady; Loaded=1; Unloaded=1"));

		App.TapTab("Lifecycle");
		App.WaitForElement("CheckLifecycle");
		App.Tap("CheckLifecycle");
		Assert.That(
			App.WaitForTextToBePresentInElement("CheckSequence", "Check=0", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The lifecycle check must run after tab re-entry.");
		Assert.That(App.WaitForElement("ControlIdentity").GetText(), Is.EqualTo(initialIdentity), "Tab re-entry must retain the original control instance.");
		Assert.That(App.WaitForElement("RecognizerCount").GetText(), Is.EqualTo("Recognizers=1"), "The original public gesture recognizer must remain attached.");

		App.WaitForTextToBePresentInElement(
			"LifecycleEvents",
			"Loaded=2; Unloaded=1",
			timeout: TimeSpan.FromSeconds(5));
		var measuredState = App.WaitForElement("LifecycleEvents").GetText();
		Assert.That(
			measuredState,
			Is.EqualTo("Loaded=2; Unloaded=1"),
			$"Issue35627 expected Loaded count 2 after native tab re-entry; measured {measuredState}");
	}
#endif
}

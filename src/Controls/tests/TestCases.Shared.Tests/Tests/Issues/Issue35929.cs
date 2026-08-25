#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35929 : _IssuesUITest
{
	public Issue35929(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] Debugging breakpoint never binds on device";

	[Test]
	[Category(UITestCategories.Button)]
	public void BreakpointBindsAndIsHitWhenCounterButtonIsTapped()
	{
		App.WaitForElement("CounterButton");

		App.Tap("CounterButton");

		App.WaitForElement("Clicked 1 time");
		var counterButton = App.WaitForElement("CounterButton");
		Assert.That(counterButton.GetText(), Is.EqualTo("Clicked 1 time"),
			"OnCounterClicked did not complete after the counter button was tapped");

		var debuggerStatus = App.WaitForElement("DebuggerStatus");
		Assert.That(debuggerStatus.GetText(), Is.EqualTo("OnCounterClicked completed; Debugger.IsAttached=True"),
			"Issue 35929 breakpoint remained unbound after OnCounterClicked executed");
	}
}
#endif

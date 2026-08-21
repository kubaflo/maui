using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35929 : _IssuesUITest
{
	public Issue35929(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] Managed breakpoint does not bind on a physical device";

#if IOS
	[Test]
	[Category(UITestCategories.Button)]
	public void ManagedDebuggerRemainsAttachedWhenButtonHandlerRuns()
	{
		App.WaitForElement("CounterButton");
		Assert.That(
			App.FindElement("DebuggerStatusLabel").GetText(),
			Is.EqualTo("Managed debugger attached: not observed"));

		App.Tap("CounterButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("HandlerStatusLabel", "Handler invoked: 1"),
			Is.True,
			"OnCounterClicked was not observed.");
		Assert.That(
			App.FindElement("CounterLabel").GetText(),
			Is.EqualTo("Current count: 1"));

		var debuggerStatus = App.FindElement("DebuggerStatusLabel").GetText();
		Assert.That(
			debuggerStatus,
			Is.EqualTo("Managed debugger attached: True"),
			"Managed debugger was not attached when OnCounterClicked ran: attached=False, handlerInvocations=1; expected attached=True.");
	}
#endif
}

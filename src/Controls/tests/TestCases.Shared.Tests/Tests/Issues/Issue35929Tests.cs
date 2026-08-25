#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35929Tests : _IssuesUITest
{
	public Issue35929Tests(TestDevice device) : base(device) { }

	public override string Issue => "[iOS] Debugging breakpoint never binds on device";

	[Test]
	[Category(UITestCategories.Button)]
	public void SourceBreakpointStopsBeforeCounterHandlerCompletes()
	{
		const string counterButtonId = "Issue35929CounterButton";

		var counterButton = App.WaitForElement(counterButtonId);
		Assert.That(counterButton.GetText(), Is.EqualTo("Click me"));

		App.Tap(counterButtonId);

		var textAfterTap = App.WaitForElement(counterButtonId).GetText();
		Assert.That(
			textAfterTap,
			Is.EqualTo("Click me"),
			$"Issue35929 source breakpoint did not bind and stop; counter text after tap was '{textAfterTap}'.");
	}
}
#endif

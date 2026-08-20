#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34470 : _IssuesUITest
{
	public Issue34470(TestDevice device) : base(device) { }

	public override string Issue => "Modal with NavigationPage creates memory leaks";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ButtonHandlerIsCollectedAfterForwardModalNavigation()
	{
		App.WaitForElement("NavigateButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("RootPageState", "Root button loaded"),
			Is.True,
			"The root Navigate button did not reach its loaded state.");
		Assert.That(
			App.FindElement("HandlerState").GetText(),
			Is.EqualTo("HandlerIsAlive=Pending"),
			"The handler result must start at the pending sentinel.");

		App.Tap("NavigateButton");

		App.WaitForElement("ModalPageState");
		Assert.That(
			App.WaitForTextToBePresentInElement("SourceButtonState", "Source Navigate button unloaded"),
			Is.True,
			"The source Navigate button did not raise Unloaded after modal navigation.");
		Assert.That(
			App.WaitForTextToBePresentInElement("GcCheckState", "GC check complete", timeout: TimeSpan.FromSeconds(15)),
			Is.True,
			"The bounded handler collection check did not complete.");

		var handlerState = App.FindElement("ModalHandlerState").GetText();
		Assert.That(
			handlerState,
			Is.EqualTo("HandlerIsAlive=False"),
			$"Navigate ButtonHandler should be collected after unloading; observed {handlerState}, expected HandlerIsAlive=False");
	}
}
#endif

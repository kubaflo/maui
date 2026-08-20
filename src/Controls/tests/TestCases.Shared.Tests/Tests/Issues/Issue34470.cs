#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34470 : _IssuesUITest
{
	const string ResultLabelId = "ResultLabel";

	public Issue34470(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "Modal with NavigationPage creates memory leaks";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void ButtonHandlerIsCollectedAfterModalNavigationPagePresentation()
	{
		App.WaitForElement("NavigateButton");
		Assert.That(App.FindElement(ResultLabelId).GetText(), Is.EqualTo("Waiting for handler collection"));

		App.Tap("NavigateButton");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ModalReadyLabel",
				"Modal loaded: True",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The nested ContentPage did not complete its loaded transition.");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"ModalReadyLabel",
				"source unloaded: True",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The source ContentPage did not complete its outgoing lifecycle transition.");

		const string expectedResult = "Outgoing Button handler alive after modal NavigationPage presentation: False; expected False";
		Assert.That(
			App.WaitForTextToBePresentInElement(
				ResultLabelId,
				"Outgoing Button handler alive after modal NavigationPage presentation:",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The bounded handler collection result was not reported.");

		var actualResult = App.FindElement(ResultLabelId).GetText();
		Assert.That(actualResult, Is.EqualTo(expectedResult), actualResult);
	}
}
#endif

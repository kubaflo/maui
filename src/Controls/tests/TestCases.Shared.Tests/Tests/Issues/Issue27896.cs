#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27896 : _IssuesUITest
{
	public Issue27896(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Android system back does not invoke the activity OnBackPressedDispatcher callback for a modal page";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void SystemBackInvokesActivityCallbackForModalPage()
	{
		App.WaitForElement("Issue27896OriginalPage");
		App.WaitForElement("Issue27896OpenModal");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue27896CallbackState", "Callback registered: False; callback count: -1; modal disappeared: False"),
			Is.True,
			"The callback count must begin at its unregistered sentinel before opening the modal.");
		App.Tap("Issue27896OpenModal");

		App.WaitForElement("Issue27896ModalPage");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue27896CallbackState", "Callback registered: True; callback count: 0; modal disappeared: False"),
			Is.True,
			"The activity callback must be registered before sending the system back action.");

		App.Back();

		App.WaitForElement("Issue27896OriginalPage");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue27896CallbackState", "Modal disappeared: True; callback count:"),
			Is.True,
			"The modal Disappearing transition must complete after the system back action.");

		var callbackStateElement = App.WaitForElement("Issue27896CallbackState");
		if (callbackStateElement is null)
		{
			Assert.Fail("The callback state label was not found after the modal disappeared.");
			return;
		}

		var callbackState = callbackStateElement.GetText();
		if (callbackState is null)
		{
			Assert.Fail("The callback state label had no text after the modal disappeared.");
			return;
		}

		const string countPrefix = "Modal disappeared: True; callback count: ";
		Assert.That(callbackState, Does.StartWith(countPrefix));

		var countText = callbackState[countPrefix.Length..];
		Assert.That(int.TryParse(countText, out var callbackInvocationCount), Is.True,
			$"The activity callback invocation count '{countText}' was not an integer.");
		Assert.That(callbackInvocationCount, Is.EqualTo(1),
			$"Android activity back callback invocation count was {callbackInvocationCount} after modal disappearance; expected 1.");
	}
}
#endif

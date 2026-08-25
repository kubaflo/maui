#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29575 : _IssuesUITest
{
	public Issue29575(TestDevice device) : base(device)
	{
	}

	public override string Issue => "iOS WebView cancellation after an awaited Navigating handler";

	[Test]
	[Category(UITestCategories.WebView)]
	public void AwaitedNavigatingHandlerCancelsWebViewNavigation()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29575InitialHash", "Initial hash: <empty>"),
			Is.True,
			"The initial WebView document should have an empty location hash.");
		Assert.That(
			App.WaitForElement("Issue29575InitialHash").GetText(),
			Is.EqualTo("Initial hash: <empty>"));

		App.Tap("Issue29575WebView");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29575TriggerStatus", "Navigation received"),
			Is.True,
			"The Sign In tap should reach the WebView.Navigating callback.");
		App.Tap("Issue29575Return");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29575TriggerStatus", "Cancel set"),
			Is.True,
			"The awaited callback should resume and set WebNavigatingEventArgs.Cancel.");
		App.Tap("Issue29575Check");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29575MeasurementStatus", "Hash measured"),
			Is.True,
			"The post-trigger document hash should be measured.");

		var observedHash = App.WaitForElement("Issue29575Result").GetText();
		Assert.That(
			observedHash,
			Is.EqualTo("<empty>"),
			$"WebView navigation cancellation failed after awaited Navigating handler. Expected hash <empty>, observed hash {observedHash}.");
	}
}
#endif

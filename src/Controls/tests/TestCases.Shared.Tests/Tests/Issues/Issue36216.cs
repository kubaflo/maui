#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36216 : _IssuesUITest
{
	public override string Issue => "Accelerometer ReadingChanged retains stopped page subscribers";

	public Issue36216(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Essentials)]
	public void StoppedAccelerometerDoesNotRetainPoppedSubscriberPage()
	{
		var lifecycleStatus = App.WaitForElement("LifecycleStatus");
		if (lifecycleStatus is null)
		{
			Assert.Fail("Lifecycle status was not found.");
			return;
		}

		Assert.That(lifecycleStatus.GetText(), Is.EqualTo("Lifecycle: pending"));

		var retainedPages = App.WaitForElement("RetainedPages");
		if (retainedPages is null)
		{
			Assert.Fail("Retained-page status was not found.");
			return;
		}

		Assert.That(retainedPages.GetText(), Is.EqualTo("Retained pages: not checked"));

		App.WaitForElement("CreatePageButton");
		App.Tap("CreatePageButton");

		App.WaitForElement("CheckPageButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("LifecycleStatus", "Lifecycle: appearing=1, disappearing=1"),
			Is.True,
			"The subscriber page did not complete the expected push/pop lifecycle.");

		lifecycleStatus = App.FindElement("LifecycleStatus");
		if (lifecycleStatus is null)
		{
			Assert.Fail("Lifecycle status was not found after navigation.");
			return;
		}

		Assert.That(lifecycleStatus.GetText(), Is.EqualTo("Lifecycle: appearing=1, disappearing=1"));

		App.Tap("CheckPageButton");
		Assert.That(
			App.WaitForTextToBePresentInElement(
				"RetainedPages",
				"Popped subscriber pages retained:",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The popped subscriber page retention measurement did not complete.");

		retainedPages = App.FindElement("RetainedPages");
		if (retainedPages is null)
		{
			Assert.Fail("Retained-page status was not found after collection.");
			return;
		}

		var retainedPagesText = retainedPages.GetText();
		Assert.That(
			retainedPagesText,
			Does.EndWith("of 1"),
			"The retention measurement did not identify exactly one popped subscriber page.");
		Assert.That(
			retainedPagesText,
			Is.EqualTo("Popped subscriber pages retained: 0 of 1"),
			"Accelerometer.ReadingChanged retained the popped subscriber page after Stop()");
	}
}
#endif

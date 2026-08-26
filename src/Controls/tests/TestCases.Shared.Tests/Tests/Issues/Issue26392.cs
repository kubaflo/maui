#if ANDROID
using System.Diagnostics;

using NUnit.Framework;

using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26392 : _IssuesUITest
{
	const string CallbackSentinel = "FlyoutIsPresented callback: sentinel";
	const string CallbackPresented = "FlyoutIsPresented callback: True";

	public Issue26392(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Click on flyout clicks on page behind";

	[Test]
	[Category(UITestCategories.Shell)]
	public void BlankFlyoutContentConsumesTap()
	{
		var picker = App.WaitForElement("Issue26392FirstPicker");
		Assert.That(picker.GetText(), Is.EqualTo("Select a monkey"));

		var source = App.WaitForElement("Issue26392PickerSource");
		Assert.That(source.GetText(), Does.Contain("Baboon"));
		Assert.That(App.FindElementsByText("Baboon"), Is.Empty);

		var callback = App.WaitForElement("Issue26392FlyoutCallback");
		Assert.That(callback.GetText(), Is.EqualTo(CallbackSentinel));

		App.TapShellFlyoutIcon();

		bool callbackOccurred = App.WaitForTextToBePresentInElement(
			"Issue26392FlyoutCallback",
			CallbackPresented,
			TimeSpan.FromSeconds(2));
		Assert.That(callbackOccurred, Is.True, "FlyoutIsPresented callback did not occur");
		Assert.That(App.WaitForElement("Issue26392FlyoutCallback").GetText(), Is.EqualTo(CallbackPresented));

		App.WaitForElement("Issue26392MenuPage");
		App.WaitForElement("Issue26392FlyoutBlank");
		App.Tap("Issue26392FlyoutBlank");

		int optionCount = 0;
		var observationPeriod = TimeSpan.FromSeconds(2);
		var stopwatch = Stopwatch.StartNew();
		App.WaitForElement(() =>
		{
			var options = App.FindElementsByText("Baboon");
			optionCount = options.Count;
			if (optionCount > 0)
				return options.First();

			if (stopwatch.Elapsed < observationPeriod)
				return null;

			var flyoutElements = App.FindElements("Issue26392FlyoutBlank");
			return flyoutElements.Count > 0 ? flyoutElements.First() : null;
		}, timeout: TimeSpan.FromSeconds(3), retryFrequency: TimeSpan.FromMilliseconds(100));

		if (optionCount > 0)
			App.Back();

		Assert.That(
			optionCount,
			Is.Zero,
			$"Tap on blank Shell flyout content opened the Picker behind it; observed option count: {optionCount}; expected option count: 0");
	}
}
#endif

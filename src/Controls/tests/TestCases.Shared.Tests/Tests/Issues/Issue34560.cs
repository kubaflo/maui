#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34560 : _IssuesUITest
{
	public Issue34560(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Switch iOS Liquid glass rendering issue";

	[Test]
	[Category(UITestCategories.Switch)]
	public void DefaultSwitchOnStateMatchesNativeUISwitch()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("AffectedSwitch");
		App.WaitForElement("ResultLabel");

		if (!App.WaitForTextToBePresentInElement("ResultLabel", "Ready:", TimeSpan.FromSeconds(10)))
		{
			var setupResult = App.FindElement("ResultLabel").GetText();
			if (setupResult?.Contains("Unsupported iOS version below 26", StringComparison.Ordinal) == true)
			{
				return;
			}

			Assert.Fail($"Native rendering oracle was not ready: {setupResult}");
		}

		var readyResult = App.FindElement("ResultLabel").GetText();
		Assert.That(readyResult, Does.Contain("style=Light"));
		Assert.That(readyResult, Does.Contain("portrait=True"));

		var switchRect = App.FindElement("AffectedSwitch").GetRect();
		Assert.That(switchRect.Width, Is.InRange(40, 100), "The identified default UISwitch must have its expected absolute width.");
		Assert.That(switchRect.Height, Is.InRange(20, 60), "The identified default UISwitch must have its expected absolute height.");

		App.Tap("AffectedSwitch");
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultLabel", "managed=1", TimeSpan.FromSeconds(5)),
			Is.True,
			"The sentinel-backed Toggled callback did not observe the direct tap.");

		var result = App.FindElement("ResultLabel").GetText();
		Assert.That(result, Does.Contain("nativeOn=True"), "The native UISwitch did not enter its on state after the direct tap.");
		Assert.That(result, Does.StartWith("PASS:"), result);
	}
}
#endif

#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue25202 : _IssuesUITest
{
	public Issue25202(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Routed Shell page loses the styled toolbar background";

	[Test]
	[Category(UITestCategories.Shell)]
	public void RoutedPagePreservesStyledToolbarBackground()
	{
		App.SetOrientationPortrait();
		App.WaitForElement("SettingsTitle");
		App.WaitForElement(AppiumQuery.ByXPath("//*[@text='INITIAL_READY']"), timeout: TimeSpan.FromSeconds(20));

		var initial = ReadMeasurement("InitialMeasurement");
		Assert.That(initial, Does.Contain("|token=settings|"));
		Assert.That(initial, Does.Contain("|title=SettingsTitleView|"));
		Assert.That(initial, Does.Contain("|theme=Light|orientation=Portrait|"));
		Assert.That(initial, Does.Contain("|back=false"));
		AssertToolbarPixelsMatch(initial, "Initial Shell toolbar did not render the styled color.");

		App.Tap("NavigateToLogin");
		App.WaitForElement("LoginTitle");
		App.WaitForElement("UsernameEntry");
		App.WaitForElement(AppiumQuery.ByXPath("//android.widget.EditText[@focused='false']"));
		var backButton = App.WaitForElement(AppiumQuery.ByXPath("//android.widget.ImageButton"));
		if (backButton is null)
			throw new InvalidOperationException("The routed Shell back button lookup returned null.");

		Assert.That(backButton.GetRect().Width, Is.GreaterThan(0));
		App.WaitForElement(AppiumQuery.ByXPath("//*[@text='ROUTED_READY']"), timeout: TimeSpan.FromSeconds(20));

		var routed = ReadMeasurement("RoutedMeasurement");
		Assert.That(routed, Does.Contain("|title=LoginTitleView|"));
		Assert.That(routed, Does.Contain("|theme=Light|orientation=Portrait|"));
		Assert.That(routed, Does.Contain("|back=true"));
		Assert.That(ReadValue(routed, "token"), Is.Not.EqualTo(ReadValue(initial, "token")));
		AssertToolbarPixelsMatch(routed, "Routed Shell toolbar background did not preserve the styled color");
	}

	string ReadMeasurement(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new InvalidOperationException($"Measurement element '{automationId}' lookup returned null.");

		var text = element.GetText();
		if (text is null)
			throw new InvalidOperationException($"Measurement '{automationId}' returned null text.");

		return text;
	}

	static void AssertToolbarPixelsMatch(string measurement, string failureSignature)
	{
		var matching = ReadInt(measurement, "matching");
		var total = ReadInt(measurement, "total");
		Assert.That(total, Is.GreaterThan(0), $"{failureSignature}. No toolbar pixels were sampled.");

		var required = (int)Math.Ceiling(total * 0.95);
		Assert.That(
			matching,
			Is.GreaterThanOrEqualTo(required),
			$"{failureSignature}. Observed {ReadValue(measurement, "observed")}, expected {ReadValue(measurement, "expected")}; matching pixels: {matching}/{total}.");
	}

	static int ReadInt(string measurement, string key)
	{
		var value = ReadValue(measurement, key);
		if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
			Assert.Fail($"Measurement value '{key}' was not an integer: '{value}'.");

		return result;
	}

	static string ReadValue(string measurement, string key)
	{
		var prefix = $"{key}=";
		foreach (var part in measurement.Split('|'))
		{
			if (part.StartsWith(prefix, StringComparison.Ordinal))
				return part[prefix.Length..];
		}

		Assert.Fail($"Measurement did not contain '{key}': {measurement}");
		return string.Empty;
	}
}
#endif

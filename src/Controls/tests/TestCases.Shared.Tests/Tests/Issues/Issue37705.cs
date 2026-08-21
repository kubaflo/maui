#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37705 : _IssuesUITest
{
	public Issue37705(TestDevice device) : base(device) { }

	public override string Issue => "[Android] Status bar icons are unreadable when Material 3 is enabled";

	[Test]
	[Category(UITestCategories.Material3)]
	public void StatusBarContentContrastsWithMaterial3Background()
	{
		App.SetOrientationPortrait();

		var observationElement = App.WaitForElement(
			() =>
			{
				var element = App.FindElement("StatusBarObservation");
				string text = element?.GetText() ?? string.Empty;
				return text.StartsWith("UNOBSERVED:", StringComparison.Ordinal) ? null : element;
			},
			"Timed out waiting for the Android decor-view observation.");
		string observation = observationElement.GetText() ?? string.Empty;

		Assert.That(observation, Does.Not.StartWith("UNOBSERVED:"), "The decor-view observation sentinel did not change.");
		Assert.That(observation, Does.Not.StartWith("ERROR:"), "The native status-bar state could not be observed.");
		Assert.That(observation, Does.Contain("ATTACHED:True"), "The observed Android decor view was not attached.");

		bool isLightMode = observation.Contains("MODE:Light", StringComparison.Ordinal);
		bool isDarkMode = observation.Contains("MODE:Dark", StringComparison.Ordinal);
		Assert.That(isLightMode || isDarkMode, Is.True, $"Android returned an unsupported day/night mode. Observation: {observation}");
		Assert.That(
			observation,
			Does.Contain($"EXPECTED:{isLightMode}"),
			$"The captured expected status-bar state did not match the active mode. Observation: {observation}");

		int callbackStart = observation.IndexOf("CALLBACKS:", StringComparison.Ordinal) + "CALLBACKS:".Length;
		int callbackEnd = observation.IndexOf(';', callbackStart);
		int callbackCount = int.Parse(observation[callbackStart..callbackEnd], System.Globalization.CultureInfo.InvariantCulture);
		Assert.That(callbackCount, Is.GreaterThan(0), "The posted decor-view callback did not run.");
		int widthStart = observation.IndexOf("WIDTH:", StringComparison.Ordinal) + "WIDTH:".Length;
		int widthEnd = observation.IndexOf(';', widthStart);
		int width = int.Parse(observation[widthStart..widthEnd], System.Globalization.CultureInfo.InvariantCulture);
		int heightStart = observation.IndexOf("HEIGHT:", StringComparison.Ordinal) + "HEIGHT:".Length;
		int height = int.Parse(observation[heightStart..], System.Globalization.CultureInfo.InvariantCulture);
		Assert.That(width, Is.GreaterThan(0), "The native window width was not measured.");
		Assert.That(height, Is.GreaterThan(width), $"The native window was not in portrait orientation. Observation: {observation}");

		bool observedLightStatusBars = observation.Contains("OBSERVED:True", StringComparison.Ordinal);
		bool expectedLightStatusBars = isLightMode;
		Assert.That(
			observedLightStatusBars,
			Is.EqualTo(expectedLightStatusBars),
			$"Status bar icon appearance does not contrast with its Material 3 background: mode={(isLightMode ? "Light" : "Dark")}, observed AppearanceLightStatusBars={observedLightStatusBars}, expected={expectedLightStatusBars}. Observation: {observation}");
	}
}
#endif

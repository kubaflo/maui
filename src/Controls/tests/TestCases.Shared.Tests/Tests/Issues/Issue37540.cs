#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37540 : _IssuesUITest
{
	public Issue37540(TestDevice device) : base(device)
	{
	}

	public override string Issue => "SetDynamicResource does not update the Background property of the Label";

	[Test]
	[Category(UITestCategories.Label)]
	public void DynamicResourceUpdatesNativeLabelBackgroundAfterLoaded()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("ResultStatus", "Phase=Setup;", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The setup page did not publish its post-Loaded native sample.");

		var setupElement = App.WaitForElement("ResultStatus", timeout: TimeSpan.FromSeconds(10));
		if (setupElement is null)
			throw new AssertionException("The setup ResultStatus element was not found.");

		var setupText = setupElement.GetText();
		if (setupText is null)
			throw new AssertionException("The setup ResultStatus element had no text.");

		Assert.That(ReadDiagnostic(setupText, "Phase"), Is.EqualTo("Setup"));
		Assert.That(ReadDiagnostic(setupText, "Sampled"), Is.EqualTo("True"));
		Assert.That(ReadDiagnostic(setupText, "Resource"), Is.EqualTo("#FFFF0000"));
		Assert.That(ReadDiagnostic(setupText, "NativeView"), Is.EqualTo("True"));
		Assert.That(ReadDiagnostic(setupText, "NativeAlpha"), Is.EqualTo("0"));
		Assert.That(int.TryParse(ReadDiagnostic(setupText, "Loaded"), out var setupLoaded), Is.True);
		Assert.That(setupLoaded, Is.GreaterThan(0));
		var setupInstance = ReadDiagnostic(setupText, "Instance");
		var setupLoadSequence = ReadDiagnostic(setupText, "LoadSequence");

		var runButton = App.WaitForElement("RunScenarioButton", timeout: TimeSpan.FromSeconds(10));
		if (runButton is null)
			throw new AssertionException("The scenario button was not found.");

		App.Tap("RunScenarioButton");

		Assert.That(
			App.WaitForTextToBePresentInElement("ResultStatus", "Phase=Scenario;", timeout: TimeSpan.FromSeconds(10)),
			Is.True,
			"The freshly navigated page did not publish its post-Loaded native sample.");

		var scenarioElement = App.WaitForElement("ResultStatus", timeout: TimeSpan.FromSeconds(10));
		if (scenarioElement is null)
			throw new AssertionException("The scenario ResultStatus element was not found.");

		var scenarioText = scenarioElement.GetText();
		if (scenarioText is null)
			throw new AssertionException("The scenario ResultStatus element had no text.");

		Assert.That(ReadDiagnostic(scenarioText, "Phase"), Is.EqualTo("Scenario"));
		Assert.That(ReadDiagnostic(scenarioText, "Sampled"), Is.EqualTo("True"));
		var expectedNativeArgb = ReadDiagnostic(scenarioText, "Resource");
		Assert.That(expectedNativeArgb, Is.EqualTo("#FFFF0000"));
		Assert.That(ReadDiagnostic(scenarioText, "NativeView"), Is.EqualTo("True"));
		Assert.That(ReadDiagnostic(scenarioText, "Instance"), Is.Not.EqualTo(setupInstance));
		Assert.That(ReadDiagnostic(scenarioText, "LoadSequence"), Is.Not.EqualTo(setupLoadSequence));
		Assert.That(int.TryParse(ReadDiagnostic(scenarioText, "Loaded"), out var scenarioLoaded), Is.True);
		Assert.That(scenarioLoaded, Is.GreaterThan(0));
		var actualNativeArgb = ReadDiagnostic(scenarioText, "Native");
		Assert.That(
			actualNativeArgb,
			Is.EqualTo(expectedNativeArgb),
			$"Issue37540 native Label background after Loaded SetDynamicResource was {actualNativeArgb}; expected {expectedNativeArgb}.");
	}

	static string ReadDiagnostic(string diagnostics, string name)
	{
		foreach (var part in diagnostics.Split(';'))
		{
			var separator = part.IndexOf('=', StringComparison.Ordinal);
			if (separator > 0 && part[..separator] == name)
				return part[(separator + 1)..];
		}

		throw new AssertionException($"Diagnostic '{name}' was missing from '{diagnostics}'.");
	}
}
#endif

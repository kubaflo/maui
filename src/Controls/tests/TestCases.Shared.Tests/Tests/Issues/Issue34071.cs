#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34071 : _IssuesUITest
{
	public Issue34071(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Shell foreground color is not applied to toolbar items";

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellForegroundColorIsAppliedToPrimaryToolbarItem()
	{
		App.WaitForElement("Issue34071LaunchShellButton");
		App.Tap("Issue34071LaunchShellButton");

		App.WaitForNoElement(
			"Issue34071LaunchShellButton",
			"The initial page remained visible after replacing Window.Page with Shell",
			TimeSpan.FromSeconds(10));
		App.WaitForElement("Issue34071CheckToolbarButton");
		App.WaitForElement("Issue34071AffectedToolbarItem");
		App.WaitForElement("Issue34071ReferenceLabel");
		App.Tap("Issue34071CheckToolbarButton");

		var measurement = App.WaitForElement(
			() =>
			{
				var element = App.FindElement("Issue34071MeasurementLabel");
				return element?.GetText() is string text && text != "PENDING" ? element : null;
			},
			"Timed out waiting for the native Shell toolbar color measurement");

		var measurementText = measurement.GetText();
		if (measurementText is null)
		{
			throw new AssertionException("The completed native Shell toolbar measurement had no text");
		}

		Assert.That(measurementText, Does.StartWith("observed="),
			$"Native Shell toolbar measurement did not complete: {measurementText}");

		var parts = measurementText.Split(';');
		Assert.That(parts, Has.Length.EqualTo(2), $"Unexpected native color measurement: {measurementText}");

		var observedText = parts[0]["observed=".Length..];
		var expectedText = parts[1]["expected=".Length..];
		var observed = observedText.Split(',').Select(int.Parse).ToArray();
		var expected = expectedText.Split(',').Select(int.Parse).ToArray();
		Assert.That(observed, Has.Length.EqualTo(4), $"Unexpected observed RGBA: {observedText}");
		Assert.That(expected, Has.Length.EqualTo(4), $"Unexpected expected RGBA: {expectedText}");

		var purple = new[] { 128, 0, 128, 255 };
		var referenceChannelDifference = expected
			.Zip(purple, (actual, arranged) => Math.Abs(actual - arranged))
			.Max();
		Assert.That(
			referenceChannelDifference,
			Is.LessThanOrEqualTo(2),
			$"The rendered reference label did not preserve the arranged purple color: {expectedText}");

		var maximumChannelDifference = observed
			.Zip(expected, (actual, arranged) => Math.Abs(actual - arranged))
			.Max();

		Assert.That(
			maximumChannelDifference,
			Is.LessThanOrEqualTo(2),
			$"Shell toolbar foreground mismatch: observed {observedText}, expected {expectedText}");
	}
}
#endif

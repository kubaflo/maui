#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ContentViewDimensionsAreValidDuringTappedEvent()
	{
		const string notMeasured = "Gesture Width/Height: not measured";

		AssertRenderedItem("WrappedTapTarget");
		AssertRenderedItem("DirectTapTarget");
		Assert.That(GetRequiredText("WrappedMeasurementStatus"), Is.EqualTo(notMeasured));
		Assert.That(GetRequiredText("DirectMeasurementStatus"), Is.EqualTo(notMeasured));

		App.Tap("WrappedTapTarget");
		Assert.That(
			App.WaitForTextToBePresentInElement("WrappedMeasurementStatus", "Gesture Width="),
			Is.True,
			"Grid-wrapped ContentView tap callback should complete.");
		var wrappedDimensions = ParseDimensions(GetRequiredText("WrappedMeasurementStatus"));
		AssertPositiveDimensions(wrappedDimensions.Width, wrappedDimensions.Height, "Grid-wrapped");

		App.Tap("DirectTapTarget");
		Assert.That(
			App.WaitForTextToBePresentInElement("DirectMeasurementStatus", "Gesture Width="),
			Is.True,
			"Direct ContentView tap callback should complete.");
		var directDimensions = ParseDimensions(GetRequiredText("DirectMeasurementStatus"));
		AssertPositiveDimensions(directDimensions.Width, directDimensions.Height, "Direct");
	}

	void AssertRenderedItem(string automationId)
	{
		var element = App.WaitForElement(automationId);
		Assert.That(GetRequiredText(automationId), Is.EqualTo("Tap this custom ContentView"));

		var rectangle = element.GetRect();
		Assert.That(rectangle.Width, Is.GreaterThan(0), $"{automationId} should have positive rendered width.");
		Assert.That(rectangle.Height, Is.GreaterThan(0), $"{automationId} should have positive rendered height.");
	}

	string GetRequiredText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new AssertionException($"{automationId} should expose text.");

		return text;
	}

	static (double Width, double Height) ParseDimensions(string text)
	{
		const string widthPrefix = "Gesture Width=";
		const string heightSeparator = ", Height=";
		var separatorIndex = text.IndexOf(heightSeparator, StringComparison.Ordinal);

		if (!text.StartsWith(widthPrefix, StringComparison.Ordinal) || separatorIndex < 0)
			throw new AssertionException($"Unexpected gesture measurement text: '{text}'.");

		var widthText = text[widthPrefix.Length..separatorIndex];
		var heightText = text[(separatorIndex + heightSeparator.Length)..];
		if (!double.TryParse(widthText, NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
			!double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
		{
			throw new AssertionException($"Unable to parse gesture measurement text: '{text}'.");
		}

		return (width, height);
	}

	static void AssertPositiveDimensions(double width, double height, string scenario)
	{
		const string failureSignature = "ContentView dimensions captured by TapGestureRecognizer.Tapped must be positive after the CollectionView item is rendered;";

		Assert.That(width, Is.GreaterThan(0),
			$"{failureSignature} {scenario} Width={width}, Height={height}.");
		Assert.That(height, Is.GreaterThan(0),
			$"{failureSignature} {scenario} Width={width}, Height={height}.");
	}
}
#endif

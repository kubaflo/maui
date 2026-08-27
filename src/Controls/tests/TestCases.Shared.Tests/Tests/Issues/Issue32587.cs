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
	public void DirectlyTemplatedContentViewHasValidBoundsWhenTapped()
	{
		const double layoutTolerance = 0.01;

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587LoadedState", "Loaded", TimeSpan.FromSeconds(10)),
			Is.True,
			"The directly templated ContentView should reach its Loaded state.");

		var probe = App.WaitForElement("Issue32587BoundsProbe");
		if (probe is null)
			throw new AssertionException("The directly templated ContentView was not found.");

		var nativeFrame = probe.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(nativeFrame.Width, Is.GreaterThan(layoutTolerance), "The intended ContentView should have a visible native width.");
			Assert.That(nativeFrame.Height, Is.GreaterThan(layoutTolerance), "The intended ContentView should have a visible native height.");
			Assert.That(GetRequiredText("Issue32587TapCount"), Is.EqualTo("0"), "The tap callback should not have run before the Appium tap.");
			Assert.That(GetRequiredText("Issue32587MeasurementState"), Is.EqualTo("Not measured"), "Callback dimensions should retain their sentinel before the Appium tap.");
			Assert.That(GetRequiredText("Issue32587TapWidth"), Is.EqualTo("Not measured"), "Callback width should retain its sentinel before the Appium tap.");
			Assert.That(GetRequiredText("Issue32587TapHeight"), Is.EqualTo("Not measured"), "Callback height should retain its sentinel before the Appium tap.");
		});

		App.Tap("Issue32587BoundsProbe");

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587TapCount", "1", TimeSpan.FromSeconds(10)),
			Is.True,
			"The tap callback count should transition to one.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue32587MeasurementState", "Dimensions captured", TimeSpan.FromSeconds(10)),
			Is.True,
			"The tap callback should record its dimensions.");
		Assert.Multiple(() =>
		{
			Assert.That(GetRequiredText("Issue32587TapCount"), Is.EqualTo("1"), "The tap callback should run exactly once.");
			Assert.That(GetRequiredText("Issue32587MeasurementState"), Is.EqualTo("Dimensions captured"), "The tap callback should finish recording its dimensions.");
		});

		var widthText = GetRequiredText("Issue32587TapWidth");
		var heightText = GetRequiredText("Issue32587TapHeight");
		var widthParsed = double.TryParse(widthText, NumberStyles.Float, CultureInfo.InvariantCulture, out var width);
		var heightParsed = double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var height);

		Assert.Multiple(() =>
		{
			Assert.That(widthParsed, Is.True, $"Tap callback width '{widthText}' should be invariant-culture numeric text.");
			Assert.That(heightParsed, Is.True, $"Tap callback height '{heightText}' should be invariant-culture numeric text.");
		});
		Assert.That(
			width > layoutTolerance && height > layoutTolerance,
			Is.True,
			$"Issue32587: tap handler observed invalid ContentView bounds. Width={widthText}, Height={heightText}; expected both to be greater than {layoutTolerance}.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new AssertionException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}
}
#endif

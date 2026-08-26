#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32217 : _IssuesUITest
{
	public override string Issue => "RTL Editor spaces render inconsistently";

	public Issue32217(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Editor)]
	public void RtlEditorSpacesImmediatelyAdvanceRenderedCaret()
	{
		var appiumApp = App as AppiumApp;
		Assert.That(appiumApp, Is.Not.Null, "The iOS test requires the Appium driver.");
		if (appiumApp is null)
			return;

		var platformVersionText = appiumApp.Driver.Capabilities.GetCapability("platformVersion") as string;
		Assert.That(platformVersionText, Is.Not.Null, "The iOS platformVersion capability is required.");
		if (platformVersionText is null)
			return;

		Assert.That(Version.TryParse(platformVersionText, out var platformVersion), Is.True,
			$"Could not parse the iOS platform version '{platformVersionText}'.");
		if (platformVersion is null || platformVersion.Major < 26)
			return;

		var measurementElement = App.WaitForElement("CaretMeasurement");
		var initialMeasurement = measurementElement.GetText();
		Assert.That(initialMeasurement, Is.Not.Null, "The initial caret measurement must be readable.");
		Assert.That(initialMeasurement, Is.EqualTo("-1|sentinel"));

		var editorElement = App.WaitForElement("RtlEditor");
		var initialEditorText = editorElement.GetText();
		Assert.That(initialEditorText, Is.Not.Null, "The initially empty Editor text must be readable.");
		Assert.That(initialEditorText, Is.EqualTo(string.Empty));

		App.Tap("RtlEditor");

		editorElement.SendKeys("This");
		var afterThis = ReadMeasurement(editorElement, "This", -1);

		editorElement.SendKeys(" ");
		var afterFirstSpace = ReadMeasurement(editorElement, "This ", afterThis.Token);
		AssertSpaceAdvance(afterThis, afterFirstSpace, "first");

		editorElement.SendKeys("is");
		var afterIs = ReadMeasurement(editorElement, "This is", afterFirstSpace.Token);

		editorElement.SendKeys(" ");
		var afterSecondSpace = ReadMeasurement(editorElement, "This is ", afterIs.Token);

		editorElement.SendKeys("a");
		var afterA = ReadMeasurement(editorElement, "This is a", afterSecondSpace.Token);

		editorElement.SendKeys(" ");
		var afterThirdSpace = ReadMeasurement(editorElement, "This is a ", afterA.Token);

		editorElement.SendKeys("test");
		var finalMeasurement = ReadMeasurement(editorElement, "This is a test", afterThirdSpace.Token);
		Assert.That(finalMeasurement.NativeText, Is.EqualTo("This is a test"));

		Assert.Multiple(() =>
		{
			AssertSpaceAdvance(afterIs, afterSecondSpace, "second");
			AssertSpaceAdvance(afterA, afterThirdSpace, "third");
		});
	}

	CaretMeasurement ReadMeasurement(IUIElement editorElement, string expectedText, int previousToken)
	{
		var encodedText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(expectedText));
		Assert.That(
			App.WaitForTextToBePresentInElement("CaretMeasurement", $"|{encodedText}|", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			$"No post-input native caret measurement was published for '{expectedText}'.");

		var editorText = editorElement.GetText();
		Assert.That(editorText, Is.Not.Null, "The Editor text must be readable after input.");
		Assert.That(editorText, Is.EqualTo(expectedText));

		var measurementText = App.WaitForElement("CaretMeasurement").GetText();
		Assert.That(measurementText, Is.Not.Null, "The native caret measurement must be readable.");
		if (measurementText is null)
		{
			Assert.Fail("The native caret measurement was null.");
			return default;
		}

		var values = measurementText.Split('|');
		Assert.That(values, Has.Length.EqualTo(15), $"Unexpected native caret measurement: {measurementText}");

		var measurement = new CaretMeasurement(
			int.Parse(values[0], CultureInfo.InvariantCulture),
			System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(values[1])),
			System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(values[2])),
			values[3] == "1",
			values[4] == "1",
			int.Parse(values[5], CultureInfo.InvariantCulture),
			double.Parse(values[6], CultureInfo.InvariantCulture),
			double.Parse(values[7], CultureInfo.InvariantCulture),
			double.Parse(values[8], CultureInfo.InvariantCulture),
			double.Parse(values[9], CultureInfo.InvariantCulture),
			double.Parse(values[10], CultureInfo.InvariantCulture),
			double.Parse(values[11], CultureInfo.InvariantCulture),
			double.Parse(values[12], CultureInfo.InvariantCulture),
			double.Parse(values[13], CultureInfo.InvariantCulture),
			double.Parse(values[14], CultureInfo.InvariantCulture));

		Assert.Multiple(() =>
		{
			Assert.That(measurement.Token, Is.GreaterThan(previousToken), "The post-input callback token did not advance.");
			Assert.That(measurement.ExpectedText, Is.EqualTo(expectedText));
			Assert.That(measurement.NativeText, Is.EqualTo(expectedText));
			Assert.That(measurement.IsAttached, Is.True, "The native Editor must remain attached to its window.");
			Assert.That(measurement.IsFocused, Is.True, "The native Editor must remain focused during text input.");
			Assert.That(measurement.SelectionOffset, Is.EqualTo(expectedText.Length), "The native insertion point must follow the entered text.");
			Assert.That(measurement.CaretWidth, Is.GreaterThan(0));
			Assert.That(measurement.CaretHeight, Is.GreaterThan(0));
			Assert.That(measurement.EditorWidth, Is.GreaterThan(0));
			Assert.That(measurement.EditorHeight, Is.GreaterThan(0));
			Assert.That(measurement.CaretX, Is.InRange(measurement.EditorX, measurement.EditorX + measurement.EditorWidth));
			Assert.That(measurement.CaretY, Is.InRange(measurement.EditorY, measurement.EditorY + measurement.EditorHeight));
			Assert.That(measurement.SpaceAdvance, Is.GreaterThan(0));
		});

		return measurement;
	}

	static void AssertSpaceAdvance(CaretMeasurement beforeSpace, CaretMeasurement afterSpace, string ordinal)
	{
		var actualAdvance = Math.Abs(afterSpace.CaretX - beforeSpace.CaretX);
		Assert.That(actualAdvance, Is.EqualTo(afterSpace.SpaceAdvance).Within(1.0),
			$"RTL Editor spaces did not advance the rendered caret immediately: {ordinal} space advanced {actualAdvance:F3} points; active-font space width was {afterSpace.SpaceAdvance:F3} points.");
	}

	readonly record struct CaretMeasurement(
		int Token,
		string ExpectedText,
		string NativeText,
		bool IsAttached,
		bool IsFocused,
		int SelectionOffset,
		double CaretX,
		double CaretY,
		double CaretWidth,
		double CaretHeight,
		double EditorX,
		double EditorY,
		double EditorWidth,
		double EditorHeight,
		double SpaceAdvance);
}
#endif

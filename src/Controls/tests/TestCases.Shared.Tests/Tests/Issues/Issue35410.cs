#if IOS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35410 : _IssuesUITest
{
	const string InstructionText = "1. Enter the string 'hi my name is james. nice to meet you.' exactly (case-sensitive).";

	public Issue35410(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Text is obscured by the notch after counter-clockwise rotation";

	[Test]
	[Category(UITestCategories.SafeAreaEdges)]
	public void ContentRemainsOutsideNotchAfterLandscapeRotation()
	{
		App.WaitForElement("Issue35410FirstInstruction");

		// Rotate only after the NavigationPage and its root ContentPage are attached.
		App.SetOrientationPortrait();
		App.WaitForElement(AppiumQuery.ByXPath("//*[@label='PORTRAIT_LAYOUT_COMPLETE']"));

		var portraitInstruction = App.WaitForElement("Issue35410FirstInstruction");
		Assert.That(portraitInstruction.GetText(), Is.EqualTo(InstructionText));
		Assert.That(portraitInstruction.GetRect().Width, Is.GreaterThan(0));
		Assert.That(portraitInstruction.GetRect().Height, Is.GreaterThan(0));

		App.SetOrientationLandscape();
		var statusElement = App.WaitForElement(
			AppiumQuery.ByXPath("//*[contains(@label, 'LANDSCAPE_LAYOUT_COMPLETE|')]"),
			timeout: TimeSpan.FromSeconds(10));

		var status = statusElement.GetText();
		Assert.That(status, Is.Not.Null);
		var measurements = status!.Split('|');
		Assert.That(measurements, Has.Length.EqualTo(8));

		var windowWidth = double.Parse(measurements[1], CultureInfo.InvariantCulture);
		var windowHeight = double.Parse(measurements[2], CultureInfo.InvariantCulture);
		var safeAreaLeft = double.Parse(measurements[3], CultureInfo.InvariantCulture);
		var nativeInstructionX = double.Parse(measurements[4], CultureInfo.InvariantCulture);
		var nativeInstructionY = double.Parse(measurements[5], CultureInfo.InvariantCulture);
		var nativeInstructionWidth = double.Parse(measurements[6], CultureInfo.InvariantCulture);
		var nativeInstructionHeight = double.Parse(measurements[7], CultureInfo.InvariantCulture);

		Assert.That(windowWidth, Is.GreaterThan(windowHeight), "The native window did not complete its landscape layout.");
		Assert.That(safeAreaLeft, Is.GreaterThan(0), "The selected iOS device does not have a nonzero landscape-left safe-area inset.");
		Assert.That(nativeInstructionX, Is.GreaterThanOrEqualTo(0));
		Assert.That(nativeInstructionY, Is.GreaterThan(0).And.LessThan(windowHeight / 2), "The first D6 instruction is not at the expected top-of-content location.");
		Assert.That(nativeInstructionWidth, Is.GreaterThan(0));
		Assert.That(nativeInstructionHeight, Is.GreaterThan(0));

		var landscapeInstruction = App.WaitForElement("Issue35410FirstInstruction");
		Assert.That(landscapeInstruction.GetText(), Is.EqualTo(InstructionText));
		var instructionRect = landscapeInstruction.GetRect();
		Assert.That(instructionRect.Width, Is.GreaterThan(0));
		Assert.That(instructionRect.Height, Is.GreaterThan(0));
		Assert.That(nativeInstructionX, Is.GreaterThanOrEqualTo(safeAreaLeft),
			"The first D6 instruction must begin at or beyond the nonzero landscape safe-area inset");
	}
}
#endif

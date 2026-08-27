#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30812 : _IssuesUITest
{
	const string ResizeObservationId = "Issue30812ResizeObservation";

	public Issue30812(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Unnecessary More options button appears after resizing";

	[Test]
	[Category(UITestCategories.ToolbarItem)]
	public void SinglePrimaryToolbarItemDoesNotCreateOverflowAfterResize()
	{
		App.WaitForElement("Issue30812ResizeButton");

		App.Tap("Issue30812ResizeButton");

		var resizeObservation = App.WaitForElement(() =>
		{
			var element = App.FindElement(ResizeObservationId);
			if (element is null)
				return null;

			var text = element.GetText();
			if (text is null || !text.StartsWith("Resize callback width: ", StringComparison.Ordinal))
				return null;

			var widthText = text["Resize callback width: ".Length..];
			return double.TryParse(widthText, NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
				&& Math.Abs(width - 500) <= 2
					? element
					: null;
		}, "The page did not receive a SizeChanged callback at the requested width");

		var observationText = resizeObservation.GetText();
		if (observationText is null)
			throw new AssertionException("The resize observation had no text");

		Assert.That(observationText, Does.StartWith("Resize callback width: "),
			"The page SizeChanged callback should report the post-resize width");

		var measuredWidthText = observationText["Resize callback width: ".Length..];
		Assert.That(
			double.TryParse(measuredWidthText, NumberStyles.Float, CultureInfo.InvariantCulture, out var measuredWidth),
			Is.True,
			"The SizeChanged callback should report a numeric width");
		Assert.That(measuredWidth, Is.EqualTo(500).Within(2),
			"The real MAUI window should resize to 500 device-independent units");

		var observedMoreOptionsCount = App.FindElements("MoreButton").Count;

		Assert.That(observedMoreOptionsCount, Is.Zero,
			$"Issue30812: unnecessary More options button appeared after resize; observed count {observedMoreOptionsCount}, measured width {measuredWidth}");
	}
}
#endif

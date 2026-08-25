#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28542 : _IssuesUITest
{
	const double ThumbTolerance = 2;
	const string FailureSignature = "CollectionView scrollbar thumb must use the full variable-height content range";

	public Issue28542(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView scrollbar sizing with variable-height items";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ScrollbarThumbUsesFullVariableHeightContentRange()
	{
		App.SetOrientationPortrait();
		Assert.That(App.WaitForTextToBePresentInElement("ReadyStatus", "Ready"), Is.True);

		var root = App.WaitForElement("RootLayout").GetRect();
		Assert.That(root.Height, Is.GreaterThan(root.Width), "The recorded prerequisite is a portrait window.");

		Assert.That(App.WaitForTextToBePresentInElement("MeasurementStatus", "#"), Is.True);
		var measurementText = App.WaitForElement("MeasurementStatus").GetText();
		Assert.That(measurementText, Is.Not.Null);
		var readyMeasurements = measurementText!.Split('#');
		Assert.That(readyMeasurements, Has.Length.EqualTo(2));
		var calibration = readyMeasurements[0].Split('|');
		Assert.That(calibration, Has.Length.EqualTo(6));
		var calibrationActual = ParseDouble(calibration[0]);
		var calibrationExpected = ParseDouble(calibration[1]);
		var density = ParseDouble(calibration[4]);
		var calibrationItemHeight = ParseInt(calibration[5]);
		Assert.That(calibrationItemHeight, Is.EqualTo(70 * density).Within(ThumbTolerance));
		Assert.That(calibrationActual, Is.EqualTo(calibrationExpected).Within(ThumbTolerance),
			"The uniform-height control must validate the native scrollbar thumb oracle.");

		App.WaitForElement("Short row 1");

		var initial = readyMeasurements[1].Split('|');
		Assert.That(initial, Has.Length.EqualTo(5));
		var initialThumb = ParseDouble(initial[2]);
		Assert.That(initialThumb, Is.GreaterThan(0));
		Assert.That(ParseInt(initial[3]), Is.Zero);
		Assert.That(ParseInt(initial[4]), Is.EqualTo(-1), "No visible-item callback may be recorded before the touch drag.");

		var collection = App.WaitForElement("VariableHeightCollection").GetRect();
		var dragX = collection.X + (collection.Width / 2);
		var dragStartY = collection.Y + (collection.Height * 0.85f);
		var dragEndY = collection.Y + (collection.Height * 0.25f);
		Assert.That(dragX, Is.InRange(collection.X, collection.X + collection.Width));
		Assert.That(dragStartY, Is.InRange(collection.Y, collection.Y + collection.Height));
		Assert.That(dragEndY, Is.InRange(collection.Y, collection.Y + collection.Height));

		App.DragCoordinates(dragX, dragStartY, dragX, dragEndY);
		Assert.That(App.WaitForTextToBePresentInElement("ReadyStatus", "Idle in tall rows"), Is.True);

		App.Tap("CheckScrollbarButton");
		Assert.That(App.WaitForTextToBePresentInElement("ReadyStatus", "Measurement complete"), Is.True);
		var resultText = App.WaitForElement("MeasurementStatus").GetText();
		Assert.That(resultText, Is.Not.Null);
		var completedMeasurements = resultText!.Split('#');
		Assert.That(completedMeasurements, Has.Length.EqualTo(3));
		var result = completedMeasurements[2].Split('|');
		Assert.That(result, Has.Length.EqualTo(10));

		var actualThumb = ParseDouble(result[0]);
		var expectedThumb = ParseDouble(result[1]);
		var callbackCount = ParseInt(result[6]);
		var lastVisibleIndex = ParseInt(result[7]);
		var tallRowIndex = ParseInt(result[8]);
		var nativeTallRowHeight = ParseInt(result[9]);

		Assert.That(callbackCount, Is.GreaterThan(0), "A Scrolled callback must occur after the touch drag.");
		Assert.That(lastVisibleIndex, Is.GreaterThanOrEqualTo(8), "The drag must reach the tall-item region.");
		Assert.That(tallRowIndex, Is.EqualTo(8), "Tall row 9 must be the visible adapter item at index 8.");
		Assert.That(nativeTallRowHeight, Is.EqualTo(260 * density).Within(ThumbTolerance));

		if (Math.Abs(actualThumb - expectedThumb) > ThumbTolerance)
			Assert.Fail(FailureSignature);
	}

	static double ParseDouble(string value) => double.Parse(value, CultureInfo.InvariantCulture);

	static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);
}
#endif

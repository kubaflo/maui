using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26449 : _IssuesUITest
{
	const string InitialTelemetry = "Source=Unset; Sequence=-1; InnerEvents=0; OuterEvents=0; InnerDelta=0; OuterDelta=0";
	const string TelemetryId = "ScrollTelemetry";

	public Issue26449(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Unable to scroll inner CollectionView of nested CollectionViews";

#if ANDROID
	[Test]
	[Category(UITestCategories.CollectionView)]
	public void InnerCollectionViewExclusivelyReceivesUpwardDrag()
	{
		App.SetOrientationPortrait();

		if (App is not AppiumAndroidApp androidApp)
		{
			Assert.Fail("Issue26449 requires the Android Appium runner.");
			return;
		}

		var windowSize = androidApp.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires portrait orientation.");
		App.WaitForElement("Outer item 1");
		var innerItem = App.WaitForElement("Group 1, inner item 3");
		var initialTelemetry = App.WaitForElement(TelemetryId).GetText();
		Assert.That(initialTelemetry, Is.Not.Null, "Initial scroll telemetry should be available.");
		Assert.That(initialTelemetry, Is.EqualTo(InitialTelemetry));

		var itemRect = innerItem.GetRect();
		var startX = itemRect.CenterX();
		var startY = itemRect.CenterY();
		var dragDistance = (int)Math.Round(windowSize.Height * 0.15);

		App.DragCoordinates(startX, startY, startX, startY - dragDistance);

		var callbackObserved = App.WaitForTextToBePresentInElement(
			TelemetryId,
			"CallbackObserved=True",
			timeout: TimeSpan.FromSeconds(5));
		Assert.That(callbackObserved, Is.True, "A nonzero Scrolled callback should occur after the upward drag.");

		var telemetry = App.WaitForElement(TelemetryId).GetText();
		if (telemetry is null)
		{
			Assert.Fail("Scroll telemetry should be available after the drag.");
			return;
		}

		var values = telemetry.Split(';')
			.Select(part => part.Trim().Split('=', 2))
			.ToDictionary(part => part[0], part => part[1]);

		var source = values["Source"];
		var sequence = int.Parse(values["Sequence"]);
		var innerEvents = int.Parse(values["InnerEvents"]);
		var outerEvents = int.Parse(values["OuterEvents"]);
		var innerDelta = values["InnerDelta"];
		var outerDelta = values["OuterDelta"];

		Assert.That(sequence, Is.GreaterThan(-1), "The Scrolled callback sequence should advance from its sentinel.");
		Assert.That(
			innerEvents > 0 && outerEvents == 0,
			Is.True,
			$"Inner CollectionView should exclusively receive the upward drag; observed source={source}, innerEvents={innerEvents}, outerEvents={outerEvents}, innerDelta={innerDelta}, outerDelta={outerDelta}; expected innerEvents>0 and outerEvents=0.");
	}
#endif
}

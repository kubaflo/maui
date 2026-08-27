#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30203 : _IssuesUITest
{
	const string AliceBlueArgb = "#FFF0F8FF";

	public Issue30203(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "Unable to adjust the window background color visible when navigating";

	[Test]
	[Category(UITestCategories.Navigation)]
	public void NavigationFrameUsesPageBackgroundDuringAnimatedPush()
	{
		App.WaitForElement("Issue30203PageAMarker");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue30203InitialMeasurement",
				$"pageArgb={AliceBlueArgb}; expectedArgb={AliceBlueArgb}; frameArgb=UNSET",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"Page A should be attached with the arranged AliceBlue native background before navigation.");

		var initialElement = App.FindElement("Issue30203InitialMeasurement");
		Assert.That(initialElement, Is.Not.Null);
		if (initialElement is null)
		{
			Assert.Fail("The Page A native measurement element was not found.");
			return;
		}

		var initialMeasurement = initialElement.GetText();
		Assert.That(initialMeasurement, Is.Not.Null);
		if (initialMeasurement is null)
		{
			Assert.Fail("The Page A native measurement text was null.");
			return;
		}

		Assert.That(initialMeasurement, Does.Contain("callbackCount=0; stackCount=1;"));

		App.Tap("Issue30203NavigateButton");
		App.WaitForElement("Issue30203PageBMarker");

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"Issue30203FinalMeasurement",
				"callbackCount=1; stackCount=2;",
				TimeSpan.FromSeconds(10)),
			Is.True,
			"The native Frame.Navigating callback should run once after the NavigationPage stack advances.");

		var finalElement = App.FindElement("Issue30203FinalMeasurement");
		Assert.That(finalElement, Is.Not.Null);
		if (finalElement is null)
		{
			Assert.Fail("The Page B native measurement element was not found.");
			return;
		}

		var finalMeasurement = finalElement.GetText();
		Assert.That(finalMeasurement, Is.Not.Null);
		if (finalMeasurement is null)
		{
			Assert.Fail("The Page B native measurement text was null.");
			return;
		}

		Assert.That(
			finalMeasurement,
			Does.Contain($"pageArgb={AliceBlueArgb}; expectedArgb={AliceBlueArgb};"),
			"Page B should be attached with the arranged AliceBlue native background.");
		Assert.That(
			finalMeasurement,
			Does.Contain($"frameArgb={AliceBlueArgb}"),
			$"Navigation Frame background did not match the arranged AliceBlue page background during animated PushAsync. Measurement: {finalMeasurement}");
	}
}
#endif

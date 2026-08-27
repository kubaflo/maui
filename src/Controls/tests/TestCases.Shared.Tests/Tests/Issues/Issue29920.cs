#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29920 : _IssuesUITest
{
	public Issue29920(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Android tap event passes through overlapping containers";

	[Test]
	[Category(UITestCategories.InputTransparent)]
	public void OverlappingStackLayoutsBlockTapsToLowerBoxView()
	{
		const string metricsId = "Issue29920Metrics";
		const string initialMetrics = "Top taps: 0; Bottom taps: 0";
		const string expectedMetrics = "Top taps: 1; Bottom taps: 0";
		const string buggyMetrics = "Top taps: 1; Bottom taps: 1";

		App.WaitForElement("Issue29920TopBox");
		App.WaitForElement("Issue29920MiddleBox");
		App.WaitForElement("Issue29920BottomBox");
		Assert.That(
			App.WaitForTextToBePresentInElement(metricsId, initialMetrics, TimeSpan.FromSeconds(5)),
			Is.True,
			"The page should complete layout before input is sent");

		App.Tap("Issue29920TopBox");
		Assert.That(
			App.WaitForTextToBePresentInElement(metricsId, expectedMetrics, TimeSpan.FromSeconds(5)),
			Is.True,
			"The unobscured top BoxView TapGestureRecognizer should receive the tap");

		App.Tap("Issue29920BottomBox");
		Assert.That(
			App.WaitForTextToBePresentInElement(metricsId, buggyMetrics, TimeSpan.FromSeconds(5)),
			Is.False,
			"Obscured bottom-layer BoxView received a tap through two higher StackLayouts");
	}
}
#endif

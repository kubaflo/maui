using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37323 : _IssuesUITest
{
	public Issue37323(TestDevice device) : base(device) { }

	public override string Issue => "ScrollView padding does not update after a bound runtime change";

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void BoundPaddingUpdatesVisibleContentInsets()
	{
		const double paddingDip = 48;
		const double geometryTolerance = 3;

		App.SetOrientationLandscape();

		var windowRect = App.WaitForElement("Issue37323Window").GetRect();
		Assert.That(windowRect.Width, Is.GreaterThan(windowRect.Height), "The test requires a landscape window.");

		var topRowRect = App.WaitForElement("TopTestArea").GetRect();
		var leftColumnRect = App.WaitForElement("LeftColumn").GetRect();
		var viewportBefore = App.WaitForElement("ScrollHost").GetRect();
		var contentBefore = App.WaitForElement("ContentStart").GetRect();
		var initialStatus = App.WaitForElement("TransitionStatus").GetText();

		Assert.That(initialStatus, Is.EqualTo("NOT STARTED"), "The transition status must begin at its sentinel value.");
		Assert.That(topRowRect.Height, Is.GreaterThan(0), "The fixed 90-DIP top row must be rendered.");
		Assert.That(leftColumnRect.Width, Is.GreaterThan(0), "The fixed 70-DIP side column must be rendered.");
		Assert.Multiple(() =>
		{
			Assert.That(contentBefore.X, Is.EqualTo(viewportBefore.X).Within(geometryTolerance), "CONTENT START must initially align with the ScrollView left edge.");
			Assert.That(contentBefore.Y, Is.EqualTo(viewportBefore.Y).Within(geometryTolerance), "CONTENT START must initially align with the ScrollView top edge.");
			Assert.That(contentBefore.Width, Is.EqualTo(viewportBefore.Width).Within(geometryTolerance), "CONTENT START must initially span the ScrollView width.");
		});

		App.Tap("ApplyPaddingButton");
		App.WaitForElement(AppiumQuery.ByAccessibilityId("ApplyCompleted"), timeout: TimeSpan.FromSeconds(5));

		App.Tap("CheckPaddingButton");
		App.WaitForElement(AppiumQuery.ByAccessibilityId("CheckCompleted"), timeout: TimeSpan.FromSeconds(5));

		var horizontalPixelsPerDip = leftColumnRect.Width / 70d;
		var verticalPixelsPerDip = topRowRect.Height / 90d;

		App.RetryAssert(() =>
		{
			var viewportAfter = App.FindElement("ScrollHost").GetRect();
			var contentAfter = App.FindElement("ContentStart").GetRect();

			Assert.That(
				contentAfter.X,
				Is.EqualTo(viewportAfter.X + (paddingDip * horizontalPixelsPerDip)).Within(geometryTolerance),
				"Bound ScrollView.Padding should inset CONTENT START by 48 device-independent units");
		});

		var finalViewport = App.FindElement("ScrollHost").GetRect();
		var finalContent = App.FindElement("ContentStart").GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(finalViewport.X, Is.EqualTo(viewportBefore.X).Within(geometryTolerance), "Updating Padding must not move the ScrollView viewport horizontally.");
			Assert.That(finalViewport.Y, Is.EqualTo(viewportBefore.Y).Within(geometryTolerance), "Updating Padding must not move the ScrollView viewport vertically.");
			Assert.That(finalViewport.Width, Is.EqualTo(viewportBefore.Width).Within(geometryTolerance), "Updating Padding must not resize the ScrollView viewport.");
			Assert.That(
				finalContent.Y,
				Is.EqualTo(finalViewport.Y + (paddingDip * verticalPixelsPerDip)).Within(geometryTolerance),
				"Bound ScrollView.Padding should inset CONTENT START from the top by 48 device-independent units");
			Assert.That(
				finalContent.Width,
				Is.EqualTo(finalViewport.Width - ((paddingDip * 2) * horizontalPixelsPerDip)).Within(geometryTolerance),
				"Bound ScrollView.Padding should reduce CONTENT START width by 96 device-independent units");
		});
	}
}

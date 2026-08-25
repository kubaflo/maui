#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34780 : _IssuesUITest
{
	const double FrameTolerance = 3;

	public Issue34780(TestDevice device) : base(device)
	{
	}

	public override string Issue => "iOS 26 TabBar has opaque background";

	[Test]
	[Category(UITestCategories.TabbedPage)]
	public void DefaultContentExtendsBehindLiquidGlassTabBar()
	{
		var platformVersionValue = ((AppiumApp)App).Driver.Capabilities.GetCapability("platformVersion")
			?? throw new InvalidOperationException("platformVersion capability is missing.");
		if (!Version.TryParse(platformVersionValue.ToString(), out var platformVersion))
			throw new InvalidOperationException($"Invalid platformVersion capability: {platformVersionValue}");
		if (platformVersion.Major < 26)
			Assert.Ignore("The liquid-glass tab bar requires iOS 26 or later.");

		App.SetOrientationPortrait();

		App.WaitForElement(() =>
		{
			var element = App.FindElement(AppiumQuery.ByXPath("//XCUIElementTypeWindow"));
			if (element is null)
				return null;

			var frame = element.GetRect();
			return frame.Height > frame.Width && frame.Width > 0 ? element : null;
		}, "The native window did not settle in portrait.");

		var screenSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var correctGrid = App.WaitForElement("CorrectGrid");
		var correctContent = App.WaitForElement("CorrectContent");
		var correctHeader = App.WaitForElement("CorrectHeader");
		var correctFirstRow = App.WaitForElement("CorrectRow1");
		Assert.That(correctContent, Is.Not.Null);
		Assert.That(correctHeader.GetRect().Y, Is.GreaterThanOrEqualTo(correctGrid.GetRect().Y));
		Assert.That(correctFirstRow.GetRect().Y, Is.GreaterThanOrEqualTo(correctHeader.GetRect().Y + correctHeader.GetRect().Height));

		var correctFrame = correctGrid.GetRect();
		var windowBottom = screenSize.Height;
		var correctBottom = correctFrame.Y + correctFrame.Height;
		Assert.That(correctBottom, Is.GreaterThan(windowBottom + FrameTolerance),
			"The compensated reference Grid must extend beyond the physical screen bottom.");

		var incorrectBottom = double.NaN;
		App.Tap("Incorrect");

		App.WaitForNoElement("CorrectContent");
		Assert.That(App.FindElement("CorrectContent"), Is.Null, "Correct content remained visible after selecting Incorrect.");

		var incorrectContent = App.WaitForElement("IncorrectContent");
		var incorrectHeader = App.WaitForElement("IncorrectHeader");
		var incorrectFirstRow = App.WaitForElement("IncorrectRow1");
		Assert.That(incorrectContent, Is.Not.Null);

		var incorrectGrid = App.WaitForElement(() =>
		{
			var element = App.FindElement("IncorrectGrid");
			if (element is null)
				return null;

			var frame = element.GetRect();
			return frame.Width > 0 && frame.Height > 0 ? element : null;
		}, "The Incorrect Grid did not receive a nonempty native frame.");

		var incorrectFrame = incorrectGrid.GetRect();
		Assert.That(incorrectHeader.GetRect().Y, Is.GreaterThanOrEqualTo(incorrectFrame.Y));
		Assert.That(incorrectFirstRow.GetRect().Y, Is.GreaterThanOrEqualTo(incorrectHeader.GetRect().Y + incorrectHeader.GetRect().Height));
		incorrectBottom = incorrectFrame.Y + incorrectFrame.Height;

		Assert.That(double.IsNaN(incorrectBottom), Is.False, "The Incorrect Grid frame was not observed after the tab transition.");
		Assert.That(incorrectBottom, Is.EqualTo(correctBottom).Within(FrameTolerance),
			$"Issue34780 default content did not extend behind the iOS 26 tab bar. Observed grid bottom: {incorrectBottom}, expected reference bottom: {correctBottom}, delta: {Math.Abs(correctBottom - incorrectBottom)}, tolerance: {FrameTolerance}.");
	}
}
#endif

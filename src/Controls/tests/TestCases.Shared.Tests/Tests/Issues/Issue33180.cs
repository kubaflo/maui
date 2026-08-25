#if IOS
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33180 : _IssuesUITest
{
	public override string Issue => "WebView scroll position is not updated after scrolling";

	public Issue33180(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.WebView)]
	public void TouchScrollingUpdatesNativeContentOffset()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("WebViewReady", "WebView ready"),
			Is.True,
			"The inline WebView content should finish loading before offsets are measured.");
		var webViewRect = App.WaitForElement("AffectedWebView").GetRect();
		Assert.That(webViewRect.Width, Is.GreaterThan(0));
		Assert.That(webViewRect.Height, Is.GreaterThan(0));
		Assert.That(GetRequiredText("MeasurementSequence"), Is.EqualTo("Measurement sequence: -1"));

		App.Tap("ShowScrollOffsetButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("MeasurementSequence", "Measurement sequence: 0"),
			Is.True,
			"The initial offset measurement should complete.");
		Assert.That(GetRequiredText("ScrollHostIdentity"), Is.EqualTo("Scroll host: scrollHost"));

		var initialDomOffset = ParseOffset("DomOffset");
		var initialNativeOffset = ParseOffset("NativeOffset");
		Assert.That(initialDomOffset, Is.EqualTo(0).Within(0.5));
		Assert.That(initialNativeOffset, Is.EqualTo(0).Within(0.5));

		var appWindowRect = App.WaitForElement(
			AppiumQuery.ByXPath("//XCUIElementTypeWindow")).GetRect();
		Assert.That(appWindowRect.Height, Is.GreaterThan(0));

		var totalTravel = appWindowRect.Height / 2;
		var firstSegmentTravel = totalTravel / 2;
		var secondSegmentTravel = totalTravel - firstSegmentTravel;
		var startX = webViewRect.X + webViewRect.Width / 2;
		var startY = webViewRect.Y + webViewRect.Height - 20;
		Assert.That(startY, Is.GreaterThan(webViewRect.Y));

		if (App is not AppiumApp appiumApp)
			throw new InvalidOperationException("The iOS UI test requires the repository Appium driver.");

		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var scrollSequence = new ActionSequence(touchDevice, 0);
		scrollSequence.AddAction(touchDevice.CreatePointerMove(
			CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
		scrollSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		scrollSequence.AddAction(touchDevice.CreatePointerMove(
			CoordinateOrigin.Viewport, startX, startY - firstSegmentTravel, TimeSpan.FromMilliseconds(250)));
		scrollSequence.AddAction(touchDevice.CreatePointerMove(
			CoordinateOrigin.Viewport, startX, startY - firstSegmentTravel - secondSegmentTravel, TimeSpan.FromMilliseconds(250)));
		scrollSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		appiumApp.Driver.PerformActions([scrollSequence]);

		App.Tap("ShowScrollOffsetButton");
		Assert.That(
			App.WaitForTextToBePresentInElement("MeasurementSequence", "Measurement sequence: 1"),
			Is.True,
			"The post-scroll offset measurement should complete.");
		Assert.That(GetRequiredText("ScrollHostIdentity"), Is.EqualTo("Scroll host: scrollHost"));

		var domOffset = ParseOffset("DomOffset");
		var nativeOffset = ParseOffset("NativeOffset");
		Assert.That(domOffset, Is.GreaterThan(0),
			"The touch gesture should scroll the identified HTML scroll host.");
		Assert.That(nativeOffset, Is.GreaterThan(0.5),
			"WebView native ContentOffset.Y should be greater than 0 after touch scrolling.");
	}

	double ParseOffset(string automationId)
	{
		var text = GetRequiredText(automationId);
		Assert.That(double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset), Is.True,
			$"Expected {automationId} to contain an invariant numeric offset, but found '{text}'.");
		return offset;
	}

	string GetRequiredText(string automationId)
	{
		var text = App.WaitForElement(automationId).GetText();
		if (text is null)
			throw new InvalidOperationException($"Expected {automationId} to expose text.");

		return text;
	}
}
#endif

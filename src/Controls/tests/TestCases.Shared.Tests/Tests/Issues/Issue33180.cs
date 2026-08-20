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

	public Issue33180(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.WebView)]
	public void NativeContentOffsetUpdatesAfterTouchScrolling()
	{
		App.SetOrientationPortrait();

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test window should be in portrait orientation");

		App.RetryAssert(() =>
			Assert.That(App.FindElement("InitialOffsetLabel").GetText(), Is.EqualTo("Initial ContentOffset.Y: 0"),
				"The inline HTML should load through the iOS WebView handler at the initial zero offset"));

		var webViewRect = App.WaitForElement("WebContent").GetRect();
		Assert.That(webViewRect.Height, Is.EqualTo(400).Within(2), "The WebView should retain its requested 400-point height");

		var startX = webViewRect.X + (webViewRect.Width / 2);
		var startY = webViewRect.Y + (webViewRect.Height * 4 / 5);
		var firstEndY = startY - (int)Math.Round(windowSize.Height * 0.3);
		var endY = firstEndY - (int)Math.Round(windowSize.Height * 0.2);
		Assert.That(firstEndY, Is.GreaterThan(webViewRect.Y), "The first drag segment should remain inside the WebView");
		Assert.That(endY, Is.GreaterThan(0), "The scaled drag should remain inside the test window");

		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, startX, firstEndY, TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, startX, endY, TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		((AppiumApp)App).Driver.PerformActions([dragSequence]);

		App.RetryAssert(() =>
			Assert.That(App.FindElement("ScrollInputLabel").GetText(), Is.EqualTo("Scroll input: received"),
				"The native UIScrollView should report a Scrolled callback while dragging or decelerating"));

		const string reportedPrefix = "Reported ContentOffset.Y: ";
		const string measurementPending = "<measurement pending>";
		string reportedText = measurementPending;

		App.Tap("ShowScrollOffsetButton");

		App.RetryAssert(() =>
		{
			reportedText = App.FindElement("ReportedOffsetLabel").GetText() ?? measurementPending;
			Assert.That(reportedText,
				Does.StartWith(reportedPrefix).And.Not.EndWith("not checked").And.Not.EqualTo(measurementPending),
				"The button should complete a post-scroll native offset measurement");
		});

		var reportedOffset = double.Parse(reportedText[reportedPrefix.Length..], CultureInfo.InvariantCulture);
		var formattedOffset = reportedOffset.ToString("0.###", CultureInfo.InvariantCulture);
		Assert.That(reportedOffset, Is.GreaterThan(0.5),
			$"WKWebView ContentOffset.Y should exceed 0.5 after confirmed touch scrolling; measured {formattedOffset}.");
	}
}
#endif

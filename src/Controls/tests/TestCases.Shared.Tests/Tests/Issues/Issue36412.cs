#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36412 : _IssuesUITest
{
	public Issue36412(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Done keyboard accessory blocks taps on the Entry above the keyboard";

	[Test]
	[Category(UITestCategories.Entry)]
	public void VisibleEntryAboveNumericKeyboardCanReceiveFocus()
	{
		var platformVersionText = ((AppiumIOSApp)App).Driver.Capabilities.GetCapability("platformVersion")?.ToString();
		Assert.That(platformVersionText, Is.Not.Null.And.Not.Empty);
		if (!Version.TryParse(platformVersionText, out var platformVersion) || platformVersion is null)
		{
			Assert.Fail($"Could not parse the iOS platform version '{platformVersionText}'.");
			return;
		}

		if (platformVersion.Major < 15)
		{
			Assert.Ignore("Issue 36412 applies to iOS 15 and later.");
		}

		App.SetOrientationPortrait();
		Assert.That(App.GetOrientation(), Is.EqualTo(OpenQA.Selenium.ScreenOrientation.Portrait));

		var field1 = App.WaitForElement("Field1");
		var field10 = App.WaitForElement("Field10");
		Assert.Multiple(() =>
		{
			Assert.That(field1.GetText(), Is.EqualTo("Field 1"));
			Assert.That(field10.GetText(), Is.EqualTo("Field 10"));
			Assert.That(field1.GetAttribute<string>("enabled"), Is.EqualTo("true").IgnoreCase);
			Assert.That(field10.GetAttribute<string>("enabled"), Is.EqualTo("true").IgnoreCase);
		});

		App.Tap("Field1");
		Assert.That(App.WaitForKeyboardToShow(TimeSpan.FromSeconds(5)), Is.True,
			"The iOS software keyboard did not appear after focusing Field 1.");

		var toolbar = App.WaitForElement(
			AppiumQuery.ByClass("XCUIElementTypeToolbar"),
			"The MAUI Done accessory toolbar did not appear.",
			TimeSpan.FromSeconds(5));

		App.Tap(AppiumQuery.ByName("1"));
		App.RetryAssert(() =>
			Assert.That(App.FindElement("Field1").GetText(), Is.EqualTo("1"),
				"Typing on the numeric keyboard did not update Field 1 after its unobstructed tap."));

		field10 = App.FindElement("Field10");
		var field10Rect = field10.GetRect();
		var toolbarRect = toolbar.GetRect();
		var tapX = field10Rect.X + Math.Min(20, field10Rect.Width / 4);
		var tapY = Math.Max(field10Rect.Top, toolbarRect.Top) + 1;
		Assert.Multiple(() =>
		{
			Assert.That(field10.GetText(), Is.EqualTo("Field 10"));
			Assert.That(field10Rect.Width, Is.GreaterThan(0));
			Assert.That(field10Rect.Height, Is.GreaterThan(0));
			Assert.That(toolbarRect.Width, Is.GreaterThan(0));
			Assert.That(toolbarRect.Height, Is.GreaterThan(0));
			Assert.That(tapX, Is.InRange(field10Rect.Left, field10Rect.Right - 1));
			Assert.That(tapY, Is.InRange(field10Rect.Top, field10Rect.Bottom - 1));
			Assert.That(tapX, Is.InRange(toolbarRect.Left, toolbarRect.Right - 1));
			Assert.That(tapY, Is.InRange(toolbarRect.Top, toolbarRect.Bottom - 1));
		});

		var observedField10Text = "<not observed>";
		App.TapCoordinates(tapX, tapY);
		App.Tap(AppiumQuery.ByName("7"));
		App.RetryAssert(() =>
		{
			observedField10Text = App.FindElement("Field10").GetText() ?? "<null>";
			Assert.That(observedField10Text, Is.EqualTo("7"),
				$"Field 10 did not receive focus after tapping its visible area behind the numeric keyboard accessory; tap=({tapX},{tapY}), fieldRect={field10Rect}, toolbarRect={toolbarRect}, observedText={observedField10Text}, expectedText=7.");
		}, timeout: TimeSpan.FromSeconds(5));
	}
}
#endif

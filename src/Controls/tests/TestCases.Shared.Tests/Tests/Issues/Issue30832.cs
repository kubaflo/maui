#if IOS
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30832 : _IssuesUITest
{
	public Issue30832(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "TapGestureRecognizer does not activate after a long press";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void LongPressReleaseRaisesTapped()
	{
		if (App is not AppiumIOSApp iosApp)
			throw new InvalidOperationException($"Invalid app type for this test: {App}");

		var target = App.WaitForElement("Issue30832Target");
		if (target is null)
			throw new InvalidOperationException("The long-press target was not found.");

		var targetRect = target.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(targetRect.Width, Is.GreaterThan(0), "The attached target must have positive width.");
			Assert.That(targetRect.Height, Is.GreaterThan(0), "The attached target must have positive height.");
		});

		var centerX = targetRect.CenterX();
		var centerY = targetRect.CenterY();
		Assert.Multiple(() =>
		{
			Assert.That(centerX, Is.InRange(targetRect.Left, targetRect.Right));
			Assert.That(centerY, Is.InRange(targetRect.Top, targetRect.Bottom));
		});

		Assert.That(ReadTrailingInteger("Issue30832TapCount"), Is.Zero, "The tap count must start at zero.");
		Assert.That(ReadTrailingInteger("Issue30832InputState"), Is.EqualTo(-1), "No pointer input should have occurred before the trigger.");
		Assert.That(ReadTrailingInteger("Issue30832PointerPressed"), Is.Zero, "The pointer press sentinel must start at zero.");

		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var longPressSequence = new ActionSequence(touchDevice, 0);
		longPressSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, centerX, centerY, TimeSpan.Zero));
		longPressSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		longPressSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(2500)));
		longPressSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		iosApp.Driver.PerformActions([longPressSequence]);

		App.RetryAssert(() =>
			Assert.That(ReadTrailingInteger("Issue30832PointerPressed"), Is.EqualTo(1), "The target must record the pointer press."));
		App.RetryAssert(() =>
			Assert.That(ReadTrailingInteger("Issue30832InputState"), Is.Not.EqualTo(-1), "The pointer state must leave its sentinel value."));
		App.RetryAssert(() =>
			Assert.That(ReadTrailingInteger("Issue30832InputState"), Is.EqualTo(2), "The target must record the pointer release."));

		var tapCount = ReadTrailingInteger("Issue30832TapCount");
		var pointerState = ReadTrailingInteger("Issue30832InputState");
		Assert.That(tapCount, Is.EqualTo(1),
			$"Issue30832 tap count after long-press release: observed {tapCount}, expected 1; pointer state {pointerState}");
	}

	int ReadTrailingInteger(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new InvalidOperationException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new InvalidOperationException($"Element '{automationId}' did not expose text.");

		var separator = text.LastIndexOf(':');
		if (separator < 0 || !int.TryParse(text[(separator + 1)..].Trim(), out var value))
			throw new InvalidOperationException($"Element '{automationId}' did not contain an integer state.");

		return value;
	}
}
#endif

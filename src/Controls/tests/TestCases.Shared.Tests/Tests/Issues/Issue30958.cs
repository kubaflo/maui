#if WINDOWS
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue30958 : _IssuesUITest
{
	const string AffectedButtonId = "AffectedButton";
	const string ResultStatusId = "ResultStatus";

	public Issue30958(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Button remains pressed after touch is released outside its bounds";

	[Test]
	[Category(UITestCategories.Button)]
	public void ButtonReturnsToNormalAfterTouchReleaseOutsideItsBounds()
	{
		var affectedButton = App.WaitForElement(AffectedButtonId);
		if (affectedButton is null)
			throw new InvalidOperationException("The affected button was not found.");

		Assert.That(
			App.FindElement(ResultStatusId).GetText(),
			Is.EqualTo("No pressed-state transition observed"));

		var buttonRect = affectedButton.GetRect();
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var startX = buttonRect.X + (buttonRect.Width / 2);
		var startY = buttonRect.Y + (buttonRect.Height / 2);
		var endX = startX + (int)Math.Round(windowSize.Width * 0.18);
		var endY = startY;

		Assert.Multiple(() =>
		{
			Assert.That(startX, Is.GreaterThanOrEqualTo(buttonRect.X));
			Assert.That(startX, Is.LessThan(buttonRect.X + buttonRect.Width));
			Assert.That(startY, Is.GreaterThanOrEqualTo(buttonRect.Y));
			Assert.That(startY, Is.LessThan(buttonRect.Y + buttonRect.Height));
			Assert.That(endX, Is.GreaterThan(buttonRect.X + buttonRect.Width));
			Assert.That(endX, Is.LessThan(windowSize.Width));
			Assert.That(endY, Is.GreaterThanOrEqualTo(0));
			Assert.That(endY, Is.LessThan(windowSize.Height));
		});

		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(450)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		((AppiumApp)App).Driver.PerformActions([dragSequence]);

		Assert.That(
			App.FindElement(ResultStatusId).GetText(),
			Is.Not.EqualTo("No pressed-state transition observed"),
			"The touch gesture did not trigger an IsPressed transition on the Button.");

		App.RetryAssert(
			() => Assert.That(
				App.FindElement(ResultStatusId).GetText(),
				Is.EqualTo("Released"),
				$"Button remained pressed after touch was released outside its bounds: start=({startX},{startY}); end=({endX},{endY}); window=({windowSize.Width},{windowSize.Height})"),
			timeout: TimeSpan.FromSeconds(5));
	}
}
#endif

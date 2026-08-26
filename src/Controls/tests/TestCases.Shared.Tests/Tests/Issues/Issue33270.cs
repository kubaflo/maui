#if ANDROID
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33270 : _IssuesUITest
{
	const string DragInstruction = "DragInstruction";
	const string DragTarget = "DragTarget";
	const string PanStatus = "PanStatus";
	const string PointerStatus = "PointerStatus";

	public Issue33270(TestDevice device) : base(device)
	{
	}

	public override string Issue => "PointerGestureRecognizer is never fired when the view has a PanGestureRecognizer attached";

	[Test]
	[Category(UITestCategories.Gestures)]
	public void PointerEnteredFiresDuringPanDrag()
	{
		if (App is not AppiumAndroidApp androidApp)
			throw new InvalidOperationException("Issue33270 requires the Android Appium driver.");

		var targetElement = App.WaitForElement(DragTarget);
		if (targetElement is null)
			throw new InvalidOperationException("The drag target was not found.");

		var instructionElement = App.WaitForElement(DragInstruction);
		if (instructionElement is null)
			throw new InvalidOperationException("The drag instruction was not found.");

		Assert.That(GetRequiredText(DragInstruction), Is.EqualTo("DRAG HERE"));
		Assert.That(GetRequiredText(PanStatus), Is.EqualTo("Pan received: NO"));
		Assert.That(GetRequiredText(PointerStatus), Is.EqualTo("Pointer entered: 0"));

		var targetRect = targetElement.GetRect();
		var instructionRect = instructionElement.GetRect();
		Assert.That(targetRect.Width, Is.GreaterThan(0), "The drag target must have a positive width.");
		Assert.That(targetRect.Height, Is.GreaterThan(0), "The drag target must have a positive height.");
		Assert.That(instructionRect.X, Is.GreaterThanOrEqualTo(targetRect.X), "The DRAG HERE label must be inside the target.");
		Assert.That(instructionRect.Y, Is.GreaterThanOrEqualTo(targetRect.Y), "The DRAG HERE label must be inside the target.");
		Assert.That(instructionRect.X + instructionRect.Width, Is.LessThanOrEqualTo(targetRect.X + targetRect.Width), "The DRAG HERE label must be inside the target.");
		Assert.That(instructionRect.Y + instructionRect.Height, Is.LessThanOrEqualTo(targetRect.Y + targetRect.Height), "The DRAG HERE label must be inside the target.");

		var windowSize = androidApp.Driver.Manage().Window.Size;
		var segmentLength = windowSize.Width / 4;
		var middleX = targetRect.CenterX();
		var y = targetRect.CenterY();
		var startX = middleX - segmentLength;
		var endX = middleX + segmentLength;

		AssertPointInsideTarget(startX, y, targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height, "start");
		AssertPointInsideTarget(middleX, y, targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height, "middle");
		AssertPointInsideTarget(endX, y, targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height, "end");

		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, startX, y, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, middleX, y, TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, endX, y, TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		androidApp.Driver.PerformActions([dragSequence]);

		var panTransitioned = App.WaitForTextToBePresentInElement(PanStatus, "Pan received: YES", timeout: TimeSpan.FromSeconds(5));
		Assert.That(panTransitioned, Is.True, "PanUpdated should fire during the held drag.");

		var pointerCount = -1;
		var pointerTransitioned = false;
		App.RetryAssert(() =>
		{
			pointerCount = ParsePointerCount(GetRequiredText(PointerStatus));
			pointerTransitioned = pointerCount != 0;
			Assert.That(pointerTransitioned, Is.True,
				$"PointerEntered should fire during the co-located pan drag; observed pointer count {pointerCount}, expected count greater than 0.");
		}, timeout: TimeSpan.FromSeconds(5));

		Assert.That(pointerTransitioned, Is.True, "The PointerEntered callback transition must be observed after the drag.");
		Assert.That(pointerCount, Is.GreaterThan(0),
			$"PointerEntered should fire during the co-located pan drag; observed pointer count {pointerCount}, expected count greater than 0.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		if (element is null)
			throw new InvalidOperationException($"Element '{automationId}' was not found.");

		var text = element.GetText();
		if (text is null)
			throw new InvalidOperationException($"Element '{automationId}' did not expose text.");

		return text;
	}

	static int ParsePointerCount(string text)
	{
		const string Prefix = "Pointer entered: ";
		Assert.That(text, Does.StartWith(Prefix), "The pointer status text has an unexpected format.");
		return int.Parse(text[Prefix.Length..]);
	}

	static void AssertPointInsideTarget(int x, int y, int targetX, int targetY, int targetWidth, int targetHeight, string pointName)
	{
		Assert.That(x, Is.GreaterThanOrEqualTo(targetX), $"The {pointName} drag point must be inside the target.");
		Assert.That(x, Is.LessThanOrEqualTo(targetX + targetWidth), $"The {pointName} drag point must be inside the target.");
		Assert.That(y, Is.GreaterThanOrEqualTo(targetY), $"The {pointName} drag point must be inside the target.");
		Assert.That(y, Is.LessThanOrEqualTo(targetY + targetHeight), $"The {pointName} drag point must be inside the target.");
	}
}
#endif

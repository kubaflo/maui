#if ANDROID
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue35770 : _IssuesUITest
{
	public Issue35770(TestDevice device) : base(device) { }

	public override string Issue => "Nested CollectionView does not scroll on Android";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void InnerCollectionViewScrollsAfterUpwardDrag()
	{
		App.SetOrientationPortrait();

		var app = App as AppiumApp
			?? throw new InvalidOperationException("The Appium driver is required for the nested touch gesture.");
		var windowSize = app.Driver.Manage().Window.Size;
		Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test requires portrait orientation.");

		var firstRowTitle = App.WaitForElement("Outer row 1");
		var firstItem = App.WaitForElement("Row 1 Item 1");
		Assert.That(firstItem.GetText(), Is.EqualTo("Row 1 Item 1"));
		Assert.That(firstItem.GetRect().Y, Is.GreaterThan(firstRowTitle.GetRect().Bottom),
			"Row 1 Item 1 should be inside the nested collection below Outer row 1.");

		const string readyState = "0|-1|0|5|220|352|ready";
		Assert.That(
			App.WaitForTextToBePresentInElement("NestedScrollResult", readyState, TimeSpan.FromSeconds(10)),
			Is.True,
			"The first nested collection did not reach its expected attached layout state.");
		var setupValues = App.FindElement("NestedScrollResult").GetText()!.Split('|');
		Assert.That(int.Parse(setupValues[3], CultureInfo.InvariantCulture), Is.EqualTo(5));
		var viewportHeight = double.Parse(setupValues[4], CultureInfo.InvariantCulture);
		var contentExtent = double.Parse(setupValues[5], CultureInfo.InvariantCulture);
		Assert.That(contentExtent, Is.GreaterThan(viewportHeight),
			"The five nested items should extend beyond the 220-unit viewport.");

		DragUpTwiceWhileHeld(app, firstItem.GetRect());

		App.Tap("CheckNestedScroll");
		Assert.That(
			App.WaitForTextToBePresentInElement("NestedScrollResult", "|checked", TimeSpan.FromSeconds(5)),
			Is.True,
			"The nested-scroll check action did not complete.");

		var values = App.FindElement("NestedScrollResult").GetText()!.Split('|');
		var callbackCount = int.Parse(values[0], CultureInfo.InvariantCulture);
		var verticalOffset = double.Parse(values[1], CultureInfo.InvariantCulture);
		var verticalDelta = double.Parse(values[2], CultureInfo.InvariantCulture);
		var failure = FormattableString.Invariant(
			$"Inner CollectionView did not scroll after upward drag; callbackCount={callbackCount}, verticalOffset={verticalOffset}, verticalDelta={verticalDelta}; expected callbackCount>0 and verticalOffset>0 or verticalDelta!=0.");

		Assert.That(callbackCount, Is.GreaterThan(0), failure);
		Assert.That(verticalOffset != -1 || verticalDelta != 0, Is.True, failure);
		Assert.That(verticalOffset > 0 || verticalDelta != 0, Is.True, failure);
	}

	static void DragUpTwiceWhileHeld(AppiumApp app, System.Drawing.Rectangle itemBounds)
	{
		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		var x = itemBounds.X + (itemBounds.Width / 2);
		var startY = itemBounds.Y + (itemBounds.Height / 2);
		var segment = Math.Max(1, (int)Math.Round(itemBounds.Height * 0.15));

		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY - segment, TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, x, startY - (segment * 2), TimeSpan.FromMilliseconds(250)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		app.Driver.PerformActions([dragSequence]);
	}
}
#endif

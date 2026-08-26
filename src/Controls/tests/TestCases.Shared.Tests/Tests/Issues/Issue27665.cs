#if ANDROID
using System.Globalization;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27665 : _IssuesUITest
{
	public override string Issue => "Flickering when hiding or showing elements in a ScrollView Scrolled event";

	public Issue27665(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.ScrollView)]
	public void HeaderRemainsHiddenDuringContinuousUpwardDrag()
	{
		var headerEntry = App.WaitForElement("Issue27665Entry");
		var headerImage = App.WaitForElement("Issue27665Image");
		var scrollViewRect = App.WaitForElement("Issue27665ScrollView").GetRect();
		var dragRow = App.WaitForElement("Issue27665Row8");
		var dragRowRect = dragRow.GetRect();
		App.WaitForElement("Issue27665Counts");

		Assert.That(headerEntry.IsDisplayed(), Is.True, "The Entry should initially be visible.");
		Assert.That(headerImage.IsDisplayed(), Is.True, "The Image should initially be visible.");
		Assert.That(dragRow.IsDisplayed(), Is.True, "The drag must begin over a visible list row.");

		var initialCounts = ReadCounts();
		Assert.That(initialCounts.ScrollEvents, Is.Zero, "No Scrolled callback should occur before the gesture.");
		Assert.That(initialCounts.VisibilityTransitions, Is.Zero, "Header visibility should not change before the gesture.");

		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var startX = dragRowRect.CenterX();
		var startY = dragRowRect.CenterY();
		var travel = Math.Min(windowSize.Height * 0.35f, startY - scrollViewRect.Y - 20);
		Assert.That(travel, Is.GreaterThan(windowSize.Height * 0.15f), "The visible list geometry should allow a drag well beyond Android touch slop.");

		var postTriggerScrollEvents = -1;
		var postTriggerVisibilityTransitions = -1;
		DragWhileHolding((AppiumApp)App, startX, startY, startY - travel);

		App.RetryAssert(() =>
		{
			postTriggerScrollEvents = ReadCounts().ScrollEvents;
			Assert.That(postTriggerScrollEvents, Is.GreaterThan(0), "The continuous drag should raise a post-trigger Scrolled callback.");
		});

		App.RetryAssert(() =>
		{
			postTriggerVisibilityTransitions = ReadCounts().VisibilityTransitions;
			Assert.That(postTriggerVisibilityTransitions, Is.GreaterThanOrEqualTo(1), "The header should transition from visible to hidden.");
		});

		var finalCounts = ReadCounts();
		Assert.That(finalCounts.VisibilityTransitions, Is.EqualTo(1),
			$"Header visibility transitioned repeatedly during one continuous upward drag. Observed {finalCounts.VisibilityTransitions.ToString(CultureInfo.InvariantCulture)} transitions; expected 1.");

		App.RetryAssert(() =>
		{
			Assert.That(App.FindElements("Issue27665Entry"), Is.Empty, "The Entry should remain natively hidden after scrolling down.");
			Assert.That(App.FindElements("Issue27665Image"), Is.Empty, "The Image should remain natively hidden after scrolling down.");
		});
	}

	static void DragWhileHolding(AppiumApp app, float x, float startY, float endY)
	{
		var touchDevice = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(PointerKind.Touch);
		var dragSequence = new ActionSequence(touchDevice, 0);
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)x, (int)startY, TimeSpan.Zero));
		dragSequence.AddAction(touchDevice.CreatePointerDown(PointerButton.TouchContact));
		dragSequence.AddAction(touchDevice.CreatePointerMove(CoordinateOrigin.Viewport, (int)x, (int)endY, TimeSpan.FromMilliseconds(1500)));
		dragSequence.AddAction(touchDevice.CreatePause(TimeSpan.FromMilliseconds(500)));
		dragSequence.AddAction(touchDevice.CreatePointerUp(PointerButton.TouchContact));
		app.Driver.PerformActions([dragSequence]);
	}

	(int ScrollEvents, int VisibilityTransitions) ReadCounts()
	{
		var text = App.WaitForElement("Issue27665Counts").GetText();
		if (text is null)
		{
			Assert.Fail("The diagnostic count text was null.");
			return (-1, -1);
		}

		var values = text.Split(';');
		Assert.That(values, Has.Length.EqualTo(2), $"Unexpected diagnostic count text: {text}");

		var scrollEvents = int.Parse(values[0].Split('=')[1], CultureInfo.InvariantCulture);
		var visibilityTransitions = int.Parse(values[1].Split('=')[1], CultureInfo.InvariantCulture);
		return (scrollEvents, visibilityTransitions);
	}
}
#endif

#if ANDROID
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue36736 : _IssuesUITest
{
	public override string Issue => "Android SwipeItem text and icon are vertically misaligned when SwipeView wraps CollectionView";

	public Issue36736(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void SwipeItemTextAndIconShareTheSwipeViewVerticalCenter()
	{
		App.WaitForElement("5 recode");
		Assert.That(App.WaitForElement("Issue36736ItemsState").GetText(), Is.EqualTo("Items=5"));

		App.Tap("Issue36736TwentyItems");
		App.WaitForElement("10 recode");
		Assert.That(App.WaitForElement("Issue36736ItemsState").GetText(), Is.EqualTo("Items=20"));
		Assert.That(App.WaitForElement("Issue36736MeasurementState").GetText(), Is.EqualTo("Callbacks=0;Measured=0"));
		Assert.That(App.WaitForElement("Issue36736InvocationState").GetText(), Is.EqualTo("Invoked=0"));

		var collectionRect = App.WaitForElement("Issue36736Collection").GetRect();
		var tenthRowRect = App.WaitForElement("10 recode").GetRect();
		var startX = collectionRect.X + collectionRect.Width * 0.15f;
		var endX = collectionRect.X + collectionRect.Width * 0.9f;
		App.DragCoordinates(startX, tenthRowRect.CenterY(), endX, tenthRowRect.CenterY());

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36736MeasurementState", "Measured=1", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"SwipeChanging must capture the rendered native SwipeItem geometry after a nonzero swipe offset.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue36736InvocationState", "Invoked=1", timeout: TimeSpan.FromSeconds(5)),
			Is.True,
			"The real Execute-mode drag must invoke the Back SwipeItem.");

		var measurement = App.FindElement("Issue36736MeasurementState").GetText() ?? string.Empty;
		Assert.That(measurement, Does.Contain("Text=Back;Drawable=1;Parent=1;"));

		var buttonBounds = ReadBounds(measurement, "Button");
		var actionBounds = ReadBounds(measurement, "Action");
		var swipeBounds = ReadBounds(measurement, "Swipe");
		Assert.That(buttonBounds.Width, Is.GreaterThan(0));
		Assert.That(buttonBounds.Height, Is.GreaterThan(0));
		Assert.That(actionBounds.Width, Is.GreaterThan(0));
		Assert.That(actionBounds.Height, Is.GreaterThan(0));
		Assert.That(swipeBounds.Width, Is.GreaterThan(0));
		Assert.That(swipeBounds.Height, Is.GreaterThan(0));
		Assert.That(buttonBounds.X, Is.GreaterThanOrEqualTo(actionBounds.X));
		Assert.That(buttonBounds.Y, Is.GreaterThanOrEqualTo(actionBounds.Y));
		Assert.That(buttonBounds.X + buttonBounds.Width, Is.LessThanOrEqualTo(actionBounds.X + actionBounds.Width));
		Assert.That(buttonBounds.Y + buttonBounds.Height, Is.LessThanOrEqualTo(actionBounds.Y + actionBounds.Height));

		var textCenterY = ReadValue(measurement, "textCenterY");
		var iconCenterY = ReadValue(measurement, "iconCenterY");
		var swipeCenterY = ReadValue(measurement, "swipeCenterY");
		var textIconDelta = Math.Abs(textCenterY - iconCenterY);
		var textSwipeDelta = Math.Abs(textCenterY - swipeCenterY);
		var iconSwipeDelta = Math.Abs(iconCenterY - swipeCenterY);

		Assert.That(
			textIconDelta <= 1 && textSwipeDelta <= 1 && iconSwipeDelta <= 1,
			Is.True,
			$"SwipeItem native centers must align within 1.0 px; textCenterY={textCenterY:F1}, " +
			$"iconCenterY={iconCenterY:F1}, swipeCenterY={swipeCenterY:F1}, " +
			$"textIconDelta={textIconDelta:F1}, textSwipeDelta={textSwipeDelta:F1}, iconSwipeDelta={iconSwipeDelta:F1}.");
	}

	static (double X, double Y, double Width, double Height) ReadBounds(string measurement, string key)
	{
		var value = ReadToken(measurement, key);
		var parts = value.Split(',');
		Assert.That(parts, Has.Length.EqualTo(4), $"{key} must contain native x,y,width,height bounds.");
		return (
			double.Parse(parts[0], CultureInfo.InvariantCulture),
			double.Parse(parts[1], CultureInfo.InvariantCulture),
			double.Parse(parts[2], CultureInfo.InvariantCulture),
			double.Parse(parts[3], CultureInfo.InvariantCulture));
	}

	static double ReadValue(string measurement, string key) =>
		double.Parse(ReadToken(measurement, key), CultureInfo.InvariantCulture);

	static string ReadToken(string measurement, string key)
	{
		var prefix = key + "=";
		foreach (var token in measurement.Split(';'))
		{
			if (token.StartsWith(prefix, StringComparison.Ordinal))
				return token[prefix.Length..];
		}

		Assert.Fail($"Native measurement did not contain {key}: {measurement}");
		return string.Empty;
	}
}
#endif

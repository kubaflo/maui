#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	public Issue32587(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DirectContentViewHasValidBoundsWhenTapped()
	{
		var item = App.WaitForElement("BoundsItemText").GetRect();
		Assert.That(item.Width, Is.GreaterThan(0), "The CollectionView item label must be realized with a positive width before it is tapped.");
		Assert.That(item.Height, Is.GreaterThan(0), "The CollectionView item label must be realized with a positive height before it is tapped.");

		var initialStatus = App.WaitForElement("GestureStatus").GetText();
		Assert.That(initialStatus, Is.EqualTo("NOT_TAPPED"), "The tap callback must not have run before the pointer action.");

		App.Tap("BoundsItemText");
		App.RetryAssert(
			() => Assert.That(App.FindElement("ResultStatus").GetText(), Is.EqualTo("TAP_COMPLETED"),
				"The tap callback must complete after the pointer action."),
			timeout: TimeSpan.FromSeconds(10));

		var callbackStatus = App.FindElement("GestureStatus").GetText();
		Assert.That(callbackStatus, Is.Not.Null, "The tap callback must publish the captured dimensions.");
		Assert.That(callbackStatus, Is.Not.EqualTo("NOT_TAPPED"), "The tap callback status must leave its sentinel after the pointer action.");

		const string widthPrefix = "TAPPED: Width=";
		const string heightSeparator = ", Height=";
		Assert.That(callbackStatus, Does.StartWith(widthPrefix), "The callback status must contain the dimensions captured by the tapped ContentView.");

		var dimensions = callbackStatus![widthPrefix.Length..].Split(heightSeparator);
		Assert.That(dimensions, Has.Length.EqualTo(2), "The callback status must contain both captured dimensions.");
		Assert.That(double.TryParse(dimensions[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width), Is.True,
			$"Could not parse the callback width from '{callbackStatus}'.");
		Assert.That(double.TryParse(dimensions[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height), Is.True,
			$"Could not parse the callback height from '{callbackStatus}'.");

		Assert.That(double.IsFinite(width) && width > 0 && double.IsFinite(height) && height > 0, Is.True,
			$"Direct ContentView bounds after tap must be positive; observed Width={width:R}, Height={height:R}.");
	}
}
#endif

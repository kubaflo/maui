#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32587 : _IssuesUITest
{
	const string BoundsPrefix = "TAPPED BOUNDS: Width=";
	const string HeightSeparator = "; Height=";

	public Issue32587(TestDevice device) : base(device)
	{
	}

	public override string Issue => "ContentView inside CollectionView reports invalid bounds during gesture events";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappedContentViewHasPositiveBoundsAfterLoaded()
	{
		Assert.That(
			App.WaitForTextToBePresentInElement("RenderStatusLabel", "READY: direct item loaded"),
			Is.True,
			"The direct ContentView item should reach Loaded before it is tapped.");

		var itemRect = App.WaitForElement("DirectGestureItem").GetRect();
		Assert.That(itemRect.X, Is.GreaterThanOrEqualTo(0), "The intended direct template item should have an on-screen X coordinate.");
		Assert.That(itemRect.Y, Is.GreaterThanOrEqualTo(0), "The intended direct template item should have an on-screen Y coordinate.");
		Assert.That(itemRect.Width, Is.GreaterThan(0), "The intended direct template item should have positive native width.");
		Assert.That(itemRect.Height, Is.GreaterThan(0), "The intended direct template item should have positive native height.");

		var initialBoundsText = App.WaitForElement("TappedBoundsLabel").GetText();
		if (initialBoundsText is null)
			throw new AssertionException("The tapped bounds label should expose its initial text.");
		Assert.That(initialBoundsText, Is.EqualTo("TAPPED BOUNDS: unavailable"));

		App.Tap("DirectGestureItem");

		Assert.That(
			App.WaitForTextToBePresentInElement("InteractionStatusLabel", "TAP RECEIVED:"),
			Is.True,
			"The real pointer tap should reach the ContentView gesture callback.");
		Assert.That(
			App.WaitForTextToBePresentInElement("TappedBoundsLabel", BoundsPrefix),
			Is.True,
			"The gesture callback should replace the unavailable bounds sentinel.");

		var boundsText = App.WaitForElement("TappedBoundsLabel").GetText();
		if (boundsText is null)
			throw new AssertionException("The tapped bounds label should expose the callback-captured dimensions.");
		Assert.That(boundsText, Does.StartWith(BoundsPrefix));

		var dimensions = boundsText[BoundsPrefix.Length..].Split(HeightSeparator, StringSplitOptions.None);
		Assert.That(dimensions, Has.Length.EqualTo(2), $"Unexpected tapped bounds text: {boundsText}");

		var width = double.Parse(dimensions[0], CultureInfo.InvariantCulture);
		var height = double.Parse(dimensions[1], CultureInfo.InvariantCulture);
		var failureMessage = $"Issue32587 tapped ContentView bounds must be positive after Loaded; observed Width={width}, Height={height}.";

		Assert.That(width, Is.GreaterThan(0), failureMessage);
		Assert.That(height, Is.GreaterThan(0), failureMessage);
	}
}
#endif

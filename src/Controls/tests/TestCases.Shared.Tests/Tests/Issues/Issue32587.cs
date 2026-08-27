#if WINDOWS
using System.Globalization;
using System.Text.RegularExpressions;
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
	public void DirectTemplateContentViewHasValidBoundsWhenTapped()
	{
		var renderedState = App.WaitForElement("RenderedStateLabel");
		Assert.That(renderedState, Is.Not.Null);
		Assert.That(renderedState.GetText(), Is.EqualTo("ContentView is loaded and visible"));

		var gestureItem = App.WaitForElement("GestureItem");
		Assert.That(gestureItem, Is.Not.Null);
		App.WaitForElement("Tap the direct ContentView item");
		var itemRect = gestureItem.GetRect();
		var collectionRect = App.WaitForElement("ItemsCollection").GetRect();
		Assert.That(itemRect.Width, Is.GreaterThan(0), "The direct DataTemplate-root ContentView must be rendered before it is tapped.");
		Assert.That(itemRect.Height, Is.GreaterThan(0), "The direct DataTemplate-root ContentView must be rendered before it is tapped.");
		Assert.That(itemRect.X + (itemRect.Width / 2), Is.InRange(collectionRect.X, collectionRect.X + collectionRect.Width),
			"The intended item must be located inside the CollectionView.");
		Assert.That(itemRect.Y + (itemRect.Height / 2), Is.InRange(collectionRect.Y, collectionRect.Y + collectionRect.Height),
			"The intended item must be located inside the CollectionView.");

		var initialBounds = App.WaitForElement("TappedBoundsLabel");
		Assert.That(initialBounds, Is.Not.Null);
		Assert.That(initialBounds.GetText(), Is.EqualTo("Tapped bounds: not measured"));
		Assert.That(App.WaitForElement("GestureStateLabel").GetText(), Is.EqualTo("Gesture received: 0"));

		App.Tap("GestureItem");

		App.WaitForElement("Gesture received: 1");
		var gestureState = App.WaitForElement("GestureStateLabel");
		Assert.That(gestureState, Is.Not.Null);
		Assert.That(gestureState.GetText(), Is.EqualTo("Gesture received: 1"));

		var boundsElement = App.WaitForElement("TappedBoundsLabel");
		Assert.That(boundsElement, Is.Not.Null);
		var boundsText = boundsElement.GetText();
		if (boundsText is null)
		{
			Assert.Fail("Gesture-time bounds text must be available after the tap callback.");
			return;
		}

		var match = Regex.Match(boundsText, @"^Tapped bounds: Width=(?<width>-?\d+(?:\.\d+)?), Height=(?<height>-?\d+(?:\.\d+)?)$");
		Assert.That(match.Success, Is.True, $"Gesture callback must report both dimensions, but reported '{boundsText}'.");

		var width = double.Parse(match.Groups["width"].Value, CultureInfo.InvariantCulture);
		var height = double.Parse(match.Groups["height"].Value, CultureInfo.InvariantCulture);
		Assert.That(width > 0 && height > 0, Is.True,
			$"Gesture-time ContentView bounds must be positive after rendering; observed Width={width}, Height={height}; expected both dimensions >0.");
	}
}
#endif

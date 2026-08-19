#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue8636 : _IssuesUITest
{
	const double ContainmentTolerance = 0.5;

	public Issue8636(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "CollectionView size not updating";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void NestedCollectionGrowthRemeasuresOuterItem()
	{
		App.SetOrientationPortrait();

		var pageRect = App.WaitForElement("Issue8636Page").GetRect();
		Assert.That(pageRect.Width, Is.GreaterThan(0));
		Assert.That(pageRect.Height, Is.GreaterThan(pageRect.Width), "The recorded scene should be running in portrait.");

		var rowOneRect = App.WaitForElement("Row 1").GetRect();
		var initialInnerRect = App.WaitForElement("InnerCollectionView").GetRect();
		var outerRect = App.WaitForElement("OuterCollectionView").GetRect();
		Assert.That(rowOneRect.Width, Is.GreaterThan(0));
		Assert.That(rowOneRect.Height, Is.GreaterThan(0));
		Assert.That(initialInnerRect.Width, Is.GreaterThan(0));
		Assert.That(initialInnerRect.Height, Is.GreaterThan(0));
		Assert.That(outerRect.Width, Is.GreaterThan(0));
		Assert.That(outerRect.Height, Is.GreaterThan(0));
		Assert.That(App.FindElements("Row 2").Count, Is.Zero);

		App.WaitForTextToBePresentInElement("StateLabel", "loaded=True");
		var initialState = App.FindElement("StateLabel").GetText();
		Assert.That(initialState, Does.Contain("s=1"));
		Assert.That(initialState, Does.Contain("m=-1"));
		var initialResult = App.FindElement("ResultLabel").GetText();
		Assert.That(initialResult, Does.Contain("checked=false"));

		App.Tap("GrowButton");

		App.WaitForTextToBePresentInElement("StateLabel", "m=1");
		var grownState = App.FindElement("StateLabel").GetText();
		Assert.That(grownState, Does.Contain("s=2"));
		Assert.That(grownState, Does.Contain("m=1"));

		App.Tap("CheckButton");
		App.WaitForTextToBePresentInElement("ResultLabel", "checked=true");

		var result = App.FindElement("ResultLabel").GetText();
		Assert.That(result, Does.Contain("s=2"));
		Assert.That(result, Does.Contain("m=1"));
		var rowTwoRect = App.WaitForElement("Row 2").GetRect();
		Assert.That(rowTwoRect.Width, Is.GreaterThan(0));
		Assert.That(rowTwoRect.Height, Is.GreaterThan(0));
		var innerRect = App.WaitForElement("InnerCollectionView").GetRect();
		var currentRowOneRect = App.WaitForElement("Row 1").GetRect();
		var innerViewportBottom = innerRect.Y + innerRect.Height;
		var rowOneBottom = currentRowOneRect.Y + currentRowOneRect.Height;
		var rowTwoBottom = rowTwoRect.Y + rowTwoRect.Height;

		Assert.That(innerViewportBottom, Is.GreaterThan(innerRect.Y));
		Assert.That(rowTwoRect.Y, Is.GreaterThanOrEqualTo(rowOneBottom - ContainmentTolerance), "Row 2 should be positioned below Row 1.");

		Assert.That(
			result,
			Does.Contain("remeasured=True"),
			$"Issue8636 nested content is clipped after Row 2 was added: Row 2 bottom={rowTwoBottom:F3}, inner viewport bottom={innerViewportBottom:F3}.");
	}
}
#endif

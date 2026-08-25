#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27332 : _IssuesUITest
{
	public Issue27332(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView footer is displayed at the bottom of the page";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void EmptyCollectionFooterShouldRenderImmediatelyBelowHeader()
	{
		var collectionBefore = App.WaitForElement("IssueCollectionView");
		var headerBefore = App.WaitForElement("HeaderLabel");
		var footerBefore = App.WaitForElement("FooterLabel");

		Assert.That(headerBefore.GetText(), Is.EqualTo("Header"));
		Assert.That(footerBefore.GetText(), Is.EqualTo("Footer"));

		var collectionBeforeRect = collectionBefore.GetRect();
		var headerBeforeRect = headerBefore.GetRect();
		var footerBeforeRect = footerBefore.GetRect();

		Assert.That(collectionBeforeRect.Width, Is.GreaterThan(0));
		Assert.That(collectionBeforeRect.Height, Is.GreaterThan(0));
		Assert.That(headerBeforeRect.Width, Is.GreaterThan(0));
		Assert.That(headerBeforeRect.Height, Is.GreaterThan(0));
		Assert.That(footerBeforeRect.Width, Is.GreaterThan(0));
		Assert.That(footerBeforeRect.Height, Is.GreaterThan(0));
		Assert.That(headerBeforeRect.Y, Is.EqualTo(collectionBeforeRect.Y).Within(2),
			"The CollectionView header should align with the top of the CollectionView.");
		Assert.That(headerBeforeRect.Y, Is.GreaterThanOrEqualTo(collectionBeforeRect.Y));
		Assert.That(headerBeforeRect.Y + headerBeforeRect.Height, Is.LessThanOrEqualTo(collectionBeforeRect.Y + collectionBeforeRect.Height));
		Assert.That(footerBeforeRect.Y, Is.GreaterThanOrEqualTo(collectionBeforeRect.Y));
		Assert.That(footerBeforeRect.Y + footerBeforeRect.Height, Is.LessThanOrEqualTo(collectionBeforeRect.Y + collectionBeforeRect.Height));

		Assert.That(App.WaitForElement("ResetStatusLabel").GetText(), Is.EqualTo("Reset:-1"));
		App.Tap("ClearItemsButton");
		App.WaitForElement("Reset:1");
		Assert.That(App.FindElement("ResetStatusLabel").GetText(), Is.EqualTo("Reset:1"),
			"Clearing the empty items source should raise one Reset notification.");

		var collectionAfter = App.WaitForElement("IssueCollectionView");
		var headerAfter = App.WaitForElement("HeaderLabel");
		var footerAfter = App.WaitForElement("FooterLabel");
		var collectionAfterRect = collectionAfter.GetRect();
		var headerAfterRect = headerAfter.GetRect();
		var footerAfterRect = footerAfter.GetRect();

		Assert.That(headerAfter.GetText(), Is.EqualTo("Header"));
		Assert.That(footerAfter.GetText(), Is.EqualTo("Footer"));
		Assert.That(collectionAfterRect.Width, Is.GreaterThan(0));
		Assert.That(collectionAfterRect.Height, Is.GreaterThan(0));
		Assert.That(headerAfterRect.Width, Is.GreaterThan(0));
		Assert.That(headerAfterRect.Height, Is.GreaterThan(0));
		Assert.That(footerAfterRect.Width, Is.GreaterThan(0));
		Assert.That(footerAfterRect.Height, Is.GreaterThan(0));
		Assert.That(headerAfterRect.Y, Is.EqualTo(collectionAfterRect.Y).Within(2),
			"The CollectionView header should remain aligned with the top of the CollectionView.");
		Assert.That(headerAfterRect.Y, Is.GreaterThanOrEqualTo(collectionAfterRect.Y));
		Assert.That(headerAfterRect.Y + headerAfterRect.Height, Is.LessThanOrEqualTo(collectionAfterRect.Y + collectionAfterRect.Height));
		Assert.That(footerAfterRect.Y, Is.GreaterThanOrEqualTo(collectionAfterRect.Y));
		Assert.That(footerAfterRect.Y + footerAfterRect.Height, Is.LessThanOrEqualTo(collectionAfterRect.Y + collectionAfterRect.Height));

		var expectedFooterY = collectionAfterRect.Y + headerAfterRect.Height;
		var footerGap = footerAfterRect.Y - expectedFooterY;
		Assert.That(footerAfterRect.Y, Is.EqualTo(expectedFooterY).Within(2),
			$"Issue27332 footer should render immediately below the empty CollectionView header. " +
			$"Observed Y: {footerAfterRect.Y:0.##}, expected Y: {expectedFooterY:0.##}, gap: {footerGap:0.##}.");
	}
}
#endif

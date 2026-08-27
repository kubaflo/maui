#if WINDOWS
using System;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Incremental loading on scroll jumps back to the top";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalAppendPreservesViewport()
	{
		var collectionElement = App.WaitForElement("Issue29120CollectionView");
		var initialCount = App.WaitForElement("Issue29120ItemCount").GetText();
		var initialGeneration = App.WaitForElement("Issue29120LoadGeneration").GetText();
		Assert.That(initialCount, Is.Not.Null);
		Assert.That(initialGeneration, Is.Not.Null);
		Assert.That(initialCount, Is.EqualTo("10"));
		Assert.That(initialGeneration, Is.EqualTo("-1"));

		var firstBearElement = App.WaitForElement("American Black Bear");
		var secondBearElement = App.WaitForElement("Bear 2");
		var firstBearText = firstBearElement.GetText();
		var secondBearText = secondBearElement.GetText();
		Assert.That(firstBearText, Is.Not.Null);
		Assert.That(secondBearText, Is.Not.Null);
		Assert.That(firstBearText, Is.EqualTo("American Black Bear"));
		Assert.That(secondBearText, Is.EqualTo("Bear 2"));

		var collectionRect = collectionElement.GetRect();
		var initialFirstRect = firstBearElement.GetRect();
		var initialSecondRect = secondBearElement.GetRect();
		Assert.That(collectionRect.Width, Is.GreaterThan(0));
		Assert.That(collectionRect.Height, Is.GreaterThan(0));
		Assert.That(initialFirstRect.Width, Is.GreaterThan(0));
		Assert.That(initialFirstRect.Height, Is.GreaterThan(0));
		Assert.That(initialSecondRect.Width, Is.GreaterThan(0));
		Assert.That(initialSecondRect.Height, Is.GreaterThan(0));
		Assert.That(initialFirstRect.Top, Is.GreaterThanOrEqualTo(collectionRect.Top));
		Assert.That(initialFirstRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom));
		Assert.That(initialSecondRect.Top, Is.GreaterThan(initialFirstRect.Top));
		Assert.That(initialSecondRect.Bottom, Is.LessThanOrEqualTo(collectionRect.Bottom));

		var windowRect = App.WaitForElement(AppiumQuery.ByXPath("/*")).GetRect();
		float dragX = windowRect.X + (windowRect.Width * 0.56f);
		float dragStartY = windowRect.Y + (windowRect.Height * 0.87f);
		float dragEndY = windowRect.Y + (windowRect.Height * 0.18f);
		Assert.That(dragX, Is.GreaterThan(collectionRect.Left));
		Assert.That(dragX, Is.LessThan(collectionRect.Right));
		Assert.That(dragStartY, Is.GreaterThan(collectionRect.Top));
		Assert.That(dragStartY, Is.LessThan(collectionRect.Bottom));
		Assert.That(dragEndY, Is.GreaterThan(collectionRect.Top));
		Assert.That(dragEndY, Is.LessThan(collectionRect.Bottom));
		Assert.That(dragStartY - dragEndY, Is.GreaterThan(20));

		App.DragCoordinates(dragX, dragStartY, dragX, dragEndY);

		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29120LoadGeneration", "1", TimeSpan.FromSeconds(15)),
			Is.True,
			"The incremental-load command did not advance from generation -1 to 1.");
		Assert.That(
			App.WaitForTextToBePresentInElement("Issue29120ItemCount", "20", TimeSpan.FromSeconds(15)),
			Is.True,
			"The incremental-load command did not append the next ten items.");

		var postGeneration = App.WaitForElement("Issue29120LoadGeneration").GetText();
		var postCount = App.WaitForElement("Issue29120ItemCount").GetText();
		Assert.That(postGeneration, Is.Not.Null);
		Assert.That(postCount, Is.Not.Null);
		Assert.That(postGeneration, Does.StartWith("1:"));
		Assert.That(postCount, Is.EqualTo("20"));
		Assert.That(
			postGeneration,
			Is.EqualTo("1: PRESERVED"),
			$"Issue29120 viewport reset after incremental append: expected PRESERVED after the post-load Scrolled callback, but was {postGeneration}; count={postCount}.");
	}
}
#endif

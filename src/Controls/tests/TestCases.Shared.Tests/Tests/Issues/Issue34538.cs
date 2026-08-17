#if IOS && !MACCATALYST
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34538 : _IssuesUITest
{
	const string ExpectedResult = "PASS: Recycled delayed-stream images remained continuously visible";

	public Issue34538(TestDevice device) : base(device)
	{
	}

	public override string Issue => "CollectionView items flicker when using async StreamImageSource with delayed stream";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RecycledDelayedStreamImagesRemainVisibleWhileScrollingUpward()
	{
		App.SetOrientationPortrait();
		var collection = App.WaitForElement("ImageCollection");
		var collectionBounds = collection.GetRect();

		Assert.That(collectionBounds.Width, Is.LessThan(collectionBounds.Height));
		Assert.That(collectionBounds.Height, Is.LessThan(30 * 180));
		Assert.That(App.WaitForElement("ResultLabel").GetText(), Does.StartWith("PENDING:"));

		var centerX = collectionBounds.CenterX();
		var top = collectionBounds.Y + 30;
		var bottom = collectionBounds.Bottom - 30;

		for (var swipe = 0; swipe < 4; swipe++)
			App.DragCoordinates(centerX, bottom, centerX, top);

		for (var swipe = 0; swipe < 5; swipe++)
			App.DragCoordinates(centerX, top, centerX, bottom);

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"TitleLabel",
				"OBSERVED:",
				timeout: TimeSpan.FromSeconds(20)),
			Is.True,
			"The post-scroll handler loading observation must run");

		var observation = App.FindElement("TitleLabel").GetText();
		Assert.That(observation, Does.Contain("initial=True; reuse=True; sourceCorrelated=True"));

		var actualResult = App.FindElement("ResultLabel").GetText();
		Assert.That(
			actualResult,
			Is.EqualTo(ExpectedResult),
			"A recycled delayed-stream image must remain visibly rendered throughout upward scrolling");
	}
}
#endif

#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue19064 : _IssuesUITest
{
	const string BorderName = "Issue19064Border0";
	const string CollectionViewId = "Issue19064CollectionView";
	const string ProgressId = "Issue19064Progress";
	const string ResultId = "Issue19064Result";
	const string FailureSignature = "First item border should remain 100x50 after horizontal swipe round trip";
	const double SizeTolerance = 1;

	public Issue19064(TestDevice testDevice)
		: base(testDevice)
	{
	}

	public override string Issue => "[iOS] ItemSizingStrategy gallery displays items inconsistently";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FirstItemRetainsRequestedSizeAfterHorizontalSwipeRoundTrips()
	{
		App.SetOrientationPortrait();
		App.WaitForElement(CollectionViewId);
		App.WaitForElement(ProgressId);
		App.WaitForElement(ResultId);

		App.RetryAssert(() =>
		{
			var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
			Assert.That(windowSize.Height, Is.GreaterThan(windowSize.Width), "The test window should be in portrait orientation.");
		});

		App.RetryAssert(() => Assert.That(App.WaitForElement(ProgressId).GetText(), Is.EqualTo("READY")));
		Assert.That(App.WaitForElement(ResultId).GetText(), Is.EqualTo("WAITING"));

		var completedRoundTrips = -1;
		App.RetryAssert(() => AssertExpectedSize(GetUniqueElement(BorderName).GetRect(), "First item border"));
		completedRoundTrips = 0;

		for (var cycle = 0; cycle < 3; cycle++)
		{
			DragCollection(left: true);
			App.RetryAssert(() =>
			{
				Assert.That(App.Query.ByName(BorderName), Is.Empty, "Item zero should be virtualized after the left drag.");
			});
			App.RetryAssert(() => Assert.That(
				App.WaitForElement(ProgressId).GetText(),
				Is.EqualTo($"RIGHT REACHED: cycle {cycle + 1}")));

			DragCollection(left: false);
			App.RetryAssert(() =>
			{
				Assert.That(App.Query.ByName(BorderName), Has.Count.EqualTo(1), "Item zero should return after the right drag.");
			});
			App.RetryAssert(() => Assert.That(
				App.WaitForElement(ProgressId).GetText(),
				Is.EqualTo($"LEFT RETURNED: cycle {cycle + 1}")));
			completedRoundTrips++;
		}

		Assert.That(completedRoundTrips, Is.EqualTo(3), "All three virtualization round trips should complete.");

		App.RetryAssert(() => Assert.That(
			App.WaitForElement(ResultId).GetText(),
			Does.StartWith("measured:"),
			"The post-trigger size callback should publish the rendered dimensions."));

		var result = App.WaitForElement(ResultId).GetText();
		Assert.That(
			result,
			Is.EqualTo("measured: managed=100x50; native=100x50; image=100x50"),
			$"{FailureSignature}; observed {result}");
	}

	void DragCollection(bool left)
	{
		var windowSize = ((AppiumApp)App).Driver.Manage().Window.Size;
		var collectionRect = App.WaitForElement(CollectionViewId).GetRect();
		var startX = (float)(windowSize.Width * (left ? 0.85 : 0.15));
		var endX = (float)(windowSize.Width * (left ? 0.15 : 0.85));
		var y = (float)collectionRect.CenterY();

		App.DragCoordinates(startX, y, endX, y);
	}

	IUIElement GetUniqueElement(string name)
	{
		var elements = App.Query.ByName(name);
		Assert.That(elements, Has.Count.EqualTo(1), $"{name} should identify exactly one rendered native element.");
		foreach (var element in elements)
			return element;

		throw new InvalidOperationException($"{name} was not rendered.");
	}

	static void AssertExpectedSize(System.Drawing.Rectangle rect, string elementName)
	{
		Assert.Multiple(() =>
		{
			Assert.That(rect.Width, Is.EqualTo(100).Within(SizeTolerance), $"{elementName} should initially be 100 wide.");
			Assert.That(rect.Height, Is.EqualTo(50).Within(SizeTolerance), $"{elementName} should initially be 50 high.");
		});
	}
}
#endif

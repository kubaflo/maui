using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue37361 : _IssuesUITest
{
	public override string Issue => "RefreshView pull-to-refresh does nothing when CollectionView is empty";

	public Issue37361(TestDevice testDevice) : base(testDevice)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void PullingEmptyCollectionViewInvokesRefreshCommand()
	{
		if (App is not AppiumIOSApp)
		{
			Assert.Ignore("Issue 37361 is iOS-specific.");
		}

		Assert.That(App.WaitForElement("EmptyStateLabel").GetText(), Is.EqualTo("No items"));
		Assert.That(App.WaitForElement("RefreshCountLabel").GetText(), Is.EqualTo("Refreshes: 0"));

		var collection = App.WaitForElement("EmptyCollection");
		Assert.That(collection.IsEnabled(), Is.True);

		var collectionRect = collection.GetRect();
		Assert.Multiple(() =>
		{
			Assert.That(collectionRect.Width, Is.GreaterThan(0));
			Assert.That(collectionRect.Height, Is.GreaterThan(0));
		});

		App.DragCoordinates(
			collectionRect.Left + (collectionRect.Width / 2),
			collectionRect.Top + 20,
			collectionRect.Left + (collectionRect.Width / 2),
			collectionRect.Bottom - 20);

		App.Tap("CheckEmptyRefreshButton");
		Assert.That(App.WaitForElement("StatusLabel").GetText(), Is.EqualTo("Check completed"));

		App.RetryAssert(() =>
		{
			Assert.That(
				App.FindElement("RefreshCountLabel").GetText(),
				Is.EqualTo("Refreshes: 1"),
				"Pulling the empty CollectionView should invoke RefreshView.Command.");
		});
	}
}

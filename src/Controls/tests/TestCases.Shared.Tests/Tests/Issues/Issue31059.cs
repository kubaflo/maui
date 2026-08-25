#if IOS
using NUnit.Framework;
using OpenQA.Selenium;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31059 : _IssuesUITest
{
	public Issue31059(TestDevice device) : base(device) { }

	public override string Issue => "CollectionView changes the centered item when rotating from portrait to landscape";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RotationPreservesCenteredItem()
	{
		App.SetOrientationPortrait();
		App.RetryAssert(
			() => Assert.That(App.GetOrientation(), Is.EqualTo(ScreenOrientation.Portrait)),
			timeout: TimeSpan.FromSeconds(10));

		var collection = App.WaitForElement("ImageCollection");
		Assert.That(collection, Is.Not.Null);

		var initialItem = App.WaitForElement("Item 0");
		Assert.That(initialItem, Is.Not.Null);

		var initialPosition = App.WaitForElement("CurrentPosition");
		Assert.That(initialPosition, Is.Not.Null);
		Assert.That(initialPosition.GetText(), Is.EqualTo("Current item: 0"));

		int currentIndex = 0;
		for (int i = 0; i < 4; i++)
		{
			App.SwipeRightToLeft();

			if (currentIndex < 4)
			{
				int settledIndex = -1;
				App.WaitForElement(
					() =>
					{
						var position = App.FindElement("CurrentPosition");
						if (position is null)
							return null;

						const string prefix = "Current item: ";
						var positionText = position.GetText();
						if (positionText is null
							|| !positionText.StartsWith(prefix, StringComparison.Ordinal)
							|| !int.TryParse(positionText[prefix.Length..], out int observedIndex)
							|| observedIndex <= currentIndex
							|| observedIndex > 4)
						{
							return null;
						}

						settledIndex = observedIndex;
						return position;
					},
					"Timed out waiting for the CollectionView center index to advance after its swipe",
					timeout: TimeSpan.FromSeconds(15));
				Assert.That(settledIndex, Is.GreaterThan(currentIndex));
				currentIndex = settledIndex;
			}
			else
			{
				App.RetryAssert(
					() =>
					{
						var position = App.FindElement("CurrentPosition");
						if (position is null)
							throw new AssertionException("The centered-item state was not available after the swipe");

						Assert.That(position.GetText(), Is.EqualTo("Current item: 4"));
					},
					timeout: TimeSpan.FromSeconds(15));
			}
		}

		Assert.That(currentIndex, Is.EqualTo(4), "Four left swipes should reach Item 4");
		var itemFourPosition = App.WaitForElement(
			() =>
			{
				var position = App.FindElement("CurrentPosition");
				if (position is null || position.GetText() != "Current item: 4")
					return null;

				return position;
			},
			"Timed out waiting for Item 4 to become centered",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(itemFourPosition, Is.Not.Null);

		var itemFour = App.WaitForElement("Item 4");
		Assert.That(itemFour, Is.Not.Null);

		App.Tap("ArmOrientationCheck");
		var armedTransition = App.WaitForElement("SizeTransition");
		Assert.That(armedTransition, Is.Not.Null);
		Assert.That(armedTransition.GetText(), Is.EqualTo("Size transition: -1"));

		App.SetOrientationLandscape();
		App.RetryAssert(
			() => Assert.That(App.GetOrientation(), Is.EqualTo(ScreenOrientation.Landscape)),
			timeout: TimeSpan.FromSeconds(10));

		var completedTransition = App.WaitForElement(
			() =>
			{
				var transition = App.FindElement("SizeTransition");
				if (transition is null || transition.GetText() != "Size transition: 1")
					return null;

				return transition;
			},
			"Timed out waiting for the landscape page SizeChanged transition",
			timeout: TimeSpan.FromSeconds(15));
		Assert.That(completedTransition, Is.Not.Null);

		App.RetryAssert(
			() =>
			{
				var finalPosition = App.FindElement("CurrentPosition");
				if (finalPosition is null)
					throw new AssertionException("CurrentPosition was not found after rotation");

				Assert.That(
					finalPosition.GetText(),
					Is.EqualTo("Current item: 4"),
					"Issue31059: portrait-to-landscape rotation changed the centered item");
			},
			timeout: TimeSpan.FromSeconds(5));

		var finalItemFour = App.WaitForElement("Item 4");
		Assert.That(finalItemFour, Is.Not.Null);
	}
}
#endif

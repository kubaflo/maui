#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue9567 : _IssuesUITest
{
	public Issue9567(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "CollectionView SelectionChanged is not raised when tapping a button in an item";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void TappingButtonInItemRaisesSelectionChangedBeforeRemovingItem()
	{
		App.SetOrientationPortrait();

		var collectionBounds = App.WaitForElement("Issue9567CollectionView").GetRect();
		Assert.That(collectionBounds.Height, Is.GreaterThan(collectionBounds.Width), "The issue requires a portrait-sized window.");

		App.WaitForElement("Issue9567Model1");
		App.WaitForElement("Issue9567DeleteModel1");
		App.WaitForElement("Issue9567 item count: 4");
		Assert.That(App.FindElement("Issue9567SelectionCallbackCount").GetText(), Is.EqualTo("-1"));

		App.Tap("Issue9567DeleteModel1");

		App.WaitForNoElement("Issue9567Model1");
		App.WaitForElement("Issue9567 item count: 3");

		var callbackCountText = App.FindElement("Issue9567SelectionCallbackCount").GetText();
		var selectedIdentity = App.FindElement("Issue9567SelectedIdentity").GetText();

		Assert.That(int.TryParse(callbackCountText, out var callbackCount), Is.True,
			$"SelectionChanged callback count '{callbackCountText}' was not numeric.");
		Assert.That(callbackCount, Is.GreaterThanOrEqualTo(1),
			$"SelectionChanged callback count was {callbackCount} after the first item's delete button received the tap; expected at least 1");
		Assert.That(selectedIdentity, Is.EqualTo("model_1"));
	}
}
#endif

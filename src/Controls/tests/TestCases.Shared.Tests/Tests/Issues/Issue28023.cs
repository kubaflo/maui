#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28023 : _IssuesUITest
{
	const double GeometryTolerance = 2;
	const double UpdatedSpacing = 90;

	public Issue28023(TestDevice testDevice) : base(testDevice) { }

	public override string Issue => "CollectionView ItemSpacing persists after re-entering the page";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void FreshVerticalCollectionViewUsesDefaultItemSpacingAfterReentry()
	{
		App.WaitForElement("VerticalListSpacingButton");
		App.Tap("VerticalListSpacingButton");

		Assert.That(App.WaitForElement("SpacingEntry").GetText(), Is.EqualTo("0"));
		App.RetryAssert(() =>
			Assert.That(App.WaitForElement("PageVisitStatus").GetText(), Is.EqualTo("1")));
		App.RetryAssert(() =>
		{
			var initialGap = GetVerticalItemGap();
			Assert.That(initialGap, Is.EqualTo(0).Within(GeometryTolerance),
				$"The first page should start with ItemSpacing 0; measured gap: {initialGap}.");
		});

		App.ClearText("SpacingEntry");
		App.EnterText("SpacingEntry", "90");
		App.Tap("UpdateSpacingButton");
		Assert.That(App.WaitForElement("SpacingEntry").GetText(), Is.EqualTo("90"));
		App.RetryAssert(() =>
		{
			var updatedGap = GetVerticalItemGap();
			Assert.That(updatedGap, Is.EqualTo(UpdatedSpacing).Within(GeometryTolerance),
				$"Updating ItemSpacing to 90 should arrange a 90-point gap; measured gap: {updatedGap}.");
		});

		this.Back();
		App.WaitForElement("VerticalListSpacingButton");
		App.Tap("VerticalListSpacingButton");

		App.RetryAssert(() =>
			Assert.That(App.WaitForElement("PageVisitStatus").GetText(), Is.EqualTo("2")));
		Assert.That(App.WaitForElement("SpacingEntry").GetText(), Is.EqualTo("0"));
		var freshPageGap = GetVerticalItemGap();
		Assert.That(freshPageGap, Is.EqualTo(0).Within(GeometryTolerance),
			$"Fresh vertical CollectionView should render ItemSpacing 0 after re-entry; measured gap: {freshPageGap}, expected: 0.");
	}

	double GetVerticalItemGap()
	{
		var firstItem = App.WaitForElement("Monkey 2");
		var secondItem = App.WaitForElement("Monkey 3");

		Assert.That(firstItem.GetText(), Is.EqualTo("Monkey 2"));
		Assert.That(secondItem.GetText(), Is.EqualTo("Monkey 3"));

		var firstRect = firstItem.GetRect();
		var secondRect = secondItem.GetRect();
		Assert.That(firstRect.Height, Is.EqualTo(48).Within(GeometryTolerance));
		Assert.That(secondRect.Height, Is.EqualTo(48).Within(GeometryTolerance));
		Assert.That(secondRect.Y, Is.GreaterThan(firstRect.Y));

		return secondRect.Y - (firstRect.Y + firstRect.Height);
	}
}
#endif

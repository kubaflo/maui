#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28023 : _IssuesUITest
{
	public override string Issue => "Item spacing is retained when reopening a CollectionView page";

	public Issue28023(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ReopenedVerticalListUsesDefaultItemSpacing()
	{
		const int tolerance = 2;
		int instance = -1;

		App.WaitForElement("OpenVerticalSpacing");
		App.Tap("OpenVerticalSpacing");
		App.RetryAssert(() =>
		{
			var loadState = App.WaitForElement("DetailPageLoadState").GetText();
			Assert.That(loadState, Is.Not.Null);
			if (loadState is null)
				Assert.Fail("Issue28023 detail page load state was unavailable.");
			Assert.That(loadState, Is.EqualTo("Loaded"));
		});

		var firstInstanceText = App.WaitForElement("DetailPageInstance").GetText();
		Assert.That(firstInstanceText, Is.Not.Null);
		if (firstInstanceText is null)
			Assert.Fail("Issue28023 first detail page instance was unavailable.");
		Assert.That(int.TryParse(firstInstanceText, out instance), Is.True);
		Assert.That(instance, Is.EqualTo(1));

		var initialEntryText = App.WaitForElement("SpacingEntry").GetText();
		Assert.That(initialEntryText, Is.Not.Null);
		if (initialEntryText is null)
			Assert.Fail("Issue28023 initial spacing entry was unavailable.");
		Assert.That(initialEntryText, Is.EqualTo("0"));

		var initialBaboon = App.WaitForElement("MonkeyNameBaboon").GetRect();
		var initialCapuchin = App.WaitForElement("MonkeyNameCapuchin").GetRect();
		Assert.That(initialBaboon.Width, Is.GreaterThan(0));
		Assert.That(initialBaboon.Height, Is.GreaterThan(0));
		Assert.That(initialCapuchin.Width, Is.GreaterThan(0));
		Assert.That(initialCapuchin.Height, Is.GreaterThan(0));
		Assert.That(initialCapuchin.Y, Is.GreaterThan(initialBaboon.Y));
		int initialRowDelta = initialCapuchin.Y - initialBaboon.Y;

		App.ClearText("SpacingEntry");
		App.EnterText("SpacingEntry", "90");
		App.Tap("UpdateSpacingButton");

		App.RetryAssert(() =>
		{
			var updatedBaboon = App.WaitForElement("MonkeyNameBaboon").GetRect();
			var updatedCapuchin = App.WaitForElement("MonkeyNameCapuchin").GetRect();
			int updatedRowDelta = updatedCapuchin.Y - updatedBaboon.Y;
			Assert.That(updatedRowDelta, Is.GreaterThan(initialRowDelta + tolerance));
		});

		this.Back();
		App.WaitForElement("OpenVerticalSpacing");
		instance = -1;
		App.Tap("OpenVerticalSpacing");
		App.RetryAssert(() =>
		{
			var loadState = App.WaitForElement("DetailPageLoadState").GetText();
			Assert.That(loadState, Is.Not.Null);
			if (loadState is null)
				Assert.Fail("Issue28023 re-entry detail page load state was unavailable.");
			Assert.That(loadState, Is.EqualTo("Loaded"));
		});

		var secondInstanceText = App.WaitForElement("DetailPageInstance").GetText();
		Assert.That(secondInstanceText, Is.Not.Null);
		if (secondInstanceText is null)
			Assert.Fail("Issue28023 second detail page instance was unavailable.");
		Assert.That(int.TryParse(secondInstanceText, out instance), Is.True);
		Assert.That(instance, Is.EqualTo(2));

		var reentryEntryText = App.WaitForElement("SpacingEntry").GetText();
		Assert.That(reentryEntryText, Is.Not.Null);
		if (reentryEntryText is null)
			Assert.Fail("Issue28023 re-entry spacing entry was unavailable.");
		Assert.That(reentryEntryText, Is.EqualTo("0"));

		var baboon = App.WaitForElement("MonkeyNameBaboon").GetRect();
		var capuchin = App.WaitForElement("MonkeyNameCapuchin").GetRect();
		Assert.That(baboon.Width, Is.GreaterThan(0));
		Assert.That(baboon.Height, Is.GreaterThan(0));
		Assert.That(capuchin.Width, Is.GreaterThan(0));
		Assert.That(capuchin.Height, Is.GreaterThan(0));
		Assert.That(capuchin.Y, Is.GreaterThan(baboon.Y));

		int rowDelta = capuchin.Y - baboon.Y;
		Assert.That(rowDelta, Is.EqualTo(initialRowDelta).Within(tolerance),
			$"Issue28023 re-entry monkey-row delta was {rowDelta}px; expected initial zero-spacing delta {initialRowDelta}px within {tolerance}px after instance {instance}.");
	}
}
#endif

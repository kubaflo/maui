#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27344 : _IssuesUITest
{
	public override string Issue => "PopModalAsync accesses properties on a deleted page BindingContext";

	public Issue27344(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.Navigation)]
	public void PopModalDoesNotReadDeletedBindingContext()
	{
		var initialReceipt = App.WaitForElement("DeleteReceiptLabel");
		Assert.That(initialReceipt.GetText(), Is.EqualTo("Delete received: 0"));

		var initialReadCount = App.WaitForElement("PostDeleteReadCountLabel");
		Assert.That(initialReadCount.GetText(), Is.EqualTo("Post-delete reads: -1"));

		App.WaitForElement("OpenModalButton");
		App.Tap("OpenModalButton");

		var boundButton = App.WaitForElement("BoundActionButton");
		Assert.That(boundButton.IsEnabled(), Is.True);

		var deleteToolbarItemQuery = AppiumQuery.ByAccessibilityId("DeleteToolbarItem");
		var deleteToolbarItem = App.WaitForElement(deleteToolbarItemQuery);
		Assert.That(deleteToolbarItem.GetText(), Is.EqualTo("Delete"));
		App.Tap(deleteToolbarItemQuery);

		Assert.That(
			App.WaitForTextToBePresentInElement(
				"DeleteReceiptLabel",
				"Delete received: 1",
				timeout: TimeSpan.FromSeconds(10)),
			Is.True);

		var finalizedReadCount = App.WaitForElement("PostDeleteReadCountLabel");
		const string countPrefix = "Post-delete reads: ";
		var countText = finalizedReadCount.GetText();
		if (countText is null)
			throw new AssertionException("The finalized post-delete read count had no text.");

		Assert.That(countText, Does.StartWith(countPrefix));

		var parsed = int.TryParse(
			countText[countPrefix.Length..],
			System.Globalization.NumberStyles.Integer,
			System.Globalization.CultureInfo.InvariantCulture,
			out var count);
		Assert.That(parsed, Is.True, "The finalized post-delete read count was not an integer.");
		Assert.That(count, Is.GreaterThanOrEqualTo(0), "The post-delete read count was not finalized after the modal pop.");
		Assert.That(count, Is.EqualTo(0), $"PopModalAsync re-read the deleted PersonViewModel.CanDelete binding; post-delete reads: {count}, expected: 0.");
	}
}
#endif

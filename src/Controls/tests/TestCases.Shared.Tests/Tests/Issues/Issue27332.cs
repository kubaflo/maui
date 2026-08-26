#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue27332 : _IssuesUITest
{
	public override string Issue => "CollectionView footer is displayed at the bottom of the page";

	public Issue27332(TestDevice device) : base(device)
	{
	}

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void EmptyCollectionFooterImmediatelyFollowsHeaderAfterClear()
	{
		App.WaitForElement("AddItemsButton");
		App.WaitForElement("ClearItemsButton");
		var header = App.WaitForElement("HeaderView");
		var footer = App.WaitForElement("FooterView");
		Assert.That(header.GetText(), Is.EqualTo("Header"));
		Assert.That(footer.GetText(), Is.EqualTo("Footer"));
		var headerRect = header.GetRect();
		var footerRect = footer.GetRect();
		Assert.That(headerRect.Width, Is.GreaterThan(0));
		Assert.That(headerRect.Height, Is.GreaterThan(0));
		Assert.That(footerRect.Width, Is.GreaterThan(0));
		Assert.That(footerRect.Height, Is.GreaterThan(0));
		Assert.That(App.WaitForElement("ActionStatus").GetText(), Is.EqualTo("Clear handled: 0"));

		App.Tap("ClearItemsButton");
		var clearHandled = App.WaitForTextToBePresentInElement(
			"ActionStatus",
			"Clear handled: 1",
			timeout: TimeSpan.FromSeconds(10));
		Assert.That(clearHandled, Is.True, "The clear action must be handled before measuring the footer.");
		Assert.That(App.WaitForElement("ActionStatus").GetText(), Is.EqualTo("Clear handled: 1"));

		headerRect = App.WaitForElement("HeaderView").GetRect();
		footerRect = App.WaitForElement("FooterView").GetRect();
		var footerGap = footerRect.Y - (headerRect.Y + headerRect.Height);

		Assert.That(
			Math.Abs(footerGap),
			Is.LessThanOrEqualTo(2),
			$"Issue27332 footer gap after clear: observed gap {footerGap}, expected a zero gap within 2 pixels.");
	}
}
#endif

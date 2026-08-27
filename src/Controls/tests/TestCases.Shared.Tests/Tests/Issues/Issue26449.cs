using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

#if ANDROID
using System.Globalization;
#endif

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue26449 : _IssuesUITest
{
	public Issue26449(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Unable to scroll inner CollectionView of nested CollectionViews";

#if ANDROID
	[Test]
	[Category(UITestCategories.CollectionView)]
	public void DraggingInnerCollectionViewScrollsInnerList()
	{
		const string innerOffsetId = "Issue26449InnerOffset";
		const string outerOffsetId = "Issue26449OuterOffset";
		const string callbackId = "Issue26449Callback";

		var readyLabel = App.WaitForElement("Issue26449Ready");
		Assert.That(readyLabel, Is.Not.Null);
		Assert.That(readyLabel.GetText(), Is.EqualTo("Groups=4;FirstInnerItems=20"));

		var item = App.WaitForElement("Inner 1 item 4");
		Assert.That(item, Is.Not.Null);
		var itemRect = item.GetRect();
		Assert.That(itemRect.Height, Is.GreaterThan(0), "The expected first inner-list item was not realized.");

		Assert.That(GetRequiredText(innerOffsetId), Is.EqualTo("-1"));
		Assert.That(GetRequiredText(outerOffsetId), Is.EqualTo("-1"));
		Assert.That(GetRequiredText(callbackId), Is.EqualTo("Waiting"));

		App.Tap("Issue26449Prepare");

		Assert.That(GetRequiredText(innerOffsetId), Is.EqualTo("-1"));
		Assert.That(GetRequiredText(outerOffsetId), Is.EqualTo("-1"));
		Assert.That(GetRequiredText(callbackId), Is.EqualTo("Waiting"));

		var nativeWindow = App.WaitForElement(AppiumQuery.ByXPath("//android.widget.FrameLayout[1]"));
		Assert.That(nativeWindow, Is.Not.Null);
		var windowRect = nativeWindow.GetRect();
		var startX = itemRect.X + (itemRect.Width / 2);
		var startY = itemRect.Y + (itemRect.Height / 2);
		var travel = Math.Max(windowRect.Height * 0.16f, 100f);

		App.DragCoordinates(startX, startY, startX, startY - travel);

		Assert.That(
			App.WaitForTextToBePresentInElement(callbackId, "Observed", TimeSpan.FromSeconds(5)),
			Is.True,
			"A post-trigger CollectionView Scrolled callback was not observed.");

		var innerOffsetText = GetRequiredText(innerOffsetId);
		var outerOffsetText = GetRequiredText(outerOffsetId);
		Assert.That(double.TryParse(innerOffsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out var innerOffset), Is.True);
		Assert.That(double.TryParse(outerOffsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out var outerOffset), Is.True);

		Assert.That(
			innerOffset > 1 && outerOffset <= 1,
			Is.True,
			"Nested drag scrolled the outer CollectionView instead of the inner CollectionView.");
	}

	string GetRequiredText(string automationId)
	{
		var element = App.WaitForElement(automationId);
		Assert.That(element, Is.Not.Null);
		var text = element.GetText();
		if (text is null)
			throw new AssertionException($"Element '{automationId}' did not expose text.");

		return text;
	}
#endif
}

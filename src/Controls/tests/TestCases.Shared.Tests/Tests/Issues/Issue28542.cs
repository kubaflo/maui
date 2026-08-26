#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue28542 : _IssuesUITest
{
	public Issue28542(TestDevice device)
		: base(device)
	{
	}

	public override string Issue => "CollectionView scrollbar has inconsistent sizing with variable-height items";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void ScrollbarThumbUsesTotalVariableItemExtent()
	{
		App.SetOrientationPortrait();

		var itemsElement = App.WaitForElement("ItemsView");
		if (itemsElement is null)
			throw new InvalidOperationException("The CollectionView was not found.");

		var shortItem = App.WaitForElement("ShortItem1");
		if (shortItem is null)
			throw new InvalidOperationException("Short item 1 was not found.");

		var itemsRect = itemsElement.GetRect();
		var shortRect = shortItem.GetRect();
		Assert.That(itemsRect.Width, Is.GreaterThan(0));
		Assert.That(itemsRect.Height, Is.GreaterThan(0));
		Assert.That(shortRect.Height, Is.GreaterThan(0));
		Assert.That(App.WaitForElement("ScrollResult").GetText(), Is.EqualTo("WaitingForScroll"));

		App.DragCoordinates(
			itemsRect.CenterX(),
			itemsRect.CenterY(),
			itemsRect.CenterX(),
			itemsRect.CenterY() - Math.Max(30, itemsRect.Height * 8 / 100));
		Assert.That(App.WaitForTextToBePresentInElement("ScrollResult", "F:"), Is.True,
			"The first touch drag must produce a native scroll callback.");
		var firstStateText = App.WaitForElement("ScrollResult").GetText();
		if (firstStateText is null)
			throw new InvalidOperationException("The first native scrollbar state was unavailable.");
		var firstState = ParseRangeState(firstStateText);

		App.DragCoordinates(
			itemsRect.CenterX(),
			itemsRect.CenterY(),
			itemsRect.CenterX(),
			itemsRect.CenterY() - itemsRect.Height * 30 / 100);
		App.DragCoordinates(
			itemsRect.CenterX(),
			itemsRect.CenterY(),
			itemsRect.CenterX(),
			itemsRect.CenterY() - itemsRect.Height * 15 / 100);

		var tallItem = App.WaitForElement("TallItem9");
		if (tallItem is null)
			throw new InvalidOperationException("Tall item 9 was not found after the recorded drag.");

		var tallRect = tallItem.GetRect();
		Assert.That(tallRect.Y, Is.LessThan(itemsRect.Y + itemsRect.Height), "Tall item 9 must be inside the CollectionView viewport.");
		Assert.That(tallRect.Y + tallRect.Height, Is.GreaterThan(itemsRect.Y), "Tall item 9 must overlap the CollectionView viewport.");
		App.WaitForNoElement("ShortItem1");

		var tallStateText = App.WaitForElement("ScrollResult").GetText();
		if (tallStateText is null)
			throw new InvalidOperationException("The tall-item native scrollbar state was unavailable.");
		var tallState = ParseRangeState(tallStateText);
		Assert.That(tallState.Count, Is.GreaterThan(firstState.Count),
			"The recorded touch sequence must produce another native scroll callback while tall items are visible.");

		if (tallState.CurrentRange != firstState.FirstRange)
			Assert.Fail("CollectionView native vertical scroll range changed in tall-item region.");
	}

	static (int FirstRange, int CurrentRange, int Count) ParseRangeState(string text)
	{
		var parts = text.Split(';');
		if (parts.Length != 3 ||
			!int.TryParse(parts[0].Replace("F:", string.Empty, StringComparison.Ordinal), out var firstRange) ||
			!int.TryParse(parts[1].Replace("C:", string.Empty, StringComparison.Ordinal), out var currentRange) ||
			!int.TryParse(parts[2].Replace("N:", string.Empty, StringComparison.Ordinal), out var count))
			throw new InvalidOperationException($"The native scrollbar state was invalid: {text}");

		return (firstRange, currentRange, count);
	}
}
#endif

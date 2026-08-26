#if WINDOWS
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue29120 : _IssuesUITest
{
	public Issue29120(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Incremental loading resets the visible range";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void IncrementalLoadingPreservesVisibleRange()
	{
		App.WaitForElement("Bear 1");
		var collectionView = App.WaitForElement("AnimalsCollectionView");
		var collectionRect = collectionView.GetRect();
		Assert.That(collectionRect.Width, Is.GreaterThan(0));
		Assert.That(collectionRect.Height, Is.GreaterThan(0));

		var stateElement = App.WaitForElement("CollectionStateLabel");
		var state = ReadState(stateElement);
		Assert.That(state.Count, Is.EqualTo(10));

		for (var swipe = 0; swipe < 4; swipe++)
		{
			var sequenceBeforeSwipe = state.Sequence;
			var changeSequenceBeforeSwipe = state.ChangeSequence;
			App.DragCoordinates(
				collectionRect.CenterX(),
				collectionRect.Y + (collectionRect.Height * 4 / 5),
				collectionRect.CenterX(),
				collectionRect.Y + (collectionRect.Height / 5));

			stateElement = App.WaitForElement(
				() =>
				{
					var currentElement = App.FindElement("CollectionStateLabel");
					if (currentElement is null)
						return null;

					var currentState = ReadState(currentElement);
					return currentState.Sequence > sequenceBeforeSwipe &&
						currentState.ChangeSequence > changeSequenceBeforeSwipe
							? currentElement
							: null;
				},
				$"Swipe {swipe + 1} did not produce a new CollectionView Scrolled callback",
				TimeSpan.FromSeconds(10));
			state = ReadState(stateElement);
			Assert.That(state.ChangeSequence, Is.GreaterThan(changeSequenceBeforeSwipe),
				$"Swipe {swipe + 1} did not change the first visible index; observed index={state.First}");
		}

		stateElement = App.WaitForElement(
			() =>
			{
				var currentElement = App.FindElement("CollectionStateLabel");
				if (currentElement is null)
					return null;

				var currentState = ReadState(currentElement);
				return currentState.Count >= 20 &&
					currentState.IndexBeforeLoad >= 2 &&
					currentState.Sequence > currentState.LoadSequence &&
					currentState.PostLoadFirst >= 0 &&
					currentState.ThresholdEvents > 0 &&
					currentState.HasLoadedItem
						? currentElement
						: null;
			},
			"The threshold command did not append a new page after scrolling beyond item 2",
			TimeSpan.FromSeconds(15));

		state = ReadState(stateElement);
		Assert.That(
			state.PostLoadFirst,
			Is.GreaterThanOrEqualTo(2),
			"Issue 29120: incremental loading reset the visible range to the top");
	}

	static CollectionState ReadState(IUIElement element)
	{
		var text = element.GetText();
		if (text is null)
			throw new AssertionException("CollectionStateLabel did not expose text");

		var values = text.Split(';');
		Assert.That(values, Has.Length.GreaterThanOrEqualTo(9), $"Invalid collection state: {text}");

		return new CollectionState(
			int.Parse(values[0], CultureInfo.InvariantCulture),
			int.Parse(values[1], CultureInfo.InvariantCulture),
			int.Parse(values[2], CultureInfo.InvariantCulture),
			int.Parse(values[3], CultureInfo.InvariantCulture),
			int.Parse(values[4], CultureInfo.InvariantCulture),
			int.Parse(values[5], CultureInfo.InvariantCulture),
			int.Parse(values[6], CultureInfo.InvariantCulture),
			int.Parse(values[7], CultureInfo.InvariantCulture),
			!values[8].Equals("None", StringComparison.Ordinal));
	}

	readonly record struct CollectionState(
		int Sequence,
		int First,
		int Count,
		int LoadSequence,
		int IndexBeforeLoad,
		int PostLoadFirst,
		int ChangeSequence,
		int ThresholdEvents,
		bool HasLoadedItem);
}
#endif

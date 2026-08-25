using UIKit;

namespace Microsoft.Maui.Controls.Platform;

internal static class AcessibilityExtensions
{
	internal static void UpdateAccessibilityTraits(this UICollectionView collectionView, SelectableItemsView itemsView)
	{
		foreach (var subview in collectionView.Subviews)
		{
			if (subview is UICollectionViewCell cell)
			{
				cell.UpdateAccessibilityTraits(itemsView);
			}
		}
	}

	internal static void UpdateAccessibilityTraits(this UICollectionViewCell cell, ItemsView itemsView)
	{
		var selectionMode = (itemsView as CollectionView)?.SelectionMode;
		if (cell.ContentView is not null
			&& cell.ContentView.Subviews.Length > 0
			&& selectionMode is not null)
		{
			var firstChild = cell.ContentView.Subviews[0];

			// if the first child is a control, changing the accessibility traits from an entry to a button could be confusing.
			if (firstChild is UIControl)
			{
				return;
			}

			var selectable = selectionMode != SelectionMode.None;

			ApplyItemTrait(firstChild, selectable);

			// UIKit only reports traits for the views it exposes to assistive technology, and that
			// exposure is declared by IsAccessibilityElement. When the item template declares its
			// semantics on an inner element, MAUI promotes that element instead, so a trait written
			// only to the template root is never announced. Mirror it onto the exposed elements.
			foreach (var subview in firstChild.Subviews)
			{
				ApplyItemTraitToExposedElements(subview, selectable);
			}
		}
	}

	static void ApplyItemTraitToExposedElements(UIView view, bool selectable)
	{
		// Native controls keep their own role so an embedded CheckBox or Entry is not announced
		// as a button.
		if (view is UIControl)
		{
			return;
		}

		if (view.IsAccessibilityElement)
		{
			ApplyItemTrait(view, selectable);

			// UIKit does not surface the children of an accessibility element.
			return;
		}

		foreach (var subview in view.Subviews)
		{
			ApplyItemTraitToExposedElements(subview, selectable);
		}
	}

	static void ApplyItemTrait(UIView view, bool selectable)
	{
		if (selectable)
		{
			view.AccessibilityTraits |= UIAccessibilityTrait.Button;
		}
		else
		{
			view.AccessibilityTraits &= ~UIAccessibilityTrait.Button;
		}
	}
}

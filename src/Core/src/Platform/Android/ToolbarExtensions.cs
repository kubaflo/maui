using AndroidX.Core.View;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	internal static class ToolbarExtensions
	{
		public static void UpdateTitle(this AToolbar nativeToolbar, IToolbar toolbar)
		{
			var childrenBeforeTitle = SnapshotChildren(nativeToolbar);

			nativeToolbar.Title = toolbar?.Title ?? string.Empty;

			MarkAddedTitleViewAsHeading(nativeToolbar, childrenBeforeTitle);
		}

		static AView?[]? SnapshotChildren(AToolbar nativeToolbar)
		{
			var childCount = nativeToolbar.ChildCount;
			if (childCount <= 0)
				return null;

			var children = new AView?[childCount];
			for (int i = 0; i < childCount; i++)
				children[i] = nativeToolbar.GetChildAt(i);

			return children;
		}

		// AndroidX's Toolbar owns the TextView that renders the title: it creates and attaches
		// that view while the Title is being assigned. A child that is present after the
		// assignment but was absent before it is therefore the title view, which lets the page
		// title be exposed to TalkBack as a heading (parity with iOS and Windows) without
		// inspecting whatever text a view happens to display.
		static void MarkAddedTitleViewAsHeading(AToolbar nativeToolbar, AView?[]? childrenBeforeTitle)
		{
			var childCount = nativeToolbar.ChildCount;

			for (int i = 0; i < childCount; i++)
			{
				if (nativeToolbar.GetChildAt(i) is not ATextView child)
					continue;

				if (WasPresentBefore(childrenBeforeTitle, child))
					continue;

				ViewCompat.SetAccessibilityHeading(child, true);
				return;
			}
		}

		static bool WasPresentBefore(AView?[]? childrenBeforeTitle, AView child)
		{
			if (childrenBeforeTitle is null)
				return false;

			foreach (var previousChild in childrenBeforeTitle)
			{
				if (previousChild is not null && previousChild.Equals(child))
					return true;
			}

			return false;
		}
	}
}

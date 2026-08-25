using System;
using System.Collections.Generic;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	[System.Runtime.Versioning.SupportedOSPlatform("ios13.0")]
	public partial class MenuFlyoutItemHandler
	{
		internal static Dictionary<int, IMenuElement> menus = new Dictionary<int, IMenuElement>();

		bool IsInContextFlyout()
		{
			IElement? current = VirtualView;
			while (current != null)
			{
				if (current is Microsoft.Maui.IMenuFlyout)
					return true;
				current = current.Parent;
			}
			return false;
		}

		protected override UIMenuElement CreatePlatformElement()
		{
			// https://github.com/dotnet/maui/issues/9332
			// The menu code needs to be converted over to using `UIAction`
			// so that all of this can be the same
			if (IsInContextFlyout())
			{
				UIImage? contextUiImage = VirtualView.Source.GetPlatformMenuImage(MauiContext!);

				var uiAction = UIAction.Create(
					title: VirtualView.Text,
					image: contextUiImage,
					identifier: null,
					handler: (_) => VirtualView?.Clicked());

				return uiAction;
			}

			return VirtualView.CreateMenuItem(MauiContext!);
		}

		public static void MapIsEnabled(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			handler.PlatformView?.UpdateIsEnabled(view);
		}

		internal static void MapSource(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			// The platform element is created once; mutate its image in place so the
			// UIMenuElement identity (and therefore the containing UIMenu) is preserved.
			if (handler is not MenuFlyoutItemHandler { MauiContext: not null } typedHandler)
				return;

			var platformView = handler.PlatformView;
			if (platformView is null)
				return;

			var image = view.Source.GetPlatformMenuImage(typedHandler.MauiContext);

			switch (platformView)
			{
				case UIAction action:
					action.Image = image;
					break;
				case UICommand command:
					command.Image = image;
					break;
			}
		}

		internal static void Execute(UICommand uICommand)
		{
			if (uICommand.PropertyList is NSString nsString &&
				Int32.TryParse(nsString.ToString(), out int index))
			{
				menus[index].Clicked();
			}
		}

		internal static void Reset()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(13))
				return;

			menus.Clear();
		}
	}
}

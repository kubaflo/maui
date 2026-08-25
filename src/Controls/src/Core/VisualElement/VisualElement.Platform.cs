using System;
using System.Collections.Generic;
using System.Text;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif MONOANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
using PlatformView = Tizen.NUI.BaseComponents.View;
#endif

namespace Microsoft.Maui.Controls
{
	public partial class VisualElement
	{
		IDisposable? _loadedUnloadedToken;
		partial void HandlePlatformUnloadedLoaded()
		{
			_loadedUnloadedToken?.Dispose();
			_loadedUnloadedToken = null;

			// Window and this VisualElement both have a handler to work with
			if (Window?.Handler?.PlatformView is not null &&
				Handler?.PlatformView is PlatformView view)
			{
				if (view.IsLoaded())
				{
					SendLoaded(false);

					// If SendLoaded caused the unloaded tokens to wire up
					_loadedUnloadedToken?.Dispose();
					_loadedUnloadedToken = null;
					_loadedUnloadedToken = this.OnUnloaded(SendUnloadedUnlessHostedByContainerController);
				}
				else
				{
					SendUnloaded(false);

					// If SendUnloaded caused the unloaded tokens to wire up
					_loadedUnloadedToken?.Dispose();
					_loadedUnloadedToken = null;
					if (Handler is not null)
					{
						_loadedUnloadedToken = this.OnLoaded(SendLoaded);
					}
				}
			}
			// This VisualElement has a handler with MauiContext but no MAUI Window.
			// This happens when the view is hosted inside a native container via ToPlatform().
			// We still need to fire Loaded/Unloaded based on the platform view's attach state.
			else if (Window is null &&
				Handler?.MauiContext is not null &&
				Handler?.PlatformView is PlatformView nativeHostedView)
			{
				if (nativeHostedView.IsLoaded())
				{
					SendLoaded(false);

					// If SendLoaded caused the unloaded tokens to wire up
					_loadedUnloadedToken?.Dispose();
					_loadedUnloadedToken = null;
					_loadedUnloadedToken = this.OnUnloaded(SendUnloaded);
				}
				else
				{
					// Not yet attached to a native parent; wait for the platform loaded event
					_loadedUnloadedToken?.Dispose();
					_loadedUnloadedToken = null;
					if (Handler is not null)
					{
						_loadedUnloadedToken = this.OnLoaded(SendLoaded);
					}
				}
			}
			else
			{
				// My handler is still set but the window handler isn't set.
				// This means I'm starting to detach from the platform window
				// So we wait for the platform detach events to fire before calling 
				// OnUnloaded
				if (Handler?.PlatformView is PlatformView detachingView &&
					detachingView.IsLoaded())
				{
					_loadedUnloadedToken = this.OnUnloaded(SendUnloaded);
				}
				else
				{
					SendUnloaded();
				}
			}
		}

		// A platform view losing its window is not proof that the element left the visual tree.
		// On iOS a UINavigationController detaches the covered page's UIView from the UIWindow on
		// every push, yet the page's UIViewController stays installed in its container controller
		// (NavigationRenderer calls pack.AddChildViewController for each page and only calls
		// RemoveFromParentViewController when the page is really torn down). UIKit's container
		// hierarchy is therefore the authoritative record of visual-tree membership: while a parent
		// view controller is still hosting us the detach is window-level only, so Unloaded is
		// suppressed and we just wait to re-arm the unload watcher once the view is back in a
		// window. Real removal drives the element's Window to null, which re-enters
		// HandlePlatformUnloadedLoaded and unloads through the paths above.
		void SendUnloadedUnlessHostedByContainerController()
		{
#if IOS || MACCATALYST
			if (Handler is Microsoft.Maui.IPlatformViewHandler { ViewController: UIKit.UIViewController viewController } &&
				viewController.ParentViewController is not null)
			{
				_loadedUnloadedToken?.Dispose();
				_loadedUnloadedToken = null;
				_loadedUnloadedToken = this.OnLoaded(HandlePlatformUnloadedLoaded);
				return;
			}
#endif

			SendUnloaded();
		}
	}
}

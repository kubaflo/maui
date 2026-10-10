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
#if IOS || MACCATALYST
		WeakReference<PlatformView>? _loadedUnloadedFallbackView;
#endif

		partial void HandlePlatformUnloadedLoaded()
		{
			_loadedUnloadedToken?.Dispose();
			_loadedUnloadedToken = null;

#if IOS || MACCATALYST
			if (Window is null)
				_loadedUnloadedFallbackView = null;
#endif

			// Window and this VisualElement both have a handler to work with
			if (Window?.Handler?.PlatformView is not null &&
				Handler?.PlatformView is PlatformView view)
			{
#if IOS || MACCATALYST
				if (view.Superview is PlatformView superview)
					_loadedUnloadedFallbackView = new(superview);
#endif

				if (view.IsLoaded())
				{
					SendLoaded(false);

					// If SendLoaded caused the unloaded tokens to wire up
					_loadedUnloadedToken?.Dispose();
					_loadedUnloadedToken = null;
					_loadedUnloadedToken = this.OnUnloaded(SendUnloaded);
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
#if IOS || MACCATALYST
			else if (Window?.Handler?.PlatformView is not null &&
				Handler is null &&
				_loadedUnloadedFallbackView is not null &&
				_loadedUnloadedFallbackView.TryGetTarget(out var fallbackView))
			{
				if (fallbackView.IsLoaded())
				{
					_loadedUnloadedToken = fallbackView.OnUnloaded(
						_isLoadedFired ? SendUnloaded : HandlePlatformUnloadedLoaded);
				}
				else
				{
					SendUnloaded(false);
					_loadedUnloadedToken = fallbackView.OnLoaded(SendLoaded);
				}
			}
#endif
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
	}
}

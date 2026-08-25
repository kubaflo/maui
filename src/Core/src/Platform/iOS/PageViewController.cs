using System;
using CoreGraphics;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public class PageViewController : ContainerViewController
	{
		public PageViewController(IView page, IMauiContext mauiContext)
		{
			CurrentView = page;
			Context = mauiContext;

			LoadFirstView(page);
		}

		protected override UIView CreatePlatformView(IElement view)
		{
			return new PageContentView
			{
				CrossPlatformLayout = (IContentView)view
			};
		}

		public override bool PrefersHomeIndicatorAutoHidden
			=> CurrentView is IiOSPageSpecifics pageSpecifics && pageSpecifics.IsHomeIndicatorAutoHidden;

		public override bool PrefersStatusBarHidden()
		{
			if (CurrentView is IiOSPageSpecifics pageSpecifics)
			{
				return pageSpecifics.PrefersStatusBarHiddenMode switch
				{
					1 => true,
					2 => false,
					_ => base.PrefersStatusBarHidden(),
				};
			}

			return base.PrefersStatusBarHidden();
		}

		public override UIStatusBarAnimation PreferredStatusBarUpdateAnimation
		{
			get
			{
				if (CurrentView is IiOSPageSpecifics pageSpecifics)
				{
					return pageSpecifics.PreferredStatusBarUpdateAnimationMode switch
					{
						0 => UIStatusBarAnimation.Fade,
						1 => UIStatusBarAnimation.Slide,
						_ => UIStatusBarAnimation.None,
					};
				}
				return base.PreferredStatusBarUpdateAnimation;
			}
		}

		public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
		{
			if (CurrentView?.Handler is ElementHandler handler)
			{
				// Check if the window is being destroyed by verifying its handler is still connected.
				// Window.Destroying() calls Handler?.DisconnectHandler() before DisposeWindowScope(),
				// so checking window.Handler == null tells us if we're in the teardown phase.
				var window = handler.MauiContext?.GetPlatformWindow()?.GetWindow();
				if (window?.Handler == null)
				{
					// Window is being destroyed, skip theme update to avoid accessing disposed services
					return;
				}

				try
				{
					var application = handler.GetRequiredService<IApplication>();
					application.UpdateUserInterfaceStyle();
					application.ThemeChanged();

					// When the preferred content size category changes (Dynamic Type),
					// re-apply fonts to all text elements so they reflect the new
					// scaling immediately without an app restart. The font cache is
					// cleared via ObserveContentSizeCategoryChanged in FontManager.
					if (previousTraitCollection is not null &&
						previousTraitCollection.PreferredContentSizeCategory != TraitCollection.PreferredContentSizeCategory)
					{
						InvalidateFontsOnContentSizeChanged(CurrentView as IView);
					}
				}
				catch (ObjectDisposedException)
				{
					// Extra safety net in case we hit a race condition where the service provider
					// is disposed between our check and the actual service access.
				}
			}

#pragma warning disable CA1422 // Validate platform compatibility
			base.TraitCollectionDidChange(previousTraitCollection);
#pragma warning restore CA1422 // Validate platform compatibility
		}

		static void InvalidateFontsOnContentSizeChanged(IView? view)
		{
			if (view is null)
			{
				return;
			}

			if (view is ITextStyle { Font.AutoScalingEnabled: true } && view.Handler is not null)
			{
				view.Handler.UpdateValue(nameof(ITextStyle.Font));
				view.InvalidateMeasure();
			}

			if (view is IVisualTreeElement vte)
			{
				foreach (var child in vte.GetVisualChildren())
				{
					if (child is IView childView)
					{
						InvalidateFontsOnContentSizeChanged(childView);
					}
				}
			}
		}

		// Starting with iOS 26 the tab bar floats over the page instead of sitting below it: the page
		// keeps the full window height and the strip the bar occludes is reported as a bottom safe area
		// inset. The page content is arranged inside the page bounds, so by default it stops at the top
		// of that strip and the bar's glass material samples the empty window background behind it,
		// which reads as an opaque bar. Stretching the arranged content down across the occluded strip
		// puts real page content behind the bar. Content that already reaches that far - for example a
		// layout that opts in manually with a negative bottom margin - is left exactly where it is, so
		// the adjustment never stacks on top of an explicit extension.
		sealed class PageContentView : ContentView
		{
			public override void LayoutSubviews()
			{
				base.LayoutSubviews();

				ExtendContentBehindBottomBar();
			}

			void ExtendContentBehindBottomBar()
			{
				if (!OperatingSystem.IsIOSVersionAtLeast(26))
				{
					return;
				}

				var overlap = GetBottomBarOverlap();
				if (overlap <= 0)
				{
					return;
				}

				var targetBottom = Bounds.Bottom + overlap;

				foreach (var subview in Subviews)
				{
					var frame = subview.Frame;

					if (frame.IsEmpty || frame.Bottom >= targetBottom - 0.5)
					{
						continue;
					}

					subview.Frame = new CGRect(frame.X, frame.Y, frame.Width, targetBottom - frame.Y);
				}
			}

			// The height of the strip at the bottom of this page that a floating tab bar covers, or zero
			// when the page isn't hosted in a tab bar.
			nfloat GetBottomBarOverlap()
			{
				UITabBar? tabBar = null;

				for (UIResponder? responder = this; responder is not null; responder = responder.NextResponder)
				{
					if (responder is UITabBarController tabBarController)
					{
						tabBar = tabBarController.TabBar;
						break;
					}
				}

				if (tabBar is null || tabBar.Hidden || tabBar.Superview is null)
				{
					return 0;
				}

				var inset = SafeAreaInsets.Bottom;
				if (inset > 0)
				{
					return inset;
				}

				// The bar isn't part of the safe area (it can be laid out by a custom container), so fall
				// back to how far it reaches into this view.
				var barTop = ConvertRectFromView(tabBar.Frame, tabBar.Superview).Top;
				var geometricOverlap = Bounds.Bottom - barTop;

				return geometricOverlap > 0 ? geometricOverlap : 0;
			}
		}
	}
}


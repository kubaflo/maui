#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui.Controls
{
	public partial class TabbedPage
	{
		MauiTabBarController _tabBarController;

		static UIView OnCreatePlatformView(ViewHandler<ITabbedView, UIView> handler)
		{
			if (handler.VirtualView is TabbedPage tabbedPage)
				return tabbedPage.CreatePlatformView(handler);

			return null;
		}

		UIView CreatePlatformView(ViewHandler<ITabbedView, UIView> handler)
		{
			var mauiContext = handler.MauiContext ??
				throw new InvalidOperationException("MauiContext cannot be null here");

			// A handler can be recycled into a different window/context; a cached controller
			// holds view controllers built from the old context, so it cannot be reused.
			if (_tabBarController is not null && _tabBarController.MauiContext != mauiContext)
				DisconnectTabBarController();

			_tabBarController ??= new MauiTabBarController(this, mauiContext);
			handler.ViewController = _tabBarController;

			return _tabBarController.View;
		}

		partial void OnHandlerChangingPartial(HandlerChangingEventArgs args)
		{
			if (args.NewHandler is null)
				DisconnectTabBarController();
		}

		void DisconnectTabBarController()
		{
			_tabBarController?.Disconnect();
			_tabBarController = null;
		}

		internal static void MapBarBackground(ITabbedViewHandler handler, TabbedPage view)
		{
		}
		internal static void MapBarBackgroundColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view._tabBarController?.UpdateBarBackgroundColor();
		}
		internal static void MapBarTextColor(ITabbedViewHandler handler, TabbedPage view)
		{
		}
		internal static void MapUnselectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view._tabBarController?.UpdateTabColors();
		}
		internal static void MapSelectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view._tabBarController?.UpdateTabColors();
		}

		internal static void MapItemsSource(ITabbedViewHandler handler, TabbedPage view)
		{
		}
		internal static void MapItemTemplate(ITabbedViewHandler handler, TabbedPage view)
		{
		}
		internal static void MapSelectedItem(ITabbedViewHandler handler, TabbedPage view)
		{
		}
		internal static void MapCurrentPage(ITabbedViewHandler handler, TabbedPage view)
		{
			view._tabBarController?.UpdateCurrentPage();
		}

		internal static void MapPrefersHomeIndicatorAutoHiddenProperty(ITabbedViewHandler handler, TabbedPage view)
		{
			view.CurrentPage?.Handler?.UpdateValue(nameof(PlatformConfiguration.iOSSpecific.Page.PrefersHomeIndicatorAutoHiddenProperty));
		}

		internal static void MapPrefersPrefersStatusBarHiddenProperty(ITabbedViewHandler handler, TabbedPage view)
		{
			view.CurrentPage?.Handler?.UpdateValue(nameof(PlatformConfiguration.iOSSpecific.Page.PrefersStatusBarHiddenProperty));
		}

		sealed class MauiTabBarController : UITabBarController
		{
			TabbedPage _tabbedPage;
			bool _updatingSelectedIndex;

			public MauiTabBarController(TabbedPage tabbedPage, IMauiContext mauiContext)
			{
				_tabbedPage = tabbedPage;
				MauiContext = mauiContext;

				_tabbedPage.PagesChanged += OnPagesChanged;
				ViewControllerSelected += OnViewControllerSelected;
			}

			public IMauiContext MauiContext { get; }

			public override void ViewDidLoad()
			{
				base.ViewDidLoad();
				UpdateChildren();
			}

			public void UpdateChildren()
			{
				if (_tabbedPage is null || !IsViewLoaded)
					return;

				var children = _tabbedPage.Children;
				var controllers = new UIViewController[children.Count];

				for (int i = 0; i < children.Count; i++)
				{
					var page = children[i];
					var viewController = page.ToUIViewController(MauiContext);
					viewController.TabBarItem.Title = page.Title ?? string.Empty;
					controllers[i] = viewController;
				}

				ViewControllers = controllers;

				UpdateCurrentPage();
				UpdateBarBackgroundColor();
				UpdateTabColors();
			}

			public void UpdateCurrentPage()
			{
				if (_tabbedPage is null || !IsViewLoaded)
					return;

				var currentPage = _tabbedPage.CurrentPage;
				if (currentPage is null)
					return;

				var index = _tabbedPage.Children.IndexOf(currentPage);
				var viewControllers = ViewControllers;

				if (index < 0 || viewControllers is null || index >= viewControllers.Length)
					return;

				if (SelectedIndex == index)
					return;

				_updatingSelectedIndex = true;

				try
				{
					SelectedIndex = index;
				}
				finally
				{
					_updatingSelectedIndex = false;
				}
			}

			public void UpdateBarBackgroundColor()
			{
				if (_tabbedPage is null || !IsViewLoaded)
					return;

				TabBar.BarTintColor = _tabbedPage.BarBackgroundColor?.ToPlatform() ?? UITabBar.Appearance.BarTintColor;
			}

			public void UpdateTabColors()
			{
				if (_tabbedPage is null || !IsViewLoaded)
					return;

				TabBar.TintColor = _tabbedPage.SelectedTabColor?.ToPlatform() ?? UITabBar.Appearance.TintColor;
				TabBar.UnselectedItemTintColor = _tabbedPage.UnselectedTabColor?.ToPlatform() ?? UITabBar.Appearance.UnselectedItemTintColor;
			}

			public void Disconnect()
			{
				if (_tabbedPage is not null)
					_tabbedPage.PagesChanged -= OnPagesChanged;

				ViewControllerSelected -= OnViewControllerSelected;
				_tabbedPage = null;
			}

			void OnPagesChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateChildren();

			void OnViewControllerSelected(object sender, UITabBarSelectionEventArgs e)
			{
				if (_updatingSelectedIndex || _tabbedPage is null)
					return;

				var index = (int)SelectedIndex;
				var children = _tabbedPage.Children;

				if (index >= 0 && index < children.Count)
					_tabbedPage.CurrentPage = children[index];
			}
		}
	}
}

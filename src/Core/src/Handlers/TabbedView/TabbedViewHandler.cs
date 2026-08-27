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
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID && !TIZEN)
using PlatformView = System.Object;
#endif

namespace Microsoft.Maui.Handlers
{
	public partial class TabbedViewHandler : ViewHandler<ITabbedView, PlatformView>, ITabbedViewHandler
	{
		public static IPropertyMapper<ITabbedView, ITabbedViewHandler> Mapper = new PropertyMapper<ITabbedView, ITabbedViewHandler>(ViewHandler.ViewMapper);

		public static CommandMapper<ITabbedView, ITabbedViewHandler> CommandMapper = new(ViewCommandMapper);

		public TabbedViewHandler() : base(Mapper, CommandMapper)
		{
		}

		public TabbedViewHandler(IPropertyMapper? mapper)
			: base(mapper ?? Mapper, CommandMapper)
		{
		}

		public TabbedViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
			: base(mapper ?? Mapper, commandMapper ?? CommandMapper)
		{
		}

#if IOS || MACCATALYST
		protected override PlatformView CreatePlatformView()
		{
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set to create a {nameof(PlatformView)}");

			// The tabbed view is page-like on iOS, so it needs a UIViewController that navigation
			// containers (NavigationPage, Window, FlyoutPage) can push, present, or parent.
			var viewController = ViewController ??= new UIKit.UITabBarController();

			return viewController.View ?? throw new InvalidOperationException($"{nameof(ViewController)}.View cannot be null");
		}

		protected override void ConnectHandler(PlatformView platformView)
		{
			base.ConnectHandler(platformView);
			UpdateChildViewControllers();
		}

		// The child pages own the content of each tab, so their handlers have to exist before the
		// property mappers run; several mappers reach through to the current page's handler.
		void UpdateChildViewControllers()
		{
			if (ViewController is not UIKit.UITabBarController tabBarController || MauiContext is null)
				return;

			var children = (VirtualView as IVisualTreeElement)?.GetVisualChildren();
			if (children is null || children.Count == 0)
				return;

			var viewControllers = new List<UIKit.UIViewController>(children.Count);

			for (int i = 0; i < children.Count; i++)
			{
				if (children[i] is IElement child)
					viewControllers.Add(Platform.ElementExtensions.ToUIViewController(child, MauiContext));
			}

			tabBarController.ViewControllers = viewControllers.ToArray();
		}
#else
		protected override PlatformView CreatePlatformView()
		{
			throw new NotImplementedException();
		}
#endif
	}
}

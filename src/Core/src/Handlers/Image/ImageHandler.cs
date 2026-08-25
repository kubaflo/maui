using System;
using System.Threading;
#if __IOS__ || MACCATALYST
using PlatformView = UIKit.UIImageView;
#elif MONOANDROID
using PlatformView = Android.Widget.ImageView;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.Image;
#elif TIZEN
using PlatformView = Tizen.UIExtensions.NUI.Image;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID && !TIZEN)
using PlatformView = System.Object;
#endif

namespace Microsoft.Maui.Handlers
{
	public partial class ImageHandler : IImageHandler
	{
		public static IPropertyMapper<IImage, IImageHandler> Mapper = new PropertyMapper<IImage, IImageHandler>(ViewHandler.ViewMapper)
		{
#if __IOS__ || MACCATALYST
			[nameof(IImage.Background)] = MapImageBackground,
#elif __ANDROID__ || WINDOWS || TIZEN
			[nameof(IImage.Background)] = MapBackground,
#endif
#if WINDOWS
			[nameof(IImage.Height)] = MapHeight,
			[nameof(IImage.Width)] = MapWidth,
#endif
			[nameof(IImage.Aspect)] = MapAspect,
			[nameof(IImage.IsAnimationPlaying)] = MapIsAnimationPlaying,
			[nameof(IImage.Source)] = MapSource,
		};

		public static CommandMapper<IImage, IImageHandler> CommandMapper = new(ViewHandler.ViewCommandMapper)
		{
		};

		ImageSourcePartLoader? _imageSourcePartLoader;

#if __IOS__ || MACCATALYST
		bool _handlerAppliedPlatformBackground;
#endif

		public virtual ImageSourcePartLoader SourceLoader =>
			_imageSourcePartLoader ??= new ImageSourcePartLoader(new ImageImageSourcePartSetter(this));

		public ImageHandler() : base(Mapper, CommandMapper)
		{
		}

		public ImageHandler(IPropertyMapper? mapper)
			: base(mapper ?? Mapper, CommandMapper)
		{
		}

		public ImageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
			: base(mapper ?? Mapper, commandMapper ?? CommandMapper)
		{
		}


#if __IOS__ || MACCATALYST
		// UIImageView is not one of the platform view types that the shared iOS background
		// updater resets, so an empty paint leaves whatever color was applied last on screen.
		// The handler therefore tracks whether it applied the native background itself and only
		// releases the background it owns; a background MAUI never set is left untouched.
		static void MapImageBackground(IImageHandler handler, IImage image)
		{
			if (handler.PlatformView is not PlatformView platformView)
				return;

			if (!Graphics.PaintExtensions.IsNullOrEmpty(image.Background))
			{
				ViewHandler.MapBackground(handler, image);

				if (handler is ImageHandler imageHandler)
					imageHandler._handlerAppliedPlatformBackground = true;

				return;
			}

			if (handler is not ImageHandler owner || !owner._handlerAppliedPlatformBackground)
				return;

			owner._handlerAppliedPlatformBackground = false;

			Platform.LayerExtensions.RemoveBackgroundLayer(platformView);
			platformView.BackgroundColor = null;
		}
#endif

		// TODO MAUI: Should we remove all shadowing? 
		IImage IImageHandler.VirtualView => VirtualView;

		PlatformView IImageHandler.PlatformView => PlatformView;

		partial class ImageImageSourcePartSetter : ImageSourcePartSetter<IImageHandler>
		{
			public ImageImageSourcePartSetter(IImageHandler handler)
				: base(handler)
			{
			}
		}
	}
}

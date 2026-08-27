#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WStretch = Microsoft.UI.Xaml.Media.Stretch;
using RootSystem = System;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;

namespace Microsoft.Maui.DeviceTests
{
	using IO = RootSystem.IO;

	[Category("Issue26094")]
	public class Issue26094 : ControlsHandlerTestBase
	{
		const int SourceSize = 44;
		const double SizeTolerance = 1;

		[Fact]
		public async Task ImageInAbsoluteLayoutRendersAtIntrinsicSize()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var calibrationImage = new Image
			{
				Aspect = Aspect.AspectFill,
				Source = ImageSource.FromStream(() => new IO.MemoryStream(CreateBitmapBytes(), writable: false)),
				WidthRequest = SourceSize,
				HeightRequest = SourceSize,
			};

			var calibrationSizeChanged = false;
			var calibrationWidth = -1d;
			var calibrationHeight = -1d;
			WImage calibrationNativeImage = null;

			calibrationImage.HandlerChanged += (_, _) =>
			{
				if (calibrationImage.Handler is ImageHandler imageHandler)
				{
					calibrationNativeImage = imageHandler.PlatformView;
					calibrationNativeImage.SizeChanged += (_, args) =>
					{
						calibrationSizeChanged = true;
						calibrationWidth = args.NewSize.Width;
						calibrationHeight = args.NewSize.Height;
					};
				}
			};

			await CreateHandlerAndAddToWindow(calibrationImage, async () =>
			{
				await calibrationImage.WaitUntilLoaded();
				await AssertEventually(() =>
					calibrationNativeImage?.Source is WBitmapImage bitmap &&
					bitmap.PixelWidth == SourceSize &&
					bitmap.PixelHeight == SourceSize);
				await AssertEventually(() => calibrationSizeChanged && calibrationWidth > 0 && calibrationHeight > 0);

				Assert.NotNull(calibrationNativeImage);
				Assert.True(calibrationSizeChanged);
				var calibrationSource = Assert.IsType<WBitmapImage>(calibrationNativeImage.Source);
				Assert.Equal(SourceSize, calibrationSource.PixelWidth);
				Assert.Equal(SourceSize, calibrationSource.PixelHeight);
				Assert.Equal(SourceSize, calibrationNativeImage.ActualWidth, SizeTolerance);
				Assert.Equal(SourceSize, calibrationNativeImage.ActualHeight, SizeTolerance);
			});

			var affectedImage = new Image
			{
				Aspect = Aspect.AspectFill,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Source = ImageSource.FromStream(() => new IO.MemoryStream(CreateBitmapBytes(), writable: false)),
			};
			AbsoluteLayout.SetLayoutBounds(affectedImage, new Rect(0, 0, 1, 1));
			AbsoluteLayout.SetLayoutFlags(affectedImage, AbsoluteLayoutFlags.All);

			var absoluteLayout = new AbsoluteLayout
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Children = { affectedImage },
			};
			var stackLayout = new StackLayout
			{
				Children = { absoluteLayout },
			};
			var page = new ContentPage
			{
				Content = stackLayout,
			};

			var affectedSizeChanged = false;
			var affectedWidth = -1d;
			var affectedHeight = -1d;
			WImage affectedNativeImage = null;

			affectedImage.HandlerChanged += (_, _) =>
			{
				if (affectedImage.Handler is ImageHandler imageHandler)
				{
					affectedNativeImage = imageHandler.PlatformView;
					affectedNativeImage.SizeChanged += (_, args) =>
					{
						affectedSizeChanged = true;
						affectedWidth = args.NewSize.Width;
						affectedHeight = args.NewSize.Height;
					};
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await affectedImage.WaitUntilLoaded();
				await AssertEventually(() =>
					affectedNativeImage?.Source is WBitmapImage bitmap &&
					bitmap.PixelWidth == SourceSize &&
					bitmap.PixelHeight == SourceSize);
				await AssertEventually(() => affectedSizeChanged && affectedWidth > 0 && affectedHeight > 0);

				Assert.NotNull(affectedNativeImage);
				Assert.True(affectedSizeChanged);
				var affectedSource = Assert.IsType<WBitmapImage>(affectedNativeImage.Source);
				Assert.Equal(SourceSize, affectedSource.PixelWidth);
				Assert.Equal(SourceSize, affectedSource.PixelHeight);
				Assert.Equal(WStretch.UniformToFill, affectedNativeImage.Stretch);

				var nativeParent = Assert.IsAssignableFrom<WFrameworkElement>(affectedNativeImage.Parent);
				Assert.True(nativeParent.ActualWidth > calibrationWidth + SizeTolerance);
				Assert.True(nativeParent.ActualHeight > calibrationHeight + SizeTolerance);

				Assert.True(
					Math.Abs(affectedNativeImage.ActualWidth - calibrationWidth) <= SizeTolerance &&
					Math.Abs(affectedNativeImage.ActualHeight - calibrationHeight) <= SizeTolerance,
					$"Issue26094 affected image rendered at {affectedNativeImage.ActualWidth:F2} x {affectedNativeImage.ActualHeight:F2}, expected {calibrationWidth:F2} x {calibrationHeight:F2}.");
			});
		}

		static byte[] CreateBitmapBytes()
		{
			const int bytesPerPixel = 3;
			const int bitmapHeaderSize = 54;
			const int pixelBytes = SourceSize * SourceSize * bytesPerPixel;

			var bitmap = new byte[bitmapHeaderSize + pixelBytes];
			var offset = 0;

			WriteByte((byte)'B');
			WriteByte((byte)'M');
			WriteInt32(bitmap.Length);
			WriteInt32(0);
			WriteInt32(bitmapHeaderSize);
			WriteInt32(40);
			WriteInt32(SourceSize);
			WriteInt32(SourceSize);
			WriteInt16(1);
			WriteInt16(24);
			WriteInt32(0);
			WriteInt32(pixelBytes);
			WriteInt32(2835);
			WriteInt32(2835);
			WriteInt32(0);
			WriteInt32(0);

			for (var y = 0; y < SourceSize; y++)
			{
				for (var x = 0; x < SourceSize; x++)
				{
					var isDark = (x < SourceSize / 2) == (y < SourceSize / 2);
					WriteByte(isDark ? (byte)48 : (byte)224);
					WriteByte(isDark ? (byte)96 : (byte)184);
					WriteByte(isDark ? (byte)224 : (byte)48);
				}
			}

			return bitmap;

			void WriteByte(byte value) =>
				bitmap[offset++] = value;

			void WriteInt16(ushort value)
			{
				WriteByte((byte)value);
				WriteByte((byte)(value >> 8));
			}

			void WriteInt32(int value)
			{
				WriteByte((byte)value);
				WriteByte((byte)(value >> 8));
				WriteByte((byte)(value >> 16));
				WriteByte((byte)(value >> 24));
			}
		}
	}
}
#endif


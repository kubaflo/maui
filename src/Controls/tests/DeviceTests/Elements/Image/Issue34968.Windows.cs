using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;
using WPopup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using WWriteableBitmap = Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34968")]
	public class Issue34968 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task AspectFitConstrainsWriteableBitmapToItsIntrinsicSize()
		{
			var imageSourceService = await InvokeOnMainThreadAsync(() => new WriteableBitmapImageSourceService());

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureImageSources(imageSources =>
					imageSources.AddService<WriteableBitmapImageSource>(_ => imageSourceService));
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var targetImage = new Image
			{
				WidthRequest = 300,
				HeightRequest = 220,
				Aspect = Aspect.AspectFit,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
			var border = new Border
			{
				WidthRequest = 320,
				HeightRequest = 240,
				BackgroundColor = Colors.LightGray,
				Stroke = Colors.DarkGray,
				StrokeThickness = 2,
				Content = targetImage,
			};
			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				HorizontalOptions = LayoutOptions.Center,
				Children = { border },
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = stack,
				},
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(targetImage.Handler);
				var platformImage = Assert.IsType<WImage>(targetImage.Handler.PlatformView);
				var bitmap = imageSourceService.Bitmap;

				var referenceImage = new WImage
				{
					Source = bitmap,
					Width = bitmap.PixelWidth,
					Height = bitmap.PixelHeight,
				};
				var referenceArranged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				referenceImage.SizeChanged += (_, _) =>
				{
					if (referenceImage.ActualWidth > 0 && referenceImage.ActualHeight > 0)
						referenceArranged.TrySetResult(true);
				};
				var calibrationPopup = new WPopup
				{
					XamlRoot = platformImage.XamlRoot,
					Child = referenceImage,
				};

				calibrationPopup.IsOpen = true;
				await referenceArranged.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.InRange(Math.Abs(referenceImage.ActualWidth - bitmap.PixelWidth), 0, 1);
				Assert.InRange(Math.Abs(referenceImage.ActualHeight - bitmap.PixelHeight), 0, 1);
				calibrationPopup.IsOpen = false;

				var nativeSourceSet = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				var postTriggerLayout = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				double callbackWidth = -1;
				double callbackHeight = -1;
				long sourceCallback = platformImage.RegisterPropertyChangedCallback(WImage.SourceProperty, (_, _) =>
				{
					if (ReferenceEquals(platformImage.Source, bitmap))
						nativeSourceSet.TrySetResult(true);
				});
				platformImage.LayoutUpdated += OnLayoutUpdated;

				try
				{
					targetImage.Source = new WriteableBitmapImageSource();

					await imageSourceService.ResultRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
					await nativeSourceSet.Task.WaitAsync(TimeSpan.FromSeconds(5));
					await postTriggerLayout.Task.WaitAsync(TimeSpan.FromSeconds(5));

					Assert.Same(bitmap, platformImage.Source);
					Assert.Equal(120, bitmap.PixelWidth);
					Assert.Equal(80, bitmap.PixelHeight);
					Assert.True(
						callbackWidth <= bitmap.PixelWidth + 1 && callbackHeight <= bitmap.PixelHeight + 1,
						$"AspectFit WriteableBitmap rendered larger than its intrinsic size: rendered {callbackWidth:0.##} x {callbackHeight:0.##}, expected at most {bitmap.PixelWidth} x {bitmap.PixelHeight}.");
				}
				finally
				{
					platformImage.UnregisterPropertyChangedCallback(WImage.SourceProperty, sourceCallback);
					platformImage.LayoutUpdated -= OnLayoutUpdated;
				}

				void OnLayoutUpdated(object sender, object args)
				{
					if (!ReferenceEquals(platformImage.Source, bitmap))
						return;

					callbackWidth = platformImage.ActualWidth;
					callbackHeight = platformImage.ActualHeight;
					if (callbackWidth > 0 && callbackHeight > 0)
						postTriggerLayout.TrySetResult(true);
				}
			});
		}

		sealed class WriteableBitmapImageSource : ImageSource
		{
		}

		sealed class WriteableBitmapImageSourceService : IImageSourceService<WriteableBitmapImageSource>
		{
			public WWriteableBitmap Bitmap { get; } = CreateBitmap();

			public TaskCompletionSource<bool> ResultRequested { get; } =
				new(TaskCreationOptions.RunContinuationsAsynchronously);

			public Task<IImageSourceServiceResult<WImageSource>> GetImageSourceAsync(
				IImageSource imageSource,
				float scale = 1,
				CancellationToken cancellationToken = default)
			{
				ResultRequested.TrySetResult(true);
				return Task.FromResult<IImageSourceServiceResult<WImageSource>>(new ImageSourceServiceResult(Bitmap));
			}

			static WWriteableBitmap CreateBitmap()
			{
				var bitmap = new WWriteableBitmap(120, 80);
				bitmap.Invalidate();
				return bitmap;
			}
		}
	}
}


using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;
using Sys = System;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WImage = Microsoft.UI.Xaml.Controls.Image;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26094")]
	public class Issue26094 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ImageInAbsoluteLayoutRetainsItsIntrinsicSize()
		{
			const int imageSize = 44;
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var calibrationImage = new Image
			{
				WidthRequest = imageSize,
				HeightRequest = imageSize,
				Source = CreateBitmapSource()
			};
			var calibrationLayoutCompleted = ObserveNextNativeLayout(calibrationImage);

			await CreateHandlerAndAddToWindow(calibrationImage, async () =>
			{
				await calibrationImage.WaitUntilLoaded(5000);
				var calibrationNativeImage = await calibrationLayoutCompleted.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.InRange(calibrationNativeImage.ActualWidth, imageSize - tolerance, imageSize + tolerance);
				Assert.InRange(calibrationNativeImage.ActualHeight, imageSize - tolerance, imageSize + tolerance);
			});

			var imageSource = CreateBitmapSource();
			var affectedImage = new Image
			{
				Aspect = Aspect.AspectFill,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Source = imageSource
			};
			AbsoluteLayout.SetLayoutBounds(affectedImage, new Rect(0, 0, 1, 1));
			AbsoluteLayout.SetLayoutFlags(affectedImage, AbsoluteLayoutFlags.All);

			var imageLayout = new AbsoluteLayout
			{
				BackgroundColor = Colors.LightGray,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};
			imageLayout.Add(affectedImage);

			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(imageLayout);
			grid.Add(new Label { Text = "IMAGE PREVIEW", HorizontalTextAlignment = TextAlignment.Center }, 0, 1);
			grid.Add(new Label { Text = "Image dimensions", HorizontalTextAlignment = TextAlignment.Center }, 0, 2);
			grid.Add(new Button { Text = "INSPECT IMAGE" }, 0, 3);
			grid.Add(new Label { Text = "STATUS", HorizontalTextAlignment = TextAlignment.Center }, 0, 4);

			var page = new ContentPage { Content = grid };
			var affectedLayoutCompleted = ObserveNextNativeLayout(affectedImage);
			double observedWidth = -1;
			double observedHeight = -1;

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await affectedImage.WaitUntilLoaded(5000);
				var nativeImage = await affectedLayoutCompleted.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Same(imageSource, affectedImage.Source);
				Assert.Same(imageLayout, affectedImage.Parent);
				Assert.NotNull(nativeImage);

				var nativeLayout = imageLayout.Handler.PlatformView as WFrameworkElement;
				Assert.NotNull(nativeLayout);
				Assert.True(nativeLayout.ActualWidth > imageSize && nativeLayout.ActualHeight > imageSize);

				observedWidth = nativeImage.ActualWidth;
				observedHeight = nativeImage.ActualHeight;
			});

			Assert.True(
				Math.Abs(observedWidth - imageSize) <= tolerance &&
				Math.Abs(observedHeight - imageSize) <= tolerance,
				$"Issue26094 image native size should remain 44 x 44; actual size was {observedWidth} x {observedHeight}");

			static ImageSource CreateBitmapSource()
			{
				const int width = imageSize;
				const int height = imageSize;
				const int headerSize = 54;
				const int rowSize = width * 3;
				var bytes = new byte[headerSize + (rowSize * height)];

				bytes[0] = (byte)'B';
				bytes[1] = (byte)'M';
				BitConverter.GetBytes(bytes.Length).CopyTo(bytes, 2);
				BitConverter.GetBytes(headerSize).CopyTo(bytes, 10);
				BitConverter.GetBytes(40).CopyTo(bytes, 14);
				BitConverter.GetBytes(width).CopyTo(bytes, 18);
				BitConverter.GetBytes(height).CopyTo(bytes, 22);
				BitConverter.GetBytes((short)1).CopyTo(bytes, 26);
				BitConverter.GetBytes((short)24).CopyTo(bytes, 28);
				BitConverter.GetBytes(rowSize * height).CopyTo(bytes, 34);

				for (var offset = headerSize; offset < bytes.Length; offset += 3)
				{
					bytes[offset] = 139;
					bytes[offset + 1] = 69;
					bytes[offset + 2] = 19;
				}

				return ImageSource.FromStream(() => new Sys.IO.MemoryStream(bytes, writable: false));
			}

			static Task<WImage> ObserveNextNativeLayout(Image image)
			{
				var layoutCompleted = new TaskCompletionSource<WImage>(TaskCreationOptions.RunContinuationsAsynchronously);
				image.HandlerChanged += OnHandlerChanged;
				return layoutCompleted.Task;

				void OnHandlerChanged(object sender, EventArgs args)
				{
					if (image.Handler?.PlatformView is not WImage nativeImage)
						return;

					image.HandlerChanged -= OnHandlerChanged;
					nativeImage.LayoutUpdated += OnLayoutUpdated;

					void OnLayoutUpdated(object layoutSender, object layoutArgs)
					{
						if (nativeImage.ActualWidth <= 0 || nativeImage.ActualHeight <= 0)
							return;

						nativeImage.LayoutUpdated -= OnLayoutUpdated;
						layoutCompleted.TrySetResult(nativeImage);
					}
				}
			}
		}
	}
}


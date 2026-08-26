#if WINDOWS
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Handlers;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;
using WWriteableBitmap = Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34968")]
	public class Issue34968 : ControlsHandlerTestBase
	{
		const int SourceWidth = 120;
		const int SourceHeight = 80;

		[Fact]
		public async Task AspectFitConstrainsWriteableBitmapToItsIntrinsicSize()
		{
			var writeableBitmap = await InvokeOnMainThreadAsync(
				() => new WWriteableBitmap(SourceWidth, SourceHeight));
			var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var sourceService = new WriteableBitmapImageSourceService
			{
				Bitmap = writeableBitmap,
				Completed = completed
			};

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
				builder.ConfigureImageSources(services =>
					services.AddService<WriteableBitmapImageSource>(_ => sourceService));
			});

			var affectedImage = CreateImage();
			var affectedPage = CreatePage(affectedImage);
			var customSource = new WriteableBitmapImageSource();

			await CreateHandlerAndAddToWindow(affectedPage, async () =>
			{
				var affectedHandler = Assert.IsType<ImageHandler>(affectedImage.Handler);
				Assert.Same(affectedImage, affectedHandler.VirtualView);
				Assert.Equal(2, Grid.GetRow(affectedImage));
				Assert.Null(affectedHandler.PlatformView.Source);
				Assert.Equal(-1, sourceService.CompletionState);

				affectedImage.Source = customSource;

				await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.Equal(1, sourceService.CompletionState);
				Assert.Same(customSource, sourceService.LastSource);
				await affectedImage.WaitUntilLoaded(5000);
				await AssertEventually(
					() => ReferenceEquals(affectedHandler.PlatformView.Source, writeableBitmap),
					timeout: 5000,
					message: "Issue34968 setup failed: the custom image source was not applied to the native Image.");
				Assert.Equal(SourceWidth, writeableBitmap.PixelWidth);
				Assert.Equal(SourceHeight, writeableBitmap.PixelHeight);
				await AssertEventually(
					() => affectedHandler.PlatformView.ActualWidth > 0 && affectedHandler.PlatformView.ActualHeight > 0,
					timeout: 5000,
					message: "Issue34968 setup failed: the native Image did not complete layout.");

				var actualWidth = affectedHandler.PlatformView.ActualWidth;
				var actualHeight = affectedHandler.PlatformView.ActualHeight;
				Assert.True(
					IsExpectedSize(actualWidth, actualHeight),
					$"Issue34968: AspectFit WriteableBitmap native size exceeded its source; measured {actualWidth:0.##} x {actualHeight:0.##}, source {SourceWidth} x {SourceHeight}.");
			});
		}

		static Image CreateImage() =>
			new Image
			{
				Aspect = Aspect.AspectFit,
				WidthRequest = 320,
				HeightRequest = 240,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

		static ContentPage CreatePage(Image image)
		{
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(320),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			var heading = new Label
			{
				Text = "Issue 34968 WriteableBitmap AspectFit",
				FontSize = 24,
				HorizontalOptions = LayoutOptions.Center
			};
			var description = new Label
			{
				Text = "The source is 120 x 80. The centered Image requests 320 x 240.",
				HorizontalOptions = LayoutOptions.Center
			};
			var trigger = new Button
			{
				Text = "Load WriteableBitmap",
				HorizontalOptions = LayoutOptions.Center
			};
			var status = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					new Label { Text = "Measured image", HorizontalOptions = LayoutOptions.Center },
					new Label { Text = "Result", HorizontalOptions = LayoutOptions.Center }
				}
			};

			Grid.SetRow(heading, 0);
			Grid.SetRow(description, 1);
			Grid.SetRow(image, 2);
			Grid.SetRow(trigger, 3);
			Grid.SetRow(status, 4);
			grid.Add(heading);
			grid.Add(description);
			grid.Add(image);
			grid.Add(trigger);
			grid.Add(status);

			return new ContentPage { Content = grid };
		}

		static bool IsExpectedSize(double width, double height) =>
			Math.Abs(width - SourceWidth) <= 0.5 &&
			Math.Abs(height - SourceHeight) <= 0.5;

		sealed class WriteableBitmapImageSource : ImageSource
		{
		}

		sealed class WriteableBitmapImageSourceService : ImageSourceService, IImageSourceService<WriteableBitmapImageSource>
		{
			int _completionState = -1;

			public WWriteableBitmap Bitmap;

			public TaskCompletionSource<bool> Completed;

			public int CompletionState => Volatile.Read(ref _completionState);

			public IImageSource LastSource { get; private set; }

			public override Task<IImageSourceServiceResult<WImageSource>> GetImageSourceAsync(
				IImageSource imageSource,
				float scale = 1,
				CancellationToken cancellationToken = default)
			{
				LastSource = imageSource;
				Interlocked.Exchange(ref _completionState, 1);
				Completed.TrySetResult(true);
				return Task.FromResult<IImageSourceServiceResult<WImageSource>>(new ImageSourceServiceResult(Bitmap));
			}
		}
	}
}
#endif


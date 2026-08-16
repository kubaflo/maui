using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Collection(RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	public class Issue34538 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DelayedImageCompletionDoesNotUpdateRecycledCell()
		{
			var imageService = new DelayedImageSourceService
			{
				PendingRequests = new Queue<TaskCompletionSource<IImageSourceServiceResult<UIImage>>>()
			};

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
					handlers.AddHandler<Image, ImageHandler>();
				});
				builder.ConfigureImageSources(sources =>
					sources.AddService<DelayedImageSource>(_ => imageService));
			});

			var loadingItems = new Dictionary<Image, int>();
			var staleCompletionDetected = false;
			var loadFinishedCount = 0;

			bool ReleaseNextImage()
			{
				while (imageService.PendingRequests.Count > 0)
				{
					var completion = imageService.PendingRequests.Dequeue();
					var image = UIImage.GetSystemImage("photo");
					if (image is not null &&
						completion.TrySetResult(new ImageSourceServiceResult(image)))
						return true;
				}

				return false;
			}

			var items = Enumerable.Range(1, 60)
				.Select(id => new DelayedImageSource { Id = id })
				.ToArray();
			var collectionView = new CollectionView
			{
				HeightRequest = 500,
				WidthRequest = 320,
				ItemsSource = items,
				ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical)
				{
					Span = 2,
					HorizontalItemSpacing = 8,
					VerticalItemSpacing = 8
				},
				ItemTemplate = new DataTemplate(() =>
				{
					var image = new Image
					{
						HeightRequest = 150,
						Aspect = Aspect.AspectFill
					};
					image.SetBinding(Image.SourceProperty, ".");
					image.PropertyChanged += (_, args) =>
					{
						if (args.PropertyName != Image.IsLoadingProperty.PropertyName)
							return;

						if (image.IsLoading)
						{
							if (image.BindingContext is DelayedImageSource loadingItem)
								loadingItems[image] = loadingItem.Id;

							return;
						}

						if (loadingItems.Remove(image, out int loadedItemId))
						{
							loadFinishedCount++;

							if (image.BindingContext is DelayedImageSource currentItem && currentItem.Id != loadedItemId)
								staleCompletionDetected = true;
						}
					};
					return image;
				})
			};

			await CreateHandlerAndAddToWindow<CollectionViewHandler2>(collectionView, async handler =>
			{
				UICollectionView nativeCollectionView = handler.Controller.CollectionView;

				await AssertEventually(() =>
					nativeCollectionView.IndexPathsForVisibleItems.Length > 0 &&
					imageService.PendingRequests.Count > 0);

				nativeCollectionView.ScrollToItem(
					NSIndexPath.FromItemSection(items.Length - 1, 0),
					UICollectionViewScrollPosition.Bottom,
					animated: false);
				nativeCollectionView.LayoutIfNeeded();

				await AssertEventually(() =>
					nativeCollectionView.IndexPathsForVisibleItems.Any(indexPath => indexPath.Item > 40) &&
					imageService.PendingRequests.Count > 0);

				for (int targetIndex = 0; targetIndex < 8; targetIndex++)
				{
					int previousLoadFinishedCount = loadFinishedCount;
					Assert.True(ReleaseNextImage(), "A delayed image should be pending.");
					await AssertEventually(() => loadFinishedCount > previousLoadFinishedCount);

					if (staleCompletionDetected)
						break;

					int itemIndex = targetIndex % 2 == 0 ? 0 : items.Length - 1;
					UICollectionViewScrollPosition position = itemIndex == 0
						? UICollectionViewScrollPosition.Top
						: UICollectionViewScrollPosition.Bottom;
					nativeCollectionView.ScrollToItem(
						NSIndexPath.FromItemSection(itemIndex, 0),
						position,
						animated: false);
					nativeCollectionView.LayoutIfNeeded();

					await AssertEventually(() =>
						nativeCollectionView.IndexPathsForVisibleItems.Any(indexPath =>
							itemIndex == 0 ? indexPath.Item < 20 : indexPath.Item > 40) &&
						imageService.PendingRequests.Count > 0);
				}

				Assert.False(
					staleCompletionDetected,
					"Delayed image completion must not update a recycled cell.");
			});
		}

		sealed class DelayedImageSource : ImageSource
		{
			public int Id { get; set; }
		}

		sealed class DelayedImageSourceService : IImageSourceService<DelayedImageSource>
		{
			public Queue<TaskCompletionSource<IImageSourceServiceResult<UIImage>>> PendingRequests { get; set; }

			public Task<IImageSourceServiceResult<UIImage>> GetImageAsync(
				IImageSource imageSource,
				float scale = 1,
				CancellationToken cancellationToken = default) =>
				GetImageAsync((DelayedImageSource)imageSource, scale, cancellationToken);

			public Task<IImageSourceServiceResult<UIImage>> GetImageAsync(
				DelayedImageSource imageSource,
				float scale = 1,
				CancellationToken cancellationToken = default)
			{
				var completion =
					new TaskCompletionSource<IImageSourceServiceResult<UIImage>>(
						TaskCreationOptions.RunContinuationsAsynchronously);
				PendingRequests.Enqueue(completion);
				return completion.Task;
			}
		}
	}
#endif
}

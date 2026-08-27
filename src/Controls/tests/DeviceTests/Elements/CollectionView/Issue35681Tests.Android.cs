#if ANDROID
using System;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue35681")]
	public class Issue35681 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SupplementaryViewsAreExcludedFromAccessibilityRowCount()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var emptyCollectionView = new CollectionView();
			await CreateHandlerAndAddToWindow<CollectionViewHandler>(emptyCollectionView, async handler =>
			{
				var recyclerView = Assert.IsAssignableFrom<RecyclerView>(handler.PlatformView);
				await AssertEventually(() => recyclerView.IsAttachedToWindow && recyclerView.Width > 0 && recyclerView.Height > 0);
				await AssertEventually(() => recyclerView.GetAdapter()?.ItemCount == 0);

				var emptyRowCount = await GetAccessibilityRowCountAsync(recyclerView);
				Assert.Equal(0, emptyRowCount);
			});

			var header = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "Collection Title", FontSize = 24 },
					new Label { Text = "Collection Subtitle" },
				}
			};
			var emptyView = new Grid
			{
				Children =
				{
					new Label
					{
						Text = "There is nothing to see here!",
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center,
					}
				}
			};
			var footer = new Label { Text = "Footer" };
			var reportedCollectionView = new CollectionView
			{
				Header = header,
				EmptyView = emptyView,
				Footer = footer,
			};
			var content = new Grid
			{
				Padding = 12,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
				}
			};
			content.Add(reportedCollectionView, 0, 1);
			var page = new ContentPage { Content = content };

			await CreateHandlerAndAddToWindow<PageHandler>(page, async handler =>
			{
				Assert.NotNull(handler.PlatformView);
				Assert.NotNull(reportedCollectionView.Handler);
				var recyclerView = Assert.IsAssignableFrom<RecyclerView>(reportedCollectionView.Handler.PlatformView);

				await AssertEventually(() => recyclerView.IsAttachedToWindow);
				await AssertEventually(() => recyclerView.Width > 0 && recyclerView.Height > 0);
				await AssertEventually(() =>
					recyclerView.GetAdapter() is EmptyViewAdapter { ItemCount: 3 } &&
					header.IsLoaded &&
					emptyView.IsLoaded &&
					footer.IsLoaded);

				Assert.Null(reportedCollectionView.ItemsSource);
				const int expectedRowCount = 0;
				var observedRowCount = await GetAccessibilityRowCountAsync(recyclerView);

				Assert.True(
					observedRowCount == expectedRowCount,
					$"CollectionView accessibility row count included non-item views: expected {expectedRowCount}, observed {observedRowCount}.");
			});
		}

		static async Task<int> GetAccessibilityRowCountAsync(RecyclerView recyclerView)
		{
			var observedRowCount = -1;
			var callbackOccurred = false;
			var collectionInfoReturned = false;
			var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			recyclerView.Post(() =>
			{
				using var nodeInfo = recyclerView.CreateAccessibilityNodeInfo();
				using var collectionInfo = nodeInfo?.GetCollectionInfo();
				if (collectionInfo is not null)
				{
					observedRowCount = collectionInfo.RowCount;
					collectionInfoReturned = true;
				}

				callbackOccurred = true;
				completion.SetResult();
			});

			await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.True(callbackOccurred, "The accessibility-node query callback did not run.");
			Assert.True(collectionInfoReturned, "The accessibility node did not return collection metadata.");
			Assert.NotEqual(-1, observedRowCount);
			return observedRowCount;
		}
	}
}
#endif


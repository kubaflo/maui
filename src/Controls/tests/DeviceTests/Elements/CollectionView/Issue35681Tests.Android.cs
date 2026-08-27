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
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue35681")]
	public class Issue35681 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionAccessibilityCountExcludesSupplementaryViews()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var headerContent = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto),
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};
			headerContent.Add(new Label { Text = "Collection Title", FontSize = 24 }, 0, 0);
			headerContent.Add(new Label { Text = "Collection Subtitle" }, 0, 1);
			headerContent.Add(new Label { Text = "Header", FontSize = 20 }, 1, 0);

			var header = new Grid { Padding = 12 };
			header.Add(headerContent);

			var emptyLabel = new Label
			{
				Text = "There is nothing to see here!",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
			var emptyView = new Grid { Padding = 12 };
			emptyView.Add(emptyLabel);

			var footer = new Grid { Padding = 12 };
			footer.Add(new Label { Text = "Footer" });

			var collectionView = new CollectionView
			{
				ItemsSource = null,
				Header = header,
				EmptyView = emptyView,
				Footer = footer,
			};

			var pageLayout = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
				},
			};
			pageLayout.Add(new Label { Text = "CollectionView TalkBack count", FontSize = 24 }, 0, 0);
			pageLayout.Add(
				new Label
				{
					Text = "With TalkBack enabled, touch Collection Title. The header, empty view, and footer should not be announced as list items.",
				},
				0,
				1);
			pageLayout.Add(new Label { Text = "Collection accessibility information", FontAttributes = FontAttributes.Bold }, 0, 2);
			pageLayout.Add(collectionView, 0, 3);
			pageLayout.Add(new Button { Text = "Continue" }, 0, 4);

			var page = new ContentPage { Content = pageLayout };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var recyclerView = Assert.IsAssignableFrom<RecyclerView>(collectionViewHandler.PlatformView);
				int adapterCount = -1;

				await AssertEventually(() =>
				{
					if (recyclerView.GetAdapter() is not EmptyViewAdapter adapter)
						return false;

					adapterCount = adapter.ItemCount;
					return adapterCount == 3 &&
						IsAttachedDescendant(header, recyclerView) &&
						IsAttachedDescendant(emptyView, recyclerView) &&
						IsAttachedDescendant(footer, recyclerView);
				});

				Assert.Same(recyclerView, collectionViewHandler.PlatformView);
				Assert.IsType<EmptyViewAdapter>(recyclerView.GetAdapter());
				Assert.Equal(3, adapterCount);
				Assert.Null(collectionView.ItemsSource);

				Assert.NotNull(header.Handler);
				Assert.NotNull(emptyView.Handler);
				Assert.NotNull(footer.Handler);
				var headerPlatformView = Assert.IsAssignableFrom<AView>(header.Handler.PlatformView);
				var emptyPlatformView = Assert.IsAssignableFrom<AView>(emptyView.Handler.PlatformView);
				var footerPlatformView = Assert.IsAssignableFrom<AView>(footer.Handler.PlatformView);
				Assert.True(IsAttachedDescendant(headerPlatformView, recyclerView));
				Assert.True(IsAttachedDescendant(emptyPlatformView, recyclerView));
				Assert.True(IsAttachedDescendant(footerPlatformView, recyclerView));

				using var nodeInfo = recyclerView.CreateAccessibilityNodeInfo();
				Assert.NotNull(nodeInfo);
				var collectionInfo = nodeInfo.GetCollectionInfo();
				Assert.NotNull(collectionInfo);

				int expectedDataItemCount = 0;
				int measuredItemCount = collectionInfo.RowCount;
				Assert.True(
					measuredItemCount == expectedDataItemCount,
					$"CollectionView accessibility count included supplementary views: observed {measuredItemCount}, expected {expectedDataItemCount} data items.");
			});
		}

		static bool IsAttachedDescendant(VisualElement element, RecyclerView recyclerView)
		{
			if (element.Handler?.PlatformView is not AView platformView || !platformView.IsAttachedToWindow)
				return false;

			return IsAttachedDescendant(platformView, recyclerView);
		}

		static bool IsAttachedDescendant(AView platformView, RecyclerView recyclerView)
		{
			if (!platformView.IsAttachedToWindow)
				return false;

			for (var parent = platformView.Parent; parent is AView parentView; parent = parentView.Parent)
			{
				if (ReferenceEquals(parentView, recyclerView))
					return true;
			}

			return false;
		}
	}
}
#endif


using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using WScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28006")]
	public class Issue28006 : ControlsHandlerTestBase
	{
#if WINDOWS
		[Fact]
		public async Task InsertingItemAboveViewportPreservesScrollPosition()
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

			const double requestedItemHeight = 100;
			const double roundingTolerance = 2;
			var items = new ObservableCollection<string>();
			for (int i = 0; i < 20; i++)
				items.Add($"Item {i:00}");

			int firstVisibleItem = -1;
			var resultStatus = new Label { Text = "CollectionView insertion scenario" };
			var firstVisibleStatus = new Label { Text = "First visible item: not recorded" };
			var collectionView = new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label
					{
						FontSize = 12,
						HorizontalTextAlignment = TextAlignment.Center,
						VerticalOptions = LayoutOptions.Center
					};
					label.SetBinding(Label.TextProperty, ".");

					var itemGrid = new Grid
					{
						HeightRequest = requestedItemHeight,
						WidthRequest = 200
					};
					itemGrid.Add(label);
					return itemGrid;
				})
			};
			collectionView.Scrolled += (_, args) =>
			{
				firstVisibleItem = args.FirstVisibleItemIndex;
			};

			var scrollToMiddle = new Button
			{
				Text = "Scroll To Middle",
				FontSize = 10,
				HeightRequest = 40
			};
			scrollToMiddle.Clicked += (_, _) =>
				collectionView.ScrollTo(10, position: ScrollToPosition.Start, animate: false);

			var addItemAbove = new Button
			{
				Text = "Add Item Above",
				FontSize = 10,
				HeightRequest = 40
			};
			addItemAbove.Clicked += (_, _) => items.Insert(9, "Inserted item");

			var rootGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			rootGrid.Add(resultStatus);
			Grid.SetRow(firstVisibleStatus, 1);
			rootGrid.Add(firstVisibleStatus);
			Grid.SetRow(scrollToMiddle, 2);
			rootGrid.Add(scrollToMiddle);
			Grid.SetRow(addItemAbove, 3);
			rootGrid.Add(addItemAbove);
			Grid.SetRow(collectionView, 4);
			rootGrid.Add(collectionView);

			var page = new ContentPage { Content = rootGrid };
			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				var listView = Assert.IsAssignableFrom<WListView>(((CollectionViewHandler)collectionView.Handler).PlatformView);
				await AssertEventually(() => listView.GetChildren<WScrollViewer>().Any());
				var scrollViewer = listView.GetChildren<WScrollViewer>().FirstOrDefault();
				Assert.NotNull(scrollViewer);
				Assert.True(scrollViewer.ViewportHeight > requestedItemHeight);

				Invoke(((ButtonHandler)scrollToMiddle.Handler).PlatformView);
				await AssertEventually(() => firstVisibleItem == 10);
				await AssertEventually(() => listView.ContainerFromIndex(10) is WListViewItem);

				var itemContainer = Assert.IsAssignableFrom<WListViewItem>(listView.ContainerFromIndex(10));
				var itemText = itemContainer.GetChildren<WTextBlock>().FirstOrDefault();
				Assert.NotNull(itemText);
				Assert.Equal("Item 10", itemText.Text);

				double itemHeight = itemContainer.ActualHeight;
				Assert.InRange(itemHeight, requestedItemHeight - roundingTolerance, requestedItemHeight + roundingTolerance);
				Assert.True(
					scrollViewer.VerticalOffset >= 8 * itemHeight,
					$"Midpoint scroll was not established: offset {scrollViewer.VerticalOffset}, item height {itemHeight}.");

				int postInsertLayout = -1;
				void OnLayoutUpdated(object sender, object args)
				{
					if (items.Count == 21)
						postInsertLayout = 1;
				}

				listView.LayoutUpdated += OnLayoutUpdated;
				Invoke(((ButtonHandler)addItemAbove.Handler).PlatformView);
				await AssertEventually(() => postInsertLayout == 1);
				listView.LayoutUpdated -= OnLayoutUpdated;

				Assert.Equal(21, items.Count);
				Assert.Equal("Inserted item", items[9]);
				Assert.Equal("Item 10", items[11]);

				double observedOffset = scrollViewer.VerticalOffset;
				double minimumOffset = 8 * itemHeight;
				Assert.True(
					observedOffset >= minimumOffset - roundingTolerance,
					$"CollectionView jumped to the top after inserting above: offset {observedOffset}, minimum {minimumOffset}, item height {itemHeight}, tolerance {roundingTolerance}.");
			});
		}

		static void Invoke(WButton button)
		{
			var peer = new ButtonAutomationPeer(button);
			var provider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			Assert.NotNull(provider);
			provider.Invoke();
		}
#endif
	}
}


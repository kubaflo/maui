using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

#if WINDOWS
namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29284")]
	public class Issue29284 : ControlsHandlerTestBase
	{
		const string AddedItem = "Added item 3";
		const string FailureSignature = "CollectionView native items did not update after CustomReadOnlyList raised CollectionChanged";

		[Fact]
		public async Task CollectionChangedUpdatesNativeItems()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var source = new CustomReadOnlyList<string>();
			source.SetInitialItems("Initial item 1", "Initial item 2");
			var collectionView = new CollectionView
			{
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					return label;
				}),
				ItemsSource = source,
			};
			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};
			grid.Add(new Label { Text = "CollectionView with a custom IReadOnlyList", FontAttributes = FontAttributes.Bold, FontSize = 18 });
			grid.Add(new Label { Text = "Source count: 2" }, row: 1);
			grid.Add(collectionView, row: 2);
			grid.Add(new Button { Text = "Add item to custom collection" }, row: 3);
			grid.Add(new Button { Text = "Check displayed items" }, row: 4);
			grid.Add(new Label { Text = "CollectionView display status", FontAttributes = FontAttributes.Bold }, row: 5);
			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var collectionHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				WListViewBase nativeList = collectionHandler.PlatformView;

				Assert.NotNull(nativeList);
				Assert.True(nativeList.IsLoaded, "CollectionView native view was not attached to the test window");
				Assert.True(collectionView.Frame.Width > 0 && collectionView.Frame.Height > 0, $"CollectionView frame was empty: {collectionView.Frame}");

				bool initialCountReady = await Wait(() => nativeList.Items.Count == 2);
				bool initialItemsRealized = await Wait(() =>
				{
					var texts = nativeList.GetChildren<WTextBlock>();
					return texts.Any(text => text.Text == "Initial item 1") &&
						texts.Any(text => text.Text == "Initial item 2");
				});
				Assert.True(initialCountReady, $"CollectionView native item count was {nativeList.Items.Count}; expected 2");
				Assert.True(initialItemsRealized, "CollectionView did not realize both initial items");

				int notificationIndex = -1;
				int callbackCount = 0;
				NotifyCollectionChangedAction notificationAction = (NotifyCollectionChangedAction)(-1);
				source.CollectionChanged += (_, args) =>
				{
					callbackCount++;
					notificationAction = args.Action;
					notificationIndex = args.NewStartingIndex;
				};

				source.Add(AddedItem);

				Assert.Equal(3, source.Count);
				Assert.Equal(1, callbackCount);
				Assert.Equal(NotifyCollectionChangedAction.Add, notificationAction);
				Assert.Equal(2, notificationIndex);
				Assert.Same(collectionHandler, collectionView.Handler);
				Assert.Same(nativeList, collectionHandler.PlatformView);
				Assert.True(nativeList.IsLoaded, "CollectionView native view detached after the source notification");

				bool nativeCountUpdated = await Wait(() => nativeList.Items.Count == 3);
				bool addedItemRealized = await Wait(() =>
					nativeList.GetChildren<WTextBlock>().Any(text => text.Text == AddedItem));

				Assert.True(nativeCountUpdated,
					$"{FailureSignature}: source count={source.Count}, native count={nativeList.Items.Count}, native count updated={nativeCountUpdated}, added item realized={addedItemRealized}, expected native count=3, expected text=\"{AddedItem}\"");
				Assert.True(addedItemRealized,
					$"{FailureSignature}: source count={source.Count}, native count={nativeList.Items.Count}, native count updated={nativeCountUpdated}, added item realized={addedItemRealized}, expected native count=3, expected text=\"{AddedItem}\"");
			});
		}

		sealed class CustomReadOnlyList<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
		{
			List<T> _items;

			public event NotifyCollectionChangedEventHandler CollectionChanged;
			public event PropertyChangedEventHandler PropertyChanged;

			public int Count => _items.Count;
			public T this[int index] => _items[index];

			public void SetInitialItems(T first, T second)
			{
				_items = new List<T> { first, second };
			}

			public void Add(T item)
			{
				int index = _items.Count;
				_items.Add(item);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add,
					item,
					index));
			}

			public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
#endif


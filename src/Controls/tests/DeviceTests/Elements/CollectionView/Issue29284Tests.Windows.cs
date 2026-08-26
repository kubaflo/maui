using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	[Category("Issue29284")]
	public class Issue29284 : ControlsHandlerTestBase
	{
		const string AddedItem = "Third item";

		[Fact]
		public async Task CustomReadOnlyListCollectionChangedUpdatesNativeItems()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var source = new ReadOnlyNotifyingCollection();
			source.Initialize(new List<string> { "First item", "Second item" });
			var collectionView = new CollectionView
			{
				HeightRequest = 220,
				ItemsSource = source,
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, new Binding("."));

					return new Border
					{
						Padding = 12,
						Margin = new Thickness(0, 4),
						Stroke = Colors.Gray,
						Content = label
					};
				})
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 12,
					Children = { collectionView }
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var nativeList = Assert.IsAssignableFrom<WListViewBase>(handler.PlatformView);

				Assert.Equal(2, source.Count);
				Assert.Equal(source.Count, nativeList.Items.Count);
				Assert.Contains(nativeList.Items.OfType<ItemTemplateContext>(), item => Equals(item.Item, "First item"));
				Assert.Contains(nativeList.Items.OfType<ItemTemplateContext>(), item => Equals(item.Item, "Second item"));

				int eventIndex = -1;
				source.CollectionChanged += (_, args) => eventIndex = args.NewStartingIndex;

				source.Add(AddedItem);

				Assert.Equal(2, eventIndex);
				Assert.Equal(3, source.Count);
				Assert.Equal(AddedItem, source[2]);

				int measuredNativeCount = nativeList.Items.Count;
				bool updated = await Wait(() =>
				{
					measuredNativeCount = nativeList.Items.Count;
					return measuredNativeCount == source.Count &&
						nativeList.Items.OfType<ItemTemplateContext>().Any(item => Equals(item.Item, AddedItem));
				});

				Assert.True(
					updated,
					$"CollectionView native item count after custom IReadOnlyList CollectionChanged was {measuredNativeCount}, expected {source.Count}; {AddedItem} was not exposed.");
				Assert.True(
					measuredNativeCount == source.Count,
					$"CollectionView native item count after custom IReadOnlyList CollectionChanged was {measuredNativeCount}, expected {source.Count}.");
			});
		}

		sealed class ReadOnlyNotifyingCollection : IReadOnlyList<string>, INotifyCollectionChanged, INotifyPropertyChanged
		{
			List<string> _items;

			public event NotifyCollectionChangedEventHandler CollectionChanged;
			public event PropertyChangedEventHandler PropertyChanged;

			public int Count => _items.Count;

			public string this[int index] => _items[index];

			public void Initialize(List<string> items) => _items = items;

			public void Add(string item)
			{
				_items.Add(item);
				CollectionChanged?.Invoke(
					this,
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
			}

			public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}


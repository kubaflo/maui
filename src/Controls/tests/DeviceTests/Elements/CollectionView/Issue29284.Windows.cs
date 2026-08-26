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
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue29284")]
	public class Issue29284 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CollectionViewUpdatesForObservableReadOnlyList()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			const string alpha = "Alpha";
			const string beta = "Beta";
			var source = new ObservableReadOnlyList();
			source.Initialize(alpha);
			var collectionView = new CollectionView
			{
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label { FontSize = 18 };
					itemLabel.SetBinding(Label.TextProperty, ".");
					return new Grid
					{
						Padding = 12,
						Children = { itemLabel }
					};
				})
			};
			collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(ObservableReadOnlyList.Items));

			var addInvocationLabel = new Label { Text = "Add Beta to the custom collection." };
			var statusLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "CollectionView notification test"
			};
			var observationLabel = new Label { Text = "The collection starts with Alpha." };
			var addButton = new Button { Text = "Add Beta and raise CollectionChanged" };
			int buttonCallbackCount = -1;
			addButton.Clicked += (_, _) =>
			{
				buttonCallbackCount = 1;
				source.Add(beta);
			};

			var statusLayout = new VerticalStackLayout
			{
				Spacing = 4,
				Children = { statusLabel, observationLabel }
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
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "CollectionView with a custom IReadOnlyList"
			});
			grid.Add(new Label { Text = "Visible CollectionView items (initially Alpha):" }, 0, 1);
			grid.Add(collectionView, 0, 2);
			grid.Add(addButton, 0, 3);
			grid.Add(addInvocationLabel, 0, 4);
			grid.Add(statusLayout, 0, 5);

			var page = new ContentPage
			{
				Title = "Issue 29284",
				BindingContext = source,
				Content = grid
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(
				new Microsoft.Maui.Controls.Window(page),
				async _ =>
				{
					var nativeListView = Assert.IsAssignableFrom<WListView>(collectionView.Handler.PlatformView);
					await AssertEventually(
						() => nativeListView.Items.Count == 1 && GetRenderedTexts(nativeListView).Contains(alpha),
						timeout: 5000,
						message: "CollectionView did not render the initial Alpha item.");

					NotifyCollectionChangedEventArgs observedNotification = null;
					source.CollectionChanged += (_, args) => observedNotification = args;

					var nativeButton = Assert.IsAssignableFrom<WButton>(addButton.Handler.PlatformView);
					var peer = new ButtonAutomationPeer(nativeButton);
					var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
					Assert.NotNull(invokeProvider);
					invokeProvider.Invoke();

					Assert.Equal(1, buttonCallbackCount);
					Assert.Equal(2, source.Count);
					Assert.Equal(beta, source[1]);
					Assert.NotNull(observedNotification);
					Assert.Equal(NotifyCollectionChangedAction.Add, observedNotification.Action);
					Assert.Equal(1, observedNotification.NewStartingIndex);
					Assert.NotNull(observedNotification.NewItems);
					Assert.Equal(beta, Assert.Single(observedNotification.NewItems.Cast<string>()));

					bool nativeItemsUpdated = await Wait(
						() => nativeListView.Items.Count == 2 && GetRenderedTexts(nativeListView).Contains(beta),
						timeout: 5000);
					int nativeCount = nativeListView.Items.Count;
					string renderedTexts = string.Join(", ", GetRenderedTexts(nativeListView));

					Assert.True(
						nativeItemsUpdated,
						$"CollectionView native items did not update after custom IReadOnlyList raised CollectionChanged. Native count: {nativeCount}; rendered texts: [{renderedTexts}]; expected count: 2; expected item: {beta}.");
				});
		}

		static string[] GetRenderedTexts(WListView listView) =>
			listView.GetChildren<WTextBlock>().Select(textBlock => textBlock.Text).ToArray();

		sealed class ObservableReadOnlyList : IReadOnlyList<string>, INotifyCollectionChanged, INotifyPropertyChanged
		{
			List<string> _items;
			NotifyCollectionChangedEventHandler _collectionChanged;

			public event NotifyCollectionChangedEventHandler CollectionChanged
			{
				add => _collectionChanged += value;
				remove => _collectionChanged -= value;
			}

			public event PropertyChangedEventHandler PropertyChanged;

			public IReadOnlyList<string> Items => this;

			public int Count => _items.Count;

			public string this[int index] => _items[index];

			public void Initialize(params string[] items) => _items = new(items);

			public void Add(string item)
			{
				int index = _items.Count;
				_items.Add(item);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
				_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add,
					item,
					index));
			}

			public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}


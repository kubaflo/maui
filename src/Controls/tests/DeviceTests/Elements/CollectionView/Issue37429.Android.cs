using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using TextView = Android.Widget.TextView;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue37429")]
	public class Issue37429 : ControlsHandlerTestBase
	{
		const string EmptyViewText = "EMPTY VIEW: No groups remain";

		[Fact]
		public async Task GroupedCollectionViewDisplaysEmptyViewAfterFinalGroupIsRemoved()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var emptyGroups = new ObservableCollection<ItemGroup>();
			var emptyScene = CreateScene(emptyGroups);

			await CreateHandlerAndAddToWindow<IWindowHandler>(emptyScene.Page, async _ =>
			{
				var emptyHandler = Assert.IsType<CollectionViewHandler>(emptyScene.CollectionView.Handler);
				var emptyRecyclerView = emptyHandler.PlatformView;
				Assert.IsAssignableFrom<IMauiRecyclerView>(emptyRecyclerView);

				TextView nativeEmptyView = null;
				await AssertEventually(() =>
				{
					nativeEmptyView = FindTextView(emptyRecyclerView, EmptyViewText);
					return nativeEmptyView is { IsShown: true, MeasuredWidth: > 0, MeasuredHeight: > 0 };
				}, message: "Initially empty grouped CollectionView should visibly render its EmptyView");
			});

			var initialGroup = new ItemGroup { Title = "Group 1" };
			initialGroup.Add("Item 1");
			var groups = new ObservableCollection<ItemGroup> { initialGroup };
			var scene = CreateScene(groups);

			await CreateHandlerAndAddToWindow<IWindowHandler>(scene.Page, async _ =>
			{
				var collectionHandler = Assert.IsType<CollectionViewHandler>(scene.CollectionView.Handler);
				var recyclerView = collectionHandler.PlatformView;
				Assert.IsAssignableFrom<IMauiRecyclerView>(recyclerView);

				await AssertEventually(() =>
				{
					var item = FindTextView(recyclerView, "Item 1");
					return item is { IsShown: true, MeasuredWidth: > 0, MeasuredHeight: > 0 };
				}, message: "The initial grouped item should be visibly rendered");
				Assert.Null(FindTextView(recyclerView, EmptyViewText));

				var removalObserved = false;
				var observedRemovalCount = -1;
				groups.CollectionChanged += OnGroupsCollectionChanged;

				var nativeRemoveButton = scene.RemoveGroupButton.ToPlatform();
				Assert.True(nativeRemoveButton.PerformClick(), "The attached native - Group button should accept the click");

				await AssertEventually(() => removalObserved, message: "The outer grouped source should report the final group removal");
				Assert.Equal(0, observedRemovalCount);
				Assert.Empty(groups);

				TextView nativeEmptyView = null;
				var emptyViewFound = false;
				var emptyViewShown = false;
				var measuredWidth = 0;
				var measuredHeight = 0;
				var searchAttempts = 0;
				await AssertEventually(() =>
				{
					nativeEmptyView = FindTextView(recyclerView, EmptyViewText);
					emptyViewFound = nativeEmptyView is not null;
					emptyViewShown = nativeEmptyView?.IsShown == true;
					measuredWidth = nativeEmptyView?.MeasuredWidth ?? 0;
					measuredHeight = nativeEmptyView?.MeasuredHeight ?? 0;
					searchAttempts++;
					return (emptyViewFound && emptyViewShown && measuredWidth > 0 && measuredHeight > 0) || searchAttempts >= 10;
				}, timeout: 2000);
				Assert.True(
					emptyViewFound && emptyViewShown && measuredWidth > 0 && measuredHeight > 0,
					$"Grouped CollectionView should render EmptyView after final group removal; sourceCount={groups.Count}, nativeEmptyViewFound={emptyViewFound}, nativeVisible={emptyViewShown}, measuredWidth={measuredWidth}, measuredHeight={measuredHeight}");

				void OnGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
				{
					if (args.Action == NotifyCollectionChangedAction.Remove)
					{
						removalObserved = true;
						observedRemovalCount = groups.Count;
					}
				}
			});
		}

		static (ContentPage Page, CollectionView CollectionView, Button RemoveGroupButton) CreateScene(ObservableCollection<ItemGroup> groups)
		{
			var nextGroupNumber = 2;
			var nextItemNumber = 2;
			var addGroupButton = new Button { Text = "+ Group" };
			var removeGroupButton = new Button { Text = "- Group" };
			var groupCountLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = $"Groups: {groups.Count}"
			};
			var emptyMessage = new Label
			{
				Text = EmptyViewText,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center
			};
			var collectionView = new CollectionView
			{
				IsGrouped = true,
				ItemsSource = groups,
				EmptyView = emptyMessage,
				GroupHeaderTemplate = new DataTemplate(() =>
				{
					var title = new Label { VerticalOptions = LayoutOptions.Center };
					title.SetBinding(Label.TextProperty, nameof(ItemGroup.Title));
					var addItemButton = new Button { Text = "+ Item" };
					var removeItemButton = new Button { Text = "- Item" };
					addItemButton.Clicked += (sender, _) =>
					{
						if (sender is Button { BindingContext: ItemGroup group })
							group.Add($"Item {nextItemNumber++}");
					};
					removeItemButton.Clicked += (sender, _) =>
					{
						if (sender is Button { BindingContext: ItemGroup group } && group.Count > 0)
							group.RemoveAt(group.Count - 1);
					};

					var header = new Grid
					{
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Star),
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Auto)
						]
					};
					header.Add(title);
					header.Add(addItemButton, 1);
					header.Add(removeItemButton, 2);
					return header;
				}),
				GroupFooterTemplate = new DataTemplate(() =>
				{
					var footer = new Label { Text = "GROUP EMPTY" };
					footer.SetBinding(Label.IsVisibleProperty, nameof(ItemGroup.IsEmpty));
					return footer;
				}),
				ItemTemplate = new DataTemplate(() =>
				{
					var item = new Label();
					item.SetBinding(Label.TextProperty, ".");
					return item;
				})
			};

			addGroupButton.Clicked += (_, _) =>
			{
				var group = new ItemGroup { Title = $"Group {nextGroupNumber++}" };
				group.Add($"Item {nextItemNumber++}");
				groups.Add(group);
				groupCountLabel.Text = $"Groups: {groups.Count}";
			};
			removeGroupButton.Clicked += (_, _) =>
			{
				if (groups.Count > 0)
					groups.RemoveAt(groups.Count - 1);
				groupCountLabel.Text = $"Groups: {groups.Count}";
			};

			var topBar = new HorizontalStackLayout { Spacing = 12 };
			topBar.Add(addGroupButton);
			topBar.Add(removeGroupButton);

			var resultLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "EmptyView status"
			};
			var checkButton = new Button { Text = "Check EmptyView" };
			var bottomBar = new VerticalStackLayout { Spacing = 8 };
			bottomBar.Add(checkButton);
			bottomBar.Add(resultLabel);

			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				[
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				]
			};
			grid.Add(topBar);
			grid.Add(groupCountLabel, 0, 1);
			grid.Add(collectionView, 0, 2);
			grid.Add(bottomBar, 0, 3);

			return (new ContentPage { Content = grid }, collectionView, removeGroupButton);
		}

		static TextView FindTextView(AView view, string text)
		{
			if (view is TextView textView && textView.Text == text)
				return textView;

			if (view is not AViewGroup viewGroup)
				return null;

			for (var index = 0; index < viewGroup.ChildCount; index++)
			{
				var child = viewGroup.GetChildAt(index);
				if (child is null)
					continue;

				var match = FindTextView(child, text);
				if (match is not null)
					return match;
			}

			return null;
		}

		sealed class ItemGroup : ObservableCollection<string>
		{
			public string Title { get; set; }

			public bool IsEmpty
			{
				get
				{
					return Count == 0;
				}
			}

			protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
			{
				base.OnCollectionChanged(args);
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEmpty)));
			}
		}
	}
}


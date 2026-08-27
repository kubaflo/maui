using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AView = Android.Views.View;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue37429")]
	public class Issue37429 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyViewIsRenderedAfterRemovingLastGroup()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var groups = new ObservableCollection<ItemGroup>();
			var addGroupButton = new Button { Text = "+ Group" };
			var removeGroupButton = new Button { Text = "- Group" };
			var emptyView = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "No groups available" }
				}
			};
			var collectionView = new CollectionView
			{
				IsGrouped = true,
				ItemsSource = groups,
				EmptyView = emptyView
			};

			Label renderedHeader = null;
			Label renderedFooter = null;
			collectionView.ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label();
				itemLabel.SetBinding(Label.TextProperty, ".");
				return itemLabel;
			});
			collectionView.GroupHeaderTemplate = new DataTemplate(() =>
			{
				renderedHeader = new Label();
				renderedHeader.SetBinding(Label.TextProperty, nameof(ItemGroup.Title));

				var addItemButton = new Button { Text = "+ Item" };
				addItemButton.Clicked += (_, _) =>
				{
					if (addItemButton.BindingContext is ItemGroup group)
						group.Add($"Item {group.Count + 1}");
				};

				var removeItemButton = new Button { Text = "- Item" };
				removeItemButton.Clicked += (_, _) =>
				{
					if (removeItemButton.BindingContext is ItemGroup group && group.Count > 0)
						group.RemoveAt(group.Count - 1);
				};

				var headerGrid = new Grid
				{
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Star),
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Auto)
					}
				};
				headerGrid.Children.Add(renderedHeader);
				headerGrid.Children.Add(addItemButton);
				headerGrid.Children.Add(removeItemButton);
				Grid.SetColumn(addItemButton, 1);
				Grid.SetColumn(removeItemButton, 2);
				return headerGrid;
			});
			collectionView.GroupFooterTemplate = new DataTemplate(() =>
			{
				renderedFooter = new Label { Text = "This group is empty" };
				renderedFooter.SetBinding(VisualElement.IsVisibleProperty, nameof(ItemGroup.IsEmpty));
				return renderedFooter;
			});

			addGroupButton.Clicked += (_, _) => groups.Add(new ItemGroup { Title = $"Group {groups.Count + 1}" });
			removeGroupButton.Clicked += (_, _) =>
			{
				if (groups.Count > 0)
					groups.RemoveAt(groups.Count - 1);
			};

			var topButtons = new HorizontalStackLayout
			{
				Children = { addGroupButton, removeGroupButton }
			};
			var statusLabel = new Label { Text = "Collection status" };
			var checkButton = new Button { Text = "Check EmptyView" };
			var statusArea = new VerticalStackLayout
			{
				Children = { checkButton, statusLabel }
			};
			var root = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				},
				Children = { topButtons, collectionView, statusArea }
			};
			Grid.SetRow(collectionView, 1);
			Grid.SetRow(statusArea, 2);
			var page = new ContentPage { Content = root };

			int observedCollectionAction = -1;
			groups.CollectionChanged += (_, args) => observedCollectionAction = (int)args.Action;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(collectionView.Handler);
				Assert.NotNull(emptyView.Handler);
				Assert.NotNull(addGroupButton.Handler);
				Assert.NotNull(removeGroupButton.Handler);

				var recyclerView = (RecyclerView)collectionView.Handler.PlatformView;
				var emptyPlatformView = (AView)emptyView.Handler.PlatformView;
				var nativeAddButton = (AppCompatButton)addGroupButton.Handler.PlatformView;
				var nativeRemoveButton = (AppCompatButton)removeGroupButton.Handler.PlatformView;

				await AssertEventually(
					() => recyclerView.GetAdapter() is EmptyViewAdapter &&
						emptyPlatformView.IsAttachedToWindow &&
						emptyPlatformView.IsShown &&
						emptyPlatformView.Width > 0 &&
						emptyPlatformView.Height > 0,
					message: "Initial EmptyView should be natively rendered");

				nativeAddButton.PerformClick();

				await AssertEventually(
					() => groups.Count == 1 &&
						recyclerView.GetAdapter() is not EmptyViewAdapter &&
						!emptyPlatformView.IsShown &&
						renderedHeader is not null &&
						renderedHeader.Text == "Group 1" &&
						renderedHeader.Handler?.PlatformView is AView headerView &&
						headerView.IsAttachedToWindow &&
						renderedFooter is not null &&
						renderedFooter.Text == "This group is empty" &&
						renderedFooter.Handler?.PlatformView is AView footerView &&
						footerView.IsAttachedToWindow,
					message: "The empty group header and footer should be natively rendered");

				var groupedAdapter = recyclerView.GetAdapter();
				Assert.NotNull(groupedAdapter);
				observedCollectionAction = -1;
				nativeRemoveButton.PerformClick();

				Assert.Equal((int)NotifyCollectionChangedAction.Remove, observedCollectionAction);
				Assert.Empty(groups);
				await AssertEventually(
					() => groupedAdapter.ItemCount == 0,
					message: "The Android adapter should process the source as empty");

				await AssertEventually(
					() => recyclerView.GetAdapter() is EmptyViewAdapter &&
						emptyPlatformView.IsAttachedToWindow &&
						emptyPlatformView.IsShown &&
						emptyPlatformView.Width > 0 &&
						emptyPlatformView.Height > 0,
					message: "EmptyView should be natively rendered after removing the last group");
			});
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

			protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
			{
				base.OnCollectionChanged(e);
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEmpty)));
			}
		}
	}
}


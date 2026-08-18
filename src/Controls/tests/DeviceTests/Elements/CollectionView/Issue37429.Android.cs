using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using AViewGroup = Android.Views.ViewGroup;
using AViewStates = Android.Views.ViewStates;
using TextView = Android.Widget.TextView;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	public class Issue37429 : ControlsHandlerTestBase
	{
		const string EmptyViewText = "EMPTY VIEW: No groups remain";

		[Fact]
		public async Task RemovingLastGroupDisplaysEmptyView()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var groups = new ObservableCollection<ItemGroup>
			{
				new ItemGroup { Title = "Group 1" }
			};
			var removeGroupButton = new Button { Text = "- Group" };
			var emptyViewLabel = new Label
			{
				Text = EmptyViewText,
				HorizontalOptions = LayoutOptions.Center
			};
			var emptyView = new VerticalStackLayout
			{
				Padding = 24,
				Children = { emptyViewLabel }
			};
			var collectionView = CreateCollectionView(groups, emptyView);
			var resultLabel = new Label
			{
				AutomationId = "Issue37429Result",
				Text = "NO BUG:",
				FontAttributes = FontAttributes.Bold
			};
			var clickedCount = 0;
			var observedRemovedIndex = -1;

			removeGroupButton.Clicked += (_, _) =>
			{
				clickedCount++;
				if (groups.Count > 0)
					groups.RemoveAt(groups.Count - 1);
			};
			groups.CollectionChanged += (_, args) =>
			{
				if (args.Action == NotifyCollectionChangedAction.Remove)
					observedRemovedIndex = args.OldStartingIndex;
			};

			var topBar = new HorizontalStackLayout
			{
				Spacing = 12,
				Children =
				{
					removeGroupButton
				}
			};
			var addGroupButton = new Button { Text = "+ Group" };
			addGroupButton.Clicked += (_, _) => groups.Add(new ItemGroup { Title = $"Group {groups.Count + 1}" });
			topBar.Children.Insert(0, addGroupButton);
			var checkButton = new Button { Text = "Check EmptyView" };
			checkButton.Clicked += (_, _) =>
			{
				var emptyViewIsDisplayed =
					emptyViewLabel.Handler is not null &&
					emptyViewLabel.IsVisible &&
					emptyViewLabel.Width > 0 &&
					emptyViewLabel.Height > 0;

				resultLabel.Text = groups.Count == 0 && !emptyViewIsDisplayed
					? "BUG REPRODUCED:"
					: "NO BUG:";
			};

			var root = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				}
			};
			root.Add(new Label { Text = "Grouped CollectionView EmptyView", FontSize = 20 });
			root.Add(topBar);
			Grid.SetRow(topBar, 1);
			root.Add(collectionView);
			Grid.SetRow(collectionView, 2);
			root.Add(checkButton);
			Grid.SetRow(checkButton, 3);
			root.Add(resultLabel);
			Grid.SetRow(resultLabel, 4);

			var page = new ContentPage { Content = root };

			Assert.True(collectionView.IsGrouped);
			Assert.Same(groups, collectionView.ItemsSource);
			Assert.Same(emptyView, collectionView.EmptyView);
			Assert.Equal(EmptyViewText, emptyViewLabel.Text);
			Assert.Equal(new Thickness(24), emptyView.Padding);
			Assert.Equal(LayoutOptions.Center, emptyViewLabel.HorizontalOptions);
			Assert.Null(collectionView.Style);

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var collectionHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var recyclerView = collectionHandler.PlatformView;

				Assert.True(recyclerView.IsAttachedToWindow);
				await AssertEventually(
					() => IsVisibleNativeText(recyclerView, "Group 1") &&
						IsVisibleNativeText(recyclerView, "Group is empty"),
					timeout: 3000,
					message: "The initial empty group header and footer should be visible with positive native bounds.");
				Assert.Null(FindTextView(recyclerView, EmptyViewText));
				Assert.NotNull(recyclerView.GetAdapter());
				Assert.IsNotType<EmptyViewAdapter>(recyclerView.GetAdapter());

				var nativeRemoveButton = Assert.IsAssignableFrom<AView>(removeGroupButton.Handler.PlatformView);
				Assert.True(nativeRemoveButton.PerformClick(), "The native - Group button click should be handled.");

				await AssertEventually(
					() => clickedCount == 1 &&
						observedRemovedIndex == 0 &&
						groups.Count == 0,
					message: "The native click should remove the final group at index 0.");
				await AssertEventually(
					() =>
					{
						var nativeEmptyView = FindTextView(recyclerView, EmptyViewText);
						return nativeEmptyView is not null &&
							nativeEmptyView.Text == EmptyViewText &&
							nativeEmptyView.Visibility == AViewStates.Visible &&
							nativeEmptyView.IsShown &&
							nativeEmptyView.Width > 0 &&
							nativeEmptyView.Height > 0 &&
							IntersectsViewport(nativeEmptyView, recyclerView);
					},
					timeout: 3000,
					message: "EmptyView native view should be visible and laid out after removing the last group; expected handler=CollectionViewHandler, ancestor=MauiRecyclerView, visibility=Visible, shown=True, bounds=>0x>0, viewportIntersection=True.");
			});
		}

		static CollectionView CreateCollectionView(ObservableCollection<ItemGroup> groups, VerticalStackLayout emptyView)
		{
			return new CollectionView
			{
				IsGrouped = true,
				ItemsSource = groups,
				EmptyView = emptyView,
				GroupHeaderTemplate = new DataTemplate(() =>
				{
					var header = new Grid
					{
						Padding = 8,
						ColumnDefinitions =
						{
							new ColumnDefinition { Width = GridLength.Star },
							new ColumnDefinition { Width = GridLength.Auto },
							new ColumnDefinition { Width = GridLength.Auto }
						}
					};
					var title = new Label { VerticalOptions = LayoutOptions.Center };
					title.SetBinding(Label.TextProperty, nameof(ItemGroup.Title));
					var addItemButton = new Button { Text = "+ Item" };
					addItemButton.Clicked += (_, _) =>
					{
						if (addItemButton.BindingContext is ItemGroup group)
							group.Add("Item");
					};
					var removeItemButton = new Button { Text = "- Item" };
					removeItemButton.Clicked += (_, _) =>
					{
						if (removeItemButton.BindingContext is ItemGroup group && group.Count > 0)
							group.RemoveAt(group.Count - 1);
					};
					header.Add(title);
					header.Add(addItemButton);
					Grid.SetColumn(addItemButton, 1);
					header.Add(removeItemButton);
					Grid.SetColumn(removeItemButton, 2);
					return header;
				}),
				GroupFooterTemplate = new DataTemplate(() => new Label
				{
					Text = "Group is empty",
					Padding = 8
				}),
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label { Padding = 8 };
					label.SetBinding(Label.TextProperty, ".");
					return label;
				})
			};
		}

		static bool IsVisibleNativeText(RecyclerView recyclerView, string text)
		{
			var nativeView = FindTextView(recyclerView, text);
			return nativeView is not null &&
				nativeView.Visibility == AViewStates.Visible &&
				nativeView.IsShown &&
				nativeView.Width > 0 &&
				nativeView.Height > 0 &&
				IntersectsViewport(nativeView, recyclerView);
		}

		static TextView FindTextView(AView view, string text)
		{
			if (view is TextView textView && textView.Text == text)
				return textView;

			if (view is AViewGroup viewGroup)
			{
				for (var i = 0; i < viewGroup.ChildCount; i++)
				{
					var match = FindTextView(viewGroup.GetChildAt(i), text);
					if (match is not null)
						return match;
				}
			}

			return null;
		}

		static bool IntersectsViewport(AView view, RecyclerView recyclerView)
		{
			var viewLocation = new int[2];
			var viewportLocation = new int[2];
			view.GetLocationOnScreen(viewLocation);
			recyclerView.GetLocationOnScreen(viewportLocation);

			return viewLocation[0] < viewportLocation[0] + recyclerView.Width &&
				viewLocation[0] + view.Width > viewportLocation[0] &&
				viewLocation[1] < viewportLocation[1] + recyclerView.Height &&
				viewLocation[1] + view.Height > viewportLocation[1];
		}

		sealed class ItemGroup : ObservableCollection<string>
		{
			public string Title { get; set; } = string.Empty;
		}
	}
}

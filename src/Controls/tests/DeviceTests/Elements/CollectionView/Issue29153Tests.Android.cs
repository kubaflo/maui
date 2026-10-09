using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29153")]
	public class Issue29153 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task GroupedInsertionWithKeepLastItemInViewDoesNotThrow()
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
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var groups = new List<ItemGroup>
			{
				CreateGroup("Fruits",
				[
					"Apple", "Banana", "Orange", "Grapes", "Mango",
					"Pineapple", "Strawberry", "Blueberry", "Peach", "Cherry",
					"Watermelon", "Papaya", "Kiwi", "Pear", "Plum",
					"Avocado", "Fig", "Guava", "Lychee", "Pomegranate",
					"Lime", "Lemon", "Coconut", "Apricot", "Blackberry"
				]),
				CreateGroup("Vegetables",
				[
					"Carrot", "Broccoli", "Spinach", "Potato", "Tomato",
					"Cucumber", "Lettuce", "Onion", "Garlic", "Pepper",
					"Zucchini", "Pumpkin", "Radish", "Beetroot", "Cabbage",
					"Sweet Potato", "Turnip", "Cauliflower", "Celery", "Asparagus",
					"Eggplant", "Chili", "Corn", "Peas", "Mushroom"
				])
			};

			var modeButton = new Button { Text = "Use KeepLastItemInView" };
			var addButton = new Button { Text = "Add item at top" };
			var collectionView = new CollectionView
			{
				IsGrouped = true,
				ItemsSource = groups,
				GroupHeaderTemplate = new DataTemplate(() =>
				{
					var label = new Label { FontAttributes = FontAttributes.Bold };
					label.SetBinding(Label.TextProperty, nameof(ItemGroup.Key));
					return label;
				}),
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					return label;
				})
			};
			var statusLayout = new VerticalStackLayout
			{
				new Label { Text = "Mode: KeepItemsInView" },
				new Label { Text = "First group items: 25" }
			};
			var grid = new Grid
			{
				Padding = 12,
				RowSpacing = 8,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label
			{
				Text = "Issue 29153 grouped CollectionView",
				FontAttributes = FontAttributes.Bold
			});
			grid.Add(modeButton, row: 1);
			grid.Add(statusLayout, row: 2);
			grid.Add(collectionView, row: 3);
			grid.Add(addButton, row: 4);

			var page = new ContentPage { Content = grid };
			int modeStage = -1;
			int addStage = -1;
			var notRunToken = new object();
			var successToken = new object();
			object insertionOutcome = notRunToken;

			modeButton.Clicked += (sender, args) =>
			{
				collectionView.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView;
				modeStage = 1;
			};
			addButton.Clicked += (sender, args) =>
			{
				try
				{
					groups[0].Insert(0, "Dragonfruit");
					insertionOutcome = successToken;
				}
				catch (ArgumentOutOfRangeException ex)
				{
					insertionOutcome = ex;
				}

				addStage = 2;
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var collectionHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var recyclerView = collectionHandler.PlatformView;
				Assert.NotNull(recyclerView);
				await recyclerView.WaitForLayoutOrNonZeroSize();

				Assert.Same(groups[0], ((IEnumerable<ItemGroup>)collectionView.ItemsSource).First());
				Assert.Equal("Apple", groups[0][0]);
				Assert.Equal(25, groups[0].Count);
				var adapter = recyclerView.GetAdapter();
				Assert.NotNull(adapter);
				Assert.Equal(52, adapter.ItemCount);
				await AssertEventually(
					() => recyclerView.GetChildrenOfType<global::Android.Widget.TextView>()
						.Any(textView => textView.Text == "Apple"),
					timeout: 5000);

				Assert.Equal(ItemsUpdatingScrollMode.KeepItemsInView, collectionView.ItemsUpdatingScrollMode);
				var modePlatformButton = Assert.IsAssignableFrom<AppCompatButton>(modeButton.Handler.PlatformView);
				await modePlatformButton.WaitForLayoutOrNonZeroSize();
				Assert.True(DispatchTap(modePlatformButton));
				await AssertEventually(() => modeStage == 1, timeout: 5000);
				Assert.Equal(ItemsUpdatingScrollMode.KeepLastItemInView, collectionView.ItemsUpdatingScrollMode);

				var addPlatformButton = Assert.IsAssignableFrom<AppCompatButton>(addButton.Handler.PlatformView);
				await addPlatformButton.WaitForLayoutOrNonZeroSize();
				Assert.True(DispatchTap(addPlatformButton));
				await AssertEventually(() => addStage == 2, timeout: 5000);

				Assert.Equal("Dragonfruit", groups[0][0]);
				int actualCount = groups[0].Count;
				int expectedCount = 26;
				Assert.Equal(expectedCount, actualCount);
				Assert.True(
					ReferenceEquals(insertionOutcome, successToken),
					$"Grouped CollectionView insertion threw {(insertionOutcome as Exception)?.GetType().Name}; observed first-group count {actualCount}, expected {expectedCount}");
			});
		}

		static bool DispatchTap(AppCompatButton button)
		{
			float x = button.Width / 2f;
			float y = button.Height / 2f;
			long downTime = global::Android.OS.SystemClock.UptimeMillis();

			var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, x, y, 0);
			bool downHandled = button.DispatchTouchEvent(down);
			down.Recycle();

			var up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, x, y, 0);
			bool upHandled = button.DispatchTouchEvent(up);
			up.Recycle();

			return downHandled && upHandled;
		}

		static ItemGroup CreateGroup(string key, IEnumerable<string> items)
		{
			var group = new ItemGroup { Key = key };
			foreach (string item in items)
				group.Add(item);

			return group;
		}

		sealed class ItemGroup : ObservableCollection<string>
		{
			public string Key { get; init; } = string.Empty;
		}
	}
}


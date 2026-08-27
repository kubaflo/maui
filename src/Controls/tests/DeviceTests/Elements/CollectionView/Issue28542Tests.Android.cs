#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.OS;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AViewConfiguration = Android.Views.ViewConfiguration;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue28542")]
	public class Issue28542 : ControlsHandlerTestBase
	{
		const double ShortItemHeight = 56;
		const double TallItemHeight = 320;
		const double VerticalItemMargin = 4;
		const double ThumbTolerance = 0.5;

		[Fact]
		public async Task VariableHeightScrollbarThumbRemainsStableAfterScrolling()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var calibration = CreateScenario(CreateUniformItems());
			await CreateHandlerAndAddToWindow(calibration.Page, async () =>
			{
				var recyclerView = GetRecyclerView(calibration.CollectionView);
				await recyclerView.WaitForLayoutOrNonZeroSize();
				await AssertEventually(
					() => recyclerView.FindViewHolderForAdapterPosition(0) is not null,
					message: "The calibration CollectionView did not realize its first item.");

				Assert.True(recyclerView.VerticalScrollBarEnabled);
				AssertItem(recyclerView, 0, calibration.Items[0], ShortItemHeight);

				var metrics = GetMetrics(recyclerView);
				var expectedThumbHeight = GetExpectedThumbHeight(metrics.Extent, calibration.Items, recyclerView.Context.Resources.DisplayMetrics.Density);

				Assert.True(
					Math.Abs(metrics.ThumbHeight - expectedThumbHeight) <= 2,
					$"Uniform-item calibration did not match the absolute scrollbar model. Native: {metrics.ThumbHeight:F2}, expected: {expectedThumbHeight:F2}.");
			});

			var scenario = CreateScenario(CreateVariableHeightItems());
			var callbackCount = 0;
			var lastVisibleItemIndex = -1;

			await CreateHandlerAndAddToWindow(scenario.Page, async () =>
			{
				var recyclerView = GetRecyclerView(scenario.CollectionView);
				await recyclerView.WaitForLayoutOrNonZeroSize();
				await AssertEventually(
					() => recyclerView.FindViewHolderForAdapterPosition(0) is not null,
					message: "The variable-height CollectionView did not realize its first item.");

				Assert.True(recyclerView.VerticalScrollBarEnabled);
				AssertItem(recyclerView, 0, scenario.Items[0], ShortItemHeight);

				var attachedRecyclerView = recyclerView;
				var initialMetrics = GetMetrics(recyclerView);
				var initialOffset = initialMetrics.Offset;
				var expectedThumbHeight = GetExpectedThumbHeight(
					initialMetrics.Extent,
					scenario.Items,
					recyclerView.Context.Resources.DisplayMetrics.Density);

				scenario.CollectionView.Scrolled += OnScrolled;

				var firstDragOffset = GetVerticalOffset(recyclerView);
				DispatchUpwardDrag(recyclerView);
				await AssertEventually(
					() => recyclerView.ScrollState == RecyclerView.ScrollStateIdle &&
						GetVerticalOffset(recyclerView) > firstDragOffset,
					timeout: 3000,
					message: "The first touch drag did not settle at an increased native scroll offset.");

				var secondDragOffset = GetVerticalOffset(recyclerView);
				DispatchUpwardDrag(recyclerView);
				await AssertEventually(
					() => recyclerView.ScrollState == RecyclerView.ScrollStateIdle &&
						GetVerticalOffset(recyclerView) > secondDragOffset,
					timeout: 3000,
					message: "The second touch drag did not settle at an increased native scroll offset.");

				await AssertEventually(
					() => callbackCount > 0 && lastVisibleItemIndex >= 10,
					timeout: 3000,
					message: "CollectionView.Scrolled did not report that the tall-item group was reached.");

				Assert.Same(attachedRecyclerView, GetRecyclerView(scenario.CollectionView));
				Assert.True(GetVerticalOffset(recyclerView) > initialOffset);

				var layoutManager = Assert.IsAssignableFrom<LinearLayoutManager>(recyclerView.GetLayoutManager());
				var tallPosition = Math.Max(10, layoutManager.FindFirstVisibleItemPosition());
				Assert.InRange(tallPosition, 10, 19);
				AssertItem(recyclerView, tallPosition, scenario.Items[tallPosition], TallItemHeight);

				var postScrollMetrics = GetMetrics(recyclerView);
				Assert.True(
					Math.Abs(postScrollMetrics.ThumbHeight - initialMetrics.ThumbHeight) <= ThumbTolerance &&
						Math.Abs(postScrollMetrics.ThumbHeight - expectedThumbHeight) <= 2,
					$"Issue 28542: variable-height CollectionView native scrollbar thumb changed after scrolling. Initial: {initialMetrics.ThumbHeight:F2}, post-scroll: {postScrollMetrics.ThumbHeight:F2}, expected: {expectedThumbHeight:F2}.");

				scenario.CollectionView.Scrolled -= OnScrolled;
			});

			void OnScrolled(object _, ItemsViewScrolledEventArgs args)
			{
				callbackCount++;
				lastVisibleItemIndex = args.LastVisibleItemIndex;
			}
		}

		static (ContentPage Page, CollectionView CollectionView, IReadOnlyList<ScrollItem> Items) CreateScenario(IReadOnlyList<ScrollItem> items)
		{
			var collectionView = new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label
					{
						FontSize = 18,
						VerticalOptions = LayoutOptions.Center
					};
					label.SetBinding(Label.TextProperty, nameof(ScrollItem.Caption));

					var itemGrid = new Grid
					{
						Margin = new Thickness(0, 2),
						Padding = 12
					};
					itemGrid.SetBinding(VisualElement.HeightRequestProperty, nameof(ScrollItem.Height));
					itemGrid.SetBinding(VisualElement.BackgroundColorProperty, nameof(ScrollItem.Color));
					itemGrid.Add(label);
					return itemGrid;
				})
			};

			var statusLayout = new VerticalStackLayout { Spacing = 2 };
			statusLayout.Add(new Label { Text = "Reference captured", FontSize = 13 });
			statusLayout.Add(new Label { Text = "Scroll status", FontSize = 13 });
			statusLayout.Add(new Label { Text = "Thumb metrics", FontSize = 13 });
			statusLayout.Add(new Label { Text = "PASS:", FontSize = 18, FontAttributes = FontAttributes.Bold });

			var root = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				},
				Padding = 12,
				RowSpacing = 6
			};
			root.Add(new Label
			{
				Text = "CollectionView scrollbar sizing",
				FontSize = 20,
				FontAttributes = FontAttributes.Bold
			}, 0, 0);
			root.Add(collectionView, 0, 1);
			root.Add(statusLayout, 0, 2);

			return (new ContentPage { Content = root }, collectionView, items);
		}

		static IReadOnlyList<ScrollItem> CreateUniformItems()
		{
			var items = new List<ScrollItem>();
			for (var index = 0; index < 30; index++)
			{
				items.Add(new ScrollItem
				{
					Caption = $"Short item {index + 1}",
					Height = ShortItemHeight,
					Color = Colors.LightBlue
				});
			}

			return items;
		}

		static IReadOnlyList<ScrollItem> CreateVariableHeightItems()
		{
			var items = new List<ScrollItem>();
			for (var index = 1; index <= 10; index++)
			{
				items.Add(new ScrollItem
				{
					Caption = $"Short item {index}",
					Height = ShortItemHeight,
					Color = index % 2 == 0 ? Colors.LightBlue : Colors.LightCyan
				});
			}
			for (var index = 1; index <= 10; index++)
			{
				items.Add(new ScrollItem
				{
					Caption = $"Tall item {index}",
					Height = TallItemHeight,
					Color = index % 2 == 0 ? Colors.MistyRose : Colors.LightSalmon
				});
			}
			for (var index = 11; index <= 20; index++)
			{
				items.Add(new ScrollItem
				{
					Caption = $"Short item {index}",
					Height = ShortItemHeight,
					Color = index % 2 == 0 ? Colors.LightBlue : Colors.LightCyan
				});
			}

			return items;
		}

		static RecyclerView GetRecyclerView(CollectionView collectionView)
		{
			var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
			return Assert.IsAssignableFrom<RecyclerView>(handler.PlatformView);
		}

		static void AssertItem(RecyclerView recyclerView, int position, ScrollItem expectedItem, double expectedHeight)
		{
			var viewHolder = recyclerView.FindViewHolderForAdapterPosition(position);
			Assert.NotNull(viewHolder);
			var templatedViewHolder = Assert.IsType<TemplatedItemViewHolder>(viewHolder);
			Assert.Same(expectedItem, templatedViewHolder.View.BindingContext);

			var density = recyclerView.Context.Resources.DisplayMetrics.Density;
			var expectedPixelHeight = (expectedHeight + VerticalItemMargin) * density;
			Assert.True(
				Math.Abs(viewHolder.ItemView.Height - expectedPixelHeight) <= 2,
				$"Item {position} had native height {viewHolder.ItemView.Height}, expected {expectedPixelHeight:F2}.");
		}

		static (int Extent, int Offset, double ThumbHeight) GetMetrics(RecyclerView recyclerView)
		{
#pragma warning disable XAOBS001
			var extent = recyclerView.ComputeVerticalScrollExtent();
			var range = recyclerView.ComputeVerticalScrollRange();
			var offset = GetVerticalOffset(recyclerView);
#pragma warning restore XAOBS001
			Assert.True(extent > 0);
			Assert.True(range > extent);
			return (extent, offset, (double)extent * extent / range);
		}

		static int GetVerticalOffset(RecyclerView recyclerView)
		{
#pragma warning disable XAOBS001
			return recyclerView.ComputeVerticalScrollOffset();
#pragma warning restore XAOBS001
		}

		static double GetExpectedThumbHeight(int extent, IReadOnlyList<ScrollItem> items, float density)
		{
			var totalHeight = 0d;
			for (var index = 0; index < items.Count; index++)
				totalHeight += items[index].Height + VerticalItemMargin;

			return (double)extent * extent / (totalHeight * density);
		}

		static void DispatchUpwardDrag(RecyclerView recyclerView)
		{
			var touchSlop = AViewConfiguration.Get(recyclerView.Context).ScaledTouchSlop;
			var startX = recyclerView.Width / 2f;
			var startY = recyclerView.Height * 0.7f;
			var midpointY = recyclerView.Height * 0.5f;
			var endY = recyclerView.Height * 0.3f;
			Assert.True(startY - endY > touchSlop);

			var eventTime = SystemClock.UptimeMillis();
			var downTime = eventTime - 400;
			Dispatch(AMotionEventActions.Down, startY, downTime, downTime);
			Dispatch(AMotionEventActions.Move, midpointY, downTime, eventTime - 300);
			Dispatch(AMotionEventActions.Move, endY, downTime, eventTime - 200);
			Dispatch(AMotionEventActions.Move, endY, downTime, eventTime - 100);
			Dispatch(AMotionEventActions.Move, endY, downTime, eventTime - 50);
			Dispatch(AMotionEventActions.Up, endY, downTime, eventTime);

			void Dispatch(AMotionEventActions action, float y, long gestureStart, long eventTime)
			{
				var motionEvent = AMotionEvent.Obtain(gestureStart, eventTime, action, startX, y, 0);
				recyclerView.DispatchTouchEvent(motionEvent);
				motionEvent.Recycle();
			}
		}

		sealed class ScrollItem
		{
			public string Caption { get; set; }
			public double Height { get; set; }
			public Color Color { get; set; }
		}
	}
}
#endif


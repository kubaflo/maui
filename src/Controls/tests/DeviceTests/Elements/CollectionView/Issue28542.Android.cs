#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;
using AViewConfiguration = Android.Views.ViewConfiguration;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28542")]
	public class Issue28542 : ControlsHandlerTestBase
	{
		const string FailureSignature = "Issue28542 scrollbar thumb geometry was not stable for variable-height items:";
		const int ItemCount = 20;
		const double UniformContentHeight = ItemCount * 64;
		const double MixedContentHeight = 2090;

		[Fact]
		public async Task ScrollbarThumbRemainsStableForVariableHeightItems()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var uniformItems = CreateItems(useVariableHeights: false);
			var uniformCollection = CreatePage(uniformItems, out var uniformPage);
			ScrollMetrics uniformMetrics = default;
			float density = 0;
			double tolerance = 0;

			await CreateHandlerAndAddToWindow<IWindowHandler>(uniformPage, async _ =>
			{
				var recyclerView = GetRecyclerView(uniformCollection);
				await AssertEventually(() => recyclerView.Width > 0 && recyclerView.Height > 0);
				await AssertEventually(() => FindRealizedItem(recyclerView, 5) is not null);

				var itemSix = FindRealizedItem(recyclerView, 5);
				Assert.NotNull(itemSix);
				Assert.Equal(5, recyclerView.GetChildAdapterPosition(itemSix));

				density = recyclerView.Resources.DisplayMetrics.Density;
				Assert.True(density > 0);
				tolerance = Math.Max(4, density * 2);
				uniformMetrics = ReadScrollMetrics(recyclerView);

				double expectedRange = UniformContentHeight * density;
				double expectedThumb = CalculateThumbLength(uniformMetrics.Extent, expectedRange);
				Assert.True(
					Math.Abs(uniformMetrics.Range - expectedRange) <= tolerance &&
					Math.Abs(uniformMetrics.ThumbLength - expectedThumb) <= tolerance,
					$"Uniform-height scrollbar calibration failed: range={uniformMetrics.Range}, expectedRange={expectedRange:F2}, thumb={uniformMetrics.ThumbLength:F2}, expectedThumb={expectedThumb:F2}, tolerance={tolerance:F2}");
			});

			var mixedItems = CreateItems(useVariableHeights: true);
			var mixedCollection = CreatePage(mixedItems, out var mixedPage);
			ScrollMetrics initialMetrics = default;
			ScrollMetrics scrolledMetrics = default;
			int lastVisibleItemIndex = -1;
			int scrollEventCount = 0;
			mixedCollection.Scrolled += (_, args) =>
			{
				lastVisibleItemIndex = args.LastVisibleItemIndex;
				scrollEventCount++;
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(mixedPage, async _ =>
			{
				var recyclerView = GetRecyclerView(mixedCollection);
				await AssertEventually(() => recyclerView.Width > 0 && recyclerView.Height > 0);
				await AssertEventually(() => FindRealizedItem(recyclerView, 5) is not null);
				float mixedDensity = recyclerView.Resources.DisplayMetrics.Density;
				Assert.True(mixedDensity > 0);
				Assert.Equal(density, mixedDensity);

				var itemSix = FindRealizedItem(recyclerView, 5);
				Assert.NotNull(itemSix);
				Assert.Equal(5, recyclerView.GetChildAdapterPosition(itemSix));

				double expectedItemSixTop = 5 * 64 * density;
				Assert.True(Math.Abs(itemSix.Top - expectedItemSixTop) <= tolerance,
					$"Item 6 was not at its expected initial location: top={itemSix.Top}, expected={expectedItemSixTop:F2}");

				initialMetrics = ReadScrollMetrics(recyclerView);
				float startX = itemSix.Left + (itemSix.Width / 2f);
				float startY = itemSix.Top + (itemSix.Height / 2f);
				float dragDistance = Math.Min(recyclerView.RootView.Height * 0.45f, startY - (8 * density));
				Assert.True(dragDistance > AViewConfiguration.Get(recyclerView.Context).ScaledTouchSlop);

				lastVisibleItemIndex = -1;
				scrollEventCount = 0;
				DispatchUpwardDrag(recyclerView, startX, startY, dragDistance);
				await AssertEventually(() => scrollEventCount > 0, timeout: 5000,
					message: "CollectionView did not report scrolling after the first touch drag.");
				int firstDragEventCount = scrollEventCount;
				DispatchUpwardDrag(recyclerView, startX, startY, dragDistance);

				await AssertEventually(() => scrollEventCount > firstDragEventCount && lastVisibleItemIndex >= 12, timeout: 5000,
					message: "CollectionView did not report Item 13 visible after the touch drags.");
				await AssertEventually(() => FindRealizedItem(recyclerView, 12) is not null, timeout: 5000,
					message: "Item 13 was not realized after the touch drags.");

				var itemThirteen = FindRealizedItem(recyclerView, 12);
				Assert.NotNull(itemThirteen);
				Assert.Equal(12, recyclerView.GetChildAdapterPosition(itemThirteen));
				Assert.True(itemThirteen.Bottom > 0 && itemThirteen.Top < recyclerView.Height,
					"Item 13 was not within the native CollectionView viewport.");

				scrolledMetrics = ReadScrollMetrics(recyclerView);
			});

			double expectedMixedRange = MixedContentHeight * density;
			double expectedInitialThumb = CalculateThumbLength(initialMetrics.Extent, expectedMixedRange);
			double expectedScrolledThumb = CalculateThumbLength(scrolledMetrics.Extent, expectedMixedRange);
			bool geometryIsStable =
				Math.Abs(initialMetrics.Range - expectedMixedRange) <= tolerance &&
				Math.Abs(scrolledMetrics.Range - expectedMixedRange) <= tolerance &&
				Math.Abs(initialMetrics.ThumbLength - expectedInitialThumb) <= tolerance &&
				Math.Abs(scrolledMetrics.ThumbLength - expectedScrolledThumb) <= tolerance &&
				Math.Abs(initialMetrics.ThumbLength - scrolledMetrics.ThumbLength) <= tolerance;

			Assert.True(geometryIsStable,
				$"{FailureSignature} initialRange={initialMetrics.Range}, postDragRange={scrolledMetrics.Range}, expectedRange={expectedMixedRange:F2}, initialThumb={initialMetrics.ThumbLength:F2}, postDragThumb={scrolledMetrics.ThumbLength:F2}, expectedInitialThumb={expectedInitialThumb:F2}, expectedPostDragThumb={expectedScrolledThumb:F2}, tolerance={tolerance:F2}");
		}

		static CollectionView CreatePage(IReadOnlyList<VariableHeightItem> items, out ContentPage contentPage)
		{
			var collectionView = new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemGrid = new Grid { Padding = 10 };
					itemGrid.SetBinding(VisualElement.HeightRequestProperty, nameof(VariableHeightItem.Height));
					itemGrid.SetBinding(VisualElement.BackgroundColorProperty, nameof(VariableHeightItem.Color));

					var itemLabel = new Label
					{
						FontSize = 17,
						VerticalOptions = LayoutOptions.Center
					};
					itemLabel.SetBinding(Label.TextProperty, nameof(VariableHeightItem.Text));
					itemGrid.Add(itemLabel);
					return itemGrid;
				})
			};

			var rootGrid = new Grid
			{
				Padding = 12,
				RowSpacing = 6,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star }
				}
			};

			var headingLabel = new Label { Text = "Variable-height CollectionView", FontSize = 18 };
			var statusLabel = new Label { Text = "Scrollbar thumb size", FontSize = 16 };
			rootGrid.Add(headingLabel);
			rootGrid.Add(statusLabel);
			rootGrid.Add(collectionView);
			Grid.SetRow(statusLabel, 1);
			Grid.SetRow(collectionView, 2);
			contentPage = new ContentPage { Content = rootGrid };
			return collectionView;
		}

		static IReadOnlyList<VariableHeightItem> CreateItems(bool useVariableHeights)
		{
			double[] mixedHeights = [64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 260, 64, 150, 64, 300, 64, 180, 64, 240, 64];
			var items = new List<VariableHeightItem>(ItemCount);
			for (int index = 0; index < ItemCount; index++)
			{
				double height = useVariableHeights ? mixedHeights[index] : 64;
				Color color = index < 10
					? (index % 2 == 0 ? Colors.LightBlue : Colors.LightGreen)
					: ((index - 10) % 4) switch
					{
						0 => Colors.LightGoldenrodYellow,
						1 => Colors.LightBlue,
						2 => Colors.LightPink,
						_ => Colors.LightGreen
					};
				string size = height == 64 ? "short" : height >= 240 ? "tall" : "medium";
				items.Add(new VariableHeightItem($"Item {index + 1} {size}", height, color));
			}

			return items;
		}

		static RecyclerView GetRecyclerView(CollectionView collectionView)
		{
			var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
			return handler.PlatformView;
		}

		static AView FindRealizedItem(RecyclerView recyclerView, int position) =>
			recyclerView.GetLayoutManager().FindViewByPosition(position);

		static ScrollMetrics ReadScrollMetrics(RecyclerView recyclerView)
		{
			int extent = recyclerView.ComputeVerticalScrollExtent();
			int range = recyclerView.ComputeVerticalScrollRange();
			Assert.True(extent > 0);
			Assert.True(range >= extent);
			return new ScrollMetrics(extent, range, CalculateThumbLength(extent, range));
		}

		static double CalculateThumbLength(double extent, double range) => extent * extent / range;

		static void DispatchUpwardDrag(RecyclerView recyclerView, float startX, float startY, float distance)
		{
			long downTime = global::Android.OS.SystemClock.UptimeMillis();
			DispatchTouch(recyclerView, downTime, downTime, AMotionEventActions.Down, startX, startY);
			for (int step = 1; step <= 10; step++)
			{
				float y = startY - (distance * step / 10);
				DispatchTouch(recyclerView, downTime, downTime + (step * 16), AMotionEventActions.Move, startX, y);
			}
			DispatchTouch(recyclerView, downTime, downTime + 176, AMotionEventActions.Up, startX, startY - distance);
		}

		static void DispatchTouch(RecyclerView recyclerView, long downTime, long eventTime, AMotionEventActions action, float x, float y)
		{
			var motionEvent = AMotionEvent.Obtain(downTime, eventTime, action, x, y, 0);
			recyclerView.DispatchTouchEvent(motionEvent);
			motionEvent.Recycle();
		}

		readonly record struct ScrollMetrics(int Extent, int Range, double ThumbLength);

		sealed record VariableHeightItem(string Text, double Height, Color Color);
	}
}
#endif


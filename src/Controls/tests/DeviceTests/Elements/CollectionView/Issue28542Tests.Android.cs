#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Android.OS;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using MotionEvent = Android.Views.MotionEvent;
using MotionEventActions = Android.Views.MotionEventActions;
using ViewConfiguration = Android.Views.ViewConfiguration;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28542")]
	public class Issue28542 : ControlsHandlerTestBase
	{
		const double ViewportHeight = 320;
		const double ShortItemHeight = 40;
		const double TallItemHeight = 240;
		const int TallItemPosition = 8;

		[Fact]
		public async Task ScrollbarThumbRemainsStableWithVariableHeightItems()
		{
			SetupBuilder();

			var uniformItems = CreateItems(useVariableHeights: false);
			var uniformCollectionView = CreatePage(uniformItems, out var uniformPage);
			var uniformResult = await ExerciseCollectionView(uniformPage, uniformCollectionView, uniformItems, verifyTallItem: false);

			Assert.True(
				Math.Abs(uniformResult.InitialFraction - uniformResult.ExpectedFraction) <= uniformResult.Tolerance &&
				Math.Abs(uniformResult.FinalFraction - uniformResult.ExpectedFraction) <= uniformResult.Tolerance,
				$"Uniform-height control did not establish the native scrollbar oracle; initial={uniformResult.InitialFraction:F6}, final={uniformResult.FinalFraction:F6}, expected={uniformResult.ExpectedFraction:F6}, tolerance={uniformResult.Tolerance:F6}");

			var variableItems = CreateItems(useVariableHeights: true);
			var variableCollectionView = CreatePage(variableItems, out var variablePage);
			var variableResult = await ExerciseCollectionView(variablePage, variableCollectionView, variableItems, verifyTallItem: true);

			var matchesExpectedFraction =
				Math.Abs(variableResult.FinalFraction - variableResult.ExpectedFraction) <= variableResult.Tolerance;
			var remainsStable =
				Math.Abs(variableResult.FinalFraction - variableResult.InitialFraction) <= variableResult.Tolerance;

			Assert.True(
				matchesExpectedFraction && remainsStable,
				$"Issue 28542 scrollbar thumb fraction changed for variable-height items; initial={variableResult.InitialFraction:F6}, final={variableResult.FinalFraction:F6}, expected={variableResult.ExpectedFraction:F6}, initialRange={variableResult.InitialRange}, finalRange={variableResult.FinalRange}, initialExtent={variableResult.InitialExtent}, finalExtent={variableResult.FinalExtent}, offset={variableResult.FinalOffset}, tolerance={variableResult.Tolerance:F6}");
		}

		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});
		}

		async Task<ScrollResult> ExerciseCollectionView(
			ContentPage page,
			CollectionView collectionView,
			ObservableCollection<RowItem> items,
			bool verifyTallItem)
		{
			var result = new ScrollResult
			{
				InitialFraction = double.NaN,
				FinalFraction = double.NaN,
				ExpectedFraction = double.NaN,
				Tolerance = double.NaN,
				InitialRange = -1,
				FinalRange = -1,
				InitialExtent = -1,
				FinalExtent = -1,
				FinalOffset = -1
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var recyclerView = handler.PlatformView;

				await recyclerView.WaitForLayoutOrNonZeroSize();
				await AssertEventually(
					() => IsExpectedItemLaidOut(recyclerView, 0, items[0]),
					timeout: 3000,
					message: "The expected first short item was not laid out");

				var initialMetrics = await WaitForStableMetrics(recyclerView);
				Assert.True(initialMetrics.IsValid, $"Initial native scroll metrics were invalid: extent={initialMetrics.Extent}, range={initialMetrics.Range}, offset={initialMetrics.Offset}");
				Assert.True(initialMetrics.Range > initialMetrics.Extent, $"CollectionView was not scrollable: extent={initialMetrics.Extent}, range={initialMetrics.Range}");
				Assert.Equal(0, initialMetrics.Offset);

				var callbackCount = -1;
				var observedOffset = -1;
				var tallItemObserved = false;
				void OnScrolled(object sender, ItemsViewScrolledEventArgs args)
				{
					callbackCount = callbackCount < 0 ? 1 : callbackCount + 1;
					observedOffset = recyclerView.ComputeVerticalScrollOffset();
					tallItemObserved |= verifyTallItem &&
						IsExpectedItemLaidOut(recyclerView, TallItemPosition, items[TallItemPosition]);
				}

				collectionView.Scrolled += OnScrolled;
				try
				{
					var firstDragTime = SystemClock.UptimeMillis();
					for (var drag = 0; drag < 3; drag++)
					{
						DispatchUpwardDrag(recyclerView, firstDragTime + (drag * 1200L));
						tallItemObserved |= verifyTallItem &&
							IsExpectedItemLaidOut(recyclerView, TallItemPosition, items[TallItemPosition]);
					}

					var callbackObserved = await Wait(
						() => callbackCount > 0 && observedOffset > 0,
						timeout: 3000);
					Assert.True(
						callbackObserved,
						$"No positive MAUI Scrolled callback was observed: callbacks={callbackCount}, observedOffset={observedOffset}");

					var nativeOffsetObserved = await Wait(
						() => recyclerView.ComputeVerticalScrollOffset() > 0,
						timeout: 3000);
					Assert.True(
						nativeOffsetObserved,
						$"The native RecyclerView did not acquire a positive scroll offset: offset={recyclerView.ComputeVerticalScrollOffset()}");

					if (verifyTallItem)
					{
						var tallItemWasLaidOut = await Wait(
							() =>
							{
								tallItemObserved |= IsExpectedItemLaidOut(
									recyclerView,
									TallItemPosition,
									items[TallItemPosition]);
								return tallItemObserved;
							},
							timeout: 3000);
						Assert.True(
							tallItemWasLaidOut,
							$"The expected tall item at adapter position 8 was not laid out: offset={recyclerView.ComputeVerticalScrollOffset()}");
					}

					var finalMetrics = await WaitForStableMetrics(recyclerView);
					var density = recyclerView.Context.Resources.DisplayMetrics.Density;
					Assert.True(density > 0, $"Android display density was invalid: {density}");
					var expectedViewportExtent = ViewportHeight * density;
					Assert.True(
						Math.Abs(initialMetrics.Extent - expectedViewportExtent) <= 2,
						$"Initial native viewport extent did not match 320 device-independent units: extent={initialMetrics.Extent}, expected={expectedViewportExtent:F2}");
					Assert.True(
						Math.Abs(finalMetrics.Extent - expectedViewportExtent) <= 2,
						$"Final native viewport extent did not match 320 device-independent units: extent={finalMetrics.Extent}, expected={expectedViewportExtent:F2}");

					var totalHeight = 0d;
					for (var index = 0; index < items.Count; index++)
						totalHeight += items[index].Height;

					var expectedRange = totalHeight * density;
					var expectedFraction = finalMetrics.Extent / expectedRange;
					var tolerance = Math.Max(0.005, 2d / expectedRange);

					result = new ScrollResult
					{
						InitialFraction = (double)initialMetrics.Extent / initialMetrics.Range,
						FinalFraction = (double)finalMetrics.Extent / finalMetrics.Range,
						ExpectedFraction = expectedFraction,
						Tolerance = tolerance,
						InitialRange = initialMetrics.Range,
						FinalRange = finalMetrics.Range,
						InitialExtent = initialMetrics.Extent,
						FinalExtent = finalMetrics.Extent,
						FinalOffset = finalMetrics.Offset
					};
				}
				finally
				{
					collectionView.Scrolled -= OnScrolled;
				}
			});

			Assert.False(
				double.IsNaN(result.FinalFraction),
				"The attached CollectionView exercise did not produce native scrollbar metrics");
			return result;
		}

		static CollectionView CreatePage(ObservableCollection<RowItem> items, out ContentPage page)
		{
			var collectionView = new CollectionView
			{
				AutomationId = "Issue28542Collection",
				ItemsSource = items,
				ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				VerticalScrollBarVisibility = ScrollBarVisibility.Always,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemGrid = new Grid();
					itemGrid.SetBinding(VisualElement.HeightRequestProperty, nameof(RowItem.Height));

					var itemLabel = new Label
					{
						Margin = new Thickness(12, 0),
						VerticalOptions = LayoutOptions.Center
					};
					itemLabel.SetBinding(Label.TextProperty, nameof(RowItem.Text));
					itemGrid.Add(itemLabel);
					return itemGrid;
				})
			};

			var root = new Grid
			{
				Padding = 12,
				RowSpacing = 6,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(new GridLength(ViewportHeight))
				}
			};

			root.Add(new Label
			{
				Text = "Variable-height CollectionView scrollbar",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold
			});

			var buttons = new Grid
			{
				ColumnSpacing = 6,
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};
			buttons.Add(new Button { Text = "Capture initial size" });
			buttons.Add(new Button { Text = "Check scrollbar size" }, 1);
			root.Add(buttons, row: 1);
			root.Add(new Label { Text = "Metrics" }, row: 2);

			var status = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};
			status.Add(new Label
			{
				Text = "Scroll position",
				FontAttributes = FontAttributes.Bold
			});
			status.Add(new Label
			{
				Text = "Thumb fraction",
				FontAttributes = FontAttributes.Bold,
				HorizontalTextAlignment = TextAlignment.End
			}, 1);
			root.Add(status, row: 3);
			root.Add(collectionView, row: 4);

			page = new ContentPage { Content = root };
			return collectionView;
		}

		static ObservableCollection<RowItem> CreateItems(bool useVariableHeights)
		{
			var items = new ObservableCollection<RowItem>();
			for (var index = 1; index <= 8; index++)
				items.Add(new RowItem
				{
					Text = $"Short item {index}",
					Height = ShortItemHeight
				});

			for (var index = 1; index <= 6; index++)
			{
				var height = useVariableHeights ? TallItemHeight : ShortItemHeight;
				items.Add(new RowItem
				{
					Text = $"Tall item {index}",
					Height = height
				});
			}

			return items;
		}

		static bool IsExpectedItemLaidOut(RecyclerView recyclerView, int position, RowItem expectedItem)
		{
			var holder = recyclerView.FindViewHolderForAdapterPosition(position);
			if (holder is not TemplatedItemViewHolder templatedHolder ||
				templatedHolder.View is not Microsoft.Maui.Controls.View itemView ||
				itemView.BindingContext is not RowItem actualItem)
			{
				return false;
			}

			var expectedHeight = expectedItem.Height *
				recyclerView.Context.Resources.DisplayMetrics.Density;
			return ReferenceEquals(actualItem, expectedItem) &&
				holder.ItemView.IsLaidOut &&
				holder.ItemView.Width > 0 &&
				Math.Abs(holder.ItemView.Height - expectedHeight) <= 2 &&
				holder.ItemView.Top < recyclerView.Height &&
				holder.ItemView.Bottom > 0;
		}

		static async Task<ScrollMetrics> WaitForStableMetrics(RecyclerView recyclerView)
		{
			var previous = new ScrollMetrics { Offset = -1 };
			var settled = new ScrollMetrics { Offset = -1 };
			var stableSamples = 0;

			var metricsSettled = await Wait(
				() =>
				{
					var current = GetMetrics(recyclerView);
					if (current.IsValid && current.HasSameValues(previous))
						stableSamples++;
					else
						stableSamples = 0;

					previous = current;
					settled = current;
					return stableSamples >= 2;
				},
				timeout: 3000);
			Assert.True(
				metricsSettled,
				$"Native scroll metrics did not settle: extent={settled.Extent}, range={settled.Range}, offset={settled.Offset}");

			return settled;
		}

		static ScrollMetrics GetMetrics(RecyclerView recyclerView) =>
			new ScrollMetrics
			{
				Extent = recyclerView.ComputeVerticalScrollExtent(),
				Range = recyclerView.ComputeVerticalScrollRange(),
				Offset = recyclerView.ComputeVerticalScrollOffset()
			};

		static void DispatchUpwardDrag(RecyclerView recyclerView, long downTime)
		{
			var touchSlop = ViewConfiguration.Get(recyclerView.Context).ScaledTouchSlop;
			var windowHeight = recyclerView.RootView.Height;
			var travel = Math.Min(
				recyclerView.Height * 0.7f,
				Math.Max(windowHeight * 0.3f, touchSlop * 2f));
			var startX = recyclerView.Width / 2f;
			var startY = recyclerView.Height * 0.8f;
			var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down, startX, startY, 0);
			_ = recyclerView.DispatchTouchEvent(down);
			down.Recycle();

			for (var step = 1; step <= 4; step++)
			{
				var eventTime = downTime + 250 + (step * 160);
				var moveY = startY - (travel * step / 4f);
				var move = MotionEvent.Obtain(downTime, eventTime, MotionEventActions.Move, startX, moveY, 0);
				_ = recyclerView.DispatchTouchEvent(move);
				move.Recycle();
			}

			var up = MotionEvent.Obtain(downTime, downTime + 1170, MotionEventActions.Up, startX, startY - travel, 0);
			_ = recyclerView.DispatchTouchEvent(up);
			up.Recycle();
		}

		sealed class ScrollMetrics
		{
			public int Extent { get; set; }
			public int Range { get; set; }
			public int Offset { get; set; }
			public bool IsValid => Extent > 0 && Range > 0 && Offset >= 0;

			public bool HasSameValues(ScrollMetrics other) =>
				Extent == other.Extent && Range == other.Range && Offset == other.Offset;
		}

		sealed class ScrollResult
		{
			public double InitialFraction { get; set; }
			public double FinalFraction { get; set; }
			public double ExpectedFraction { get; set; }
			public double Tolerance { get; set; }
			public int InitialRange { get; set; }
			public int FinalRange { get; set; }
			public int InitialExtent { get; set; }
			public int FinalExtent { get; set; }
			public int FinalOffset { get; set; }
		}

		sealed class RowItem
		{
			public string Text { get; set; } = string.Empty;
			public double Height { get; set; }
		}
	}
}
#endif


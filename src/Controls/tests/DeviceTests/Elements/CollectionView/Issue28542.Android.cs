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
	[Category(TestCategory.CollectionView)]
	public class Issue28542 : ControlsHandlerTestBase
	{
		const double VariableContentHeight = (5 * 180) + (30 * 48);

		[Fact]
		[Category("Issue28542")]
		public async Task ScrollbarThumbRetainsArrangementDerivedHeight()
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

			var variableItems = new List<VariableHeightItem>();
			variableItems.AddRange(CreateItems(5, 180, Colors.LightBlue, "Tall item"));
			variableItems.AddRange(CreateItems(30, 48, Colors.LightGoldenrodYellow, "Short item"));
			var variableScene = CreateScene(variableItems);

			await CreateHandlerAndAddToWindow<IWindowHandler>(variableScene.Page, async _ =>
			{
				var recyclerView = (RecyclerView)variableScene.CollectionView.Handler.PlatformView;
				await recyclerView.WaitForLayoutOrNonZeroSize();
				var attachedRecyclerView = recyclerView;
				var layoutManager = Assert.IsType<LinearLayoutManager>(recyclerView.GetLayoutManager());

				long downTime = SystemClock.UptimeMillis();
				int windowHeight = recyclerView.RootView.Height;
				Assert.True(windowHeight > 0, "The attached Android window must have a measured height.");
				float x = recyclerView.Width / 2f;
				float startY = recyclerView.Height * 0.85f;
				float firstY = startY - Math.Max(AViewConfiguration.Get(recyclerView.Context).ScaledTouchSlop * 2, windowHeight * 0.08f);

				Dispatch(recyclerView, downTime, downTime, AMotionEventActions.Down, x, startY);
				Dispatch(recyclerView, downTime, downTime + 16, AMotionEventActions.Move, x, firstY);

				int initialExtent = recyclerView.ComputeVerticalScrollExtent();
				int initialRange = recyclerView.ComputeVerticalScrollRange();
				int initialOffset = recyclerView.ComputeVerticalScrollOffset();
				Assert.True(initialExtent > 0, "The native RecyclerView must report a positive scroll extent.");
				Assert.True(initialRange > initialExtent, "The native RecyclerView must report scrollable content.");
				Assert.True(layoutManager.FindFirstVisibleItemPosition() < 5, "The initial scrollbar measurement must be within the tall-item range.");

				using var listener = new ScrollObservationListener();
				recyclerView.AddOnScrollListener(listener);
				try
				{
					float endY = startY - (windowHeight * 1.6f);
					for (var step = 1; step <= 4; step++)
					{
						float moveY = firstY + ((endY - firstY) * step / 4);
						Dispatch(recyclerView, downTime, downTime + 16 + (step * 16), AMotionEventActions.Move, x, moveY);
					}
					Dispatch(recyclerView, downTime, downTime + 96, AMotionEventActions.Up, x, endY);

					await AssertEventually(() => listener.CallbackCount > 0, message: "No native RecyclerView scroll callback was observed.");
					await AssertEventually(() => listener.Offset > 0 && listener.Offset != initialOffset, message: "The native RecyclerView offset did not change.");
					await AssertEventually(() => listener.FirstVisiblePosition >= 5, message: "The short-item range was not reached.");
					Assert.InRange(listener.FirstVisiblePosition, 5, variableItems.Count - 1);
					var firstVisibleItem = variableItems[listener.FirstVisiblePosition];
					Assert.StartsWith("Short item", firstVisibleItem.Text, StringComparison.Ordinal);
					Assert.Equal(48, firstVisibleItem.Height);

					Assert.Same(attachedRecyclerView, variableScene.CollectionView.Handler.PlatformView);

					int finalExtent = recyclerView.ComputeVerticalScrollExtent();
					int finalRange = recyclerView.ComputeVerticalScrollRange();
					Assert.True(finalExtent > 0, "The native RecyclerView must retain a positive scroll extent.");
					Assert.True(finalRange > finalExtent, "The native RecyclerView must retain scrollable content.");

					int initialThumbHeight = GetThumbHeight(initialExtent, initialRange);
					int finalThumbHeight = GetThumbHeight(finalExtent, finalRange);
					int expectedHeight = GetExpectedThumbHeight(finalExtent, recyclerView.Resources.DisplayMetrics.Density, VariableContentHeight);
					int tolerance = GetTolerance(expectedHeight);
					bool retainedExpectedHeight =
						Math.Abs(initialThumbHeight - expectedHeight) <= tolerance &&
						Math.Abs(finalThumbHeight - expectedHeight) <= tolerance &&
						Math.Abs(initialThumbHeight - finalThumbHeight) <= tolerance;

					Assert.True(
						retainedExpectedHeight,
						$"Issue28542 scrollbar thumb did not retain the arrangement-derived height: initial={initialThumbHeight}px, final={finalThumbHeight}px, expected={expectedHeight}px, tolerance={tolerance}px, initialExtent={initialExtent}px, finalExtent={finalExtent}px, initialRange={initialRange}px, finalRange={finalRange}px, offset={listener.Offset}, firstVisible={listener.FirstVisiblePosition}.");
				}
				finally
				{
					recyclerView.RemoveOnScrollListener(listener);
				}
			});
		}

		static (ContentPage Page, CollectionView CollectionView) CreateScene(IReadOnlyList<VariableHeightItem> items)
		{
			var statusLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "Variable-height items"
			};
			var collectionView = new CollectionView
			{
				ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = 18,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					};
					itemLabel.SetBinding(Label.TextProperty, nameof(VariableHeightItem.Text));

					var itemGrid = new Grid();
					itemGrid.SetBinding(VisualElement.HeightRequestProperty, nameof(VariableHeightItem.Height));
					itemGrid.SetBinding(VisualElement.BackgroundColorProperty, nameof(VariableHeightItem.BackgroundColor));
					itemGrid.Add(itemLabel);
					return itemGrid;
				})
			};
			Grid.SetRow(collectionView, 1);

			var root = new Grid
			{
				Padding = new Thickness(12, 8),
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			root.Add(statusLabel);
			root.Add(collectionView);

			return (new ContentPage { Content = root }, collectionView);
		}

		static List<VariableHeightItem> CreateItems(int count, double height, Microsoft.Maui.Graphics.Color color, string prefix)
		{
			var items = new List<VariableHeightItem>(count);
			for (var index = 1; index <= count; index++)
			{
				items.Add(new VariableHeightItem
				{
					Text = $"{prefix} {index}",
					Height = height,
					BackgroundColor = color
				});
			}
			return items;
		}

		static void Dispatch(RecyclerView recyclerView, long downTime, long eventTime, AMotionEventActions action, float x, float y)
		{
			var motionEvent = AMotionEvent.Obtain(downTime, eventTime, action, x, y, 0);
			_ = recyclerView.DispatchTouchEvent(motionEvent);
			motionEvent.Recycle();
		}

		static int GetThumbHeight(int extent, int range) =>
			(int)Math.Round((double)extent * extent / range);

		static int GetExpectedThumbHeight(int extent, float density, double contentHeight) =>
			(int)Math.Round((double)extent * extent / (contentHeight * density));

		static int GetTolerance(int expectedHeight) =>
			Math.Max(8, (int)Math.Ceiling(expectedHeight * 0.12));

		sealed class ScrollObservationListener : RecyclerView.OnScrollListener
		{
			public int CallbackCount { get; private set; }
			public int Offset { get; private set; } = -1;
			public int FirstVisiblePosition { get; private set; } = -1;

			public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
			{
				base.OnScrolled(recyclerView, dx, dy);
				CallbackCount++;
				Offset = recyclerView.ComputeVerticalScrollOffset();
				if (recyclerView.GetLayoutManager() is LinearLayoutManager layoutManager)
					FirstVisiblePosition = layoutManager.FindFirstVisibleItemPosition();
			}
		}

		sealed class VariableHeightItem
		{
			public string Text { get; init; }
			public double Height { get; init; }
			public Microsoft.Maui.Graphics.Color BackgroundColor { get; init; }
		}
	}
}
#endif


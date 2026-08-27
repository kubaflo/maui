#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using MauiWindow = Microsoft.Maui.Controls.Window;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28542")]
	public class Issue28542 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ScrollbarThumbRepresentsMixedHeightContent()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<MauiWindow, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			const double itemHeightTolerance = 2;
			const double fractionTolerance = 0.02;

			var homogeneousItems = CreateItems(shortItemCount: 20);
			var homogeneousResult = await MeasureScrollbarAsync(
				homogeneousItems,
				requiredVisibleIndex: 8,
				requiredVisibleHeight: 72);
			var density = homogeneousResult.RecyclerView.Resources.DisplayMetrics.Density;
			var homogeneousExpected = homogeneousResult.InitialExtent / (20 * 72 * density);

			Assert.InRange(
				Math.Abs(homogeneousResult.InitialFraction - homogeneousExpected),
				0,
				fractionTolerance);
			Assert.InRange(
				Math.Abs(homogeneousResult.FinalFraction - homogeneousExpected),
				0,
				fractionTolerance);

			var mixedItems = CreateItems(shortItemCount: 8);
			var mixedResult = await MeasureScrollbarAsync(
				mixedItems,
				requiredVisibleIndex: 8,
				requiredVisibleHeight: 280);
			var expectedItemHeight = 280 * density;

			Assert.Same(mixedResult.RecyclerView, mixedResult.AttachedRecyclerView);
			Assert.Equal("Item 9 - 280 units", mixedResult.VisibleItemText);
			Assert.InRange(
				Math.Abs(mixedResult.VisibleItemHeight - expectedItemHeight),
				0,
				itemHeightTolerance);

			var expectedMixedFraction = mixedResult.InitialExtent / ((8 * 72 + 12 * 280) * density);
			var fractionChange = Math.Abs(mixedResult.InitialFraction - mixedResult.FinalFraction);
			var finalError = Math.Abs(mixedResult.FinalFraction - expectedMixedFraction);

			Assert.True(
				fractionChange <= fractionTolerance && finalError <= fractionTolerance,
				$"CollectionView scrollbar thumb fraction changed for mixed-height items: initial={mixedResult.InitialFraction:F4}, post-drag={mixedResult.FinalFraction:F4}, expected={expectedMixedFraction:F4}, tolerance={fractionTolerance:F4}");

			async Task<(
				RecyclerView RecyclerView,
				RecyclerView AttachedRecyclerView,
				int InitialExtent,
				double InitialFraction,
				double FinalFraction,
				int VisibleItemHeight,
				string VisibleItemText)> MeasureScrollbarAsync(
				IReadOnlyList<object> items,
				int requiredVisibleIndex,
				double requiredVisibleHeight)
			{
				var collectionView = CreateCollectionView(items);
				var instruction = new Label { Text = "CollectionView items" };
				var footer = new Label
				{
					Text = "Scroll to view more items.",
					FontAttributes = FontAttributes.Bold
				};
				var root = new Grid
				{
					Padding = 12,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Star),
						new RowDefinition(GridLength.Auto)
					},
					RowSpacing = 8
				};

				Grid.SetRow(instruction, 0);
				Grid.SetRow(collectionView, 1);
				Grid.SetRow(footer, 2);
				root.Children.Add(instruction);
				root.Children.Add(collectionView);
				root.Children.Add(footer);

				var page = new ContentPage { Content = root };
				var lastVisibleIndex = -1;
				var scrolled = false;
				collectionView.Scrolled += (_, args) =>
				{
					scrolled = true;
					lastVisibleIndex = args.LastVisibleItemIndex;
				};

				var measurementCaptured = false;
				(
					RecyclerView RecyclerView,
					RecyclerView AttachedRecyclerView,
					int InitialExtent,
					double InitialFraction,
					double FinalFraction,
					int VisibleItemHeight,
					string VisibleItemText) result = (null, null, -1, -1, -1, -1, string.Empty);
				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
				{
					await AssertEventually(
						() => collectionView.Handler?.PlatformView is RecyclerView recyclerView &&
							recyclerView.Width > 0 &&
							recyclerView.Height > 0 &&
							recyclerView.ComputeVerticalScrollExtent() > 0 &&
							recyclerView.ComputeVerticalScrollRange() > 0,
						timeout: 5000,
						message: "CollectionView did not attach with valid Android scroll metrics.");

					var recyclerView = Assert.IsAssignableFrom<RecyclerView>(collectionView.Handler.PlatformView);
					var attachedRecyclerView = recyclerView;
					var location = new int[2];
					recyclerView.GetLocationOnScreen(location);
					Assert.True(recyclerView.IsAttachedToWindow);
					Assert.True(recyclerView.Width > 0 && recyclerView.Height > 0);
					Assert.True(location[0] >= 0 && location[1] >= 0);

					var density = recyclerView.Resources.DisplayMetrics.Density;
					Assert.True(density > 0);
					await AssertEventually(
						() => ItemHasExpectedIdentityAndHeight(
							recyclerView,
							0,
							"Item 1 - 72 units",
							72,
							density),
						timeout: 5000,
						message: "The initial short CollectionView item did not finish layout.");

					var initialExtent = recyclerView.ComputeVerticalScrollExtent();
					var initialRange = recyclerView.ComputeVerticalScrollRange();
					var initialFraction = (double)initialExtent / initialRange;

					scrolled = false;
					lastVisibleIndex = -1;
					DispatchUpwardDrag(recyclerView);

					await AssertEventually(
						() => scrolled,
						timeout: 5000,
						message: "CollectionView did not report the touch-driven scroll.");
					await AssertEventually(
						() => lastVisibleIndex >= requiredVisibleIndex &&
							ItemHasExpectedIdentityAndHeight(
								recyclerView,
								requiredVisibleIndex,
								$"Item {requiredVisibleIndex + 1} - {requiredVisibleHeight} units",
								requiredVisibleHeight,
								density),
						timeout: 5000,
						message: $"CollectionView did not visibly lay out item {requiredVisibleIndex + 1}.");

					var holder = recyclerView.FindViewHolderForAdapterPosition(requiredVisibleIndex);
					Assert.NotNull(holder);
					var itemText = FindText(holder.ItemView);
					Assert.NotNull(itemText);
					Assert.Same(attachedRecyclerView, collectionView.Handler.PlatformView);
					Assert.True(recyclerView.IsAttachedToWindow);

					var finalExtent = recyclerView.ComputeVerticalScrollExtent();
					var finalRange = recyclerView.ComputeVerticalScrollRange();
					Assert.True(finalExtent > 0 && finalRange > 0);
					result = (
						recyclerView,
						attachedRecyclerView,
						initialExtent,
						initialFraction,
						(double)finalExtent / finalRange,
						holder.ItemView.Height,
						itemText.Text);
					measurementCaptured = true;
				});

				Assert.True(measurementCaptured, "CollectionView scroll measurements were not captured.");
				return result;
			}
		}

		static bool ItemHasExpectedIdentityAndHeight(
			RecyclerView recyclerView,
			int position,
			string expectedText,
			double expectedHeight,
			float density)
		{
			var holder = recyclerView.FindViewHolderForAdapterPosition(position);
			if (holder is null)
				return false;

			var text = FindText(holder.ItemView);
			return text is not null &&
				text.Text == expectedText &&
				Math.Abs(holder.ItemView.Height - expectedHeight * density) <= 2;
		}

		static CollectionView CreateCollectionView(IReadOnlyList<object> items)
		{
			return new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLayout = new Grid { Padding = new Thickness(12, 4) };
					itemLayout.SetBinding(VisualElement.HeightRequestProperty, "Height");
					itemLayout.SetBinding(VisualElement.BackgroundColorProperty, "Color");

					var itemLabel = new Label { VerticalOptions = LayoutOptions.Center };
					itemLabel.SetBinding(Label.TextProperty, "Text");
					itemLayout.Children.Add(itemLabel);
					return itemLayout;
				})
			};
		}

		static IReadOnlyList<object> CreateItems(int shortItemCount)
		{
			var items = new List<object>();
			for (var index = 1; index <= 20; index++)
			{
				var height = index <= shortItemCount ? 72 : 280;
				var color = index <= shortItemCount ? Colors.LightBlue : Colors.LightSalmon;
				items.Add(new
				{
					Text = $"Item {index} - {height} units",
					Height = height,
					Color = color
				});
			}

			return items;
		}

		static void DispatchUpwardDrag(RecyclerView recyclerView)
		{
			var downTime = global::Android.OS.SystemClock.UptimeMillis();
			var x = recyclerView.Width / 2f;
			var startY = recyclerView.Height * 0.75f;
			var middleY = startY - recyclerView.Height * 0.18f;
			var endY = middleY - recyclerView.Height * 0.18f;

			Dispatch(MotionEventActions.Down, downTime, downTime, startY);
			Dispatch(MotionEventActions.Move, downTime, downTime + 300, middleY);
			Dispatch(MotionEventActions.Move, downTime, downTime + 600, endY);
			Dispatch(MotionEventActions.Up, downTime, downTime + 620, endY);

			void Dispatch(MotionEventActions action, long gestureStart, long eventTime, float y)
			{
				using var motionEvent = MotionEvent.Obtain(gestureStart, eventTime, action, x, y, 0);
				recyclerView.DispatchTouchEvent(motionEvent);
			}
		}

		static TextView FindText(global::Android.Views.View view)
		{
			if (view is TextView textView)
				return textView;

			if (view is ViewGroup group)
			{
				for (var index = 0; index < group.ChildCount; index++)
				{
					var text = FindText(group.GetChildAt(index));
					if (text is not null)
						return text;
				}
			}

			return null;
		}

	}
}
#endif


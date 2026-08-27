#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AInputSourceType = Android.Views.InputSourceType;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32702")]
	public class Issue32702 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DragAndDropRecognizersDoNotPreventItemSelection()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var expectedItem = new string("Tap this item".ToCharArray());
			var itemLabels = new List<Label>();
			var selectionIndex = -1;
			var selectionChangedRaised = false;

			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { expectedItem },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					label.GestureRecognizers.Add(new DragGestureRecognizer());
					label.GestureRecognizers.Add(new DropGestureRecognizer());
					itemLabels.Add(label);
					return label;
				})
			};

			collectionView.SelectionChanged += (_, args) =>
			{
				selectionChangedRaised = true;
				selectionIndex = args.CurrentSelection.Count == 1 &&
					ReferenceEquals(args.CurrentSelection[0], expectedItem) ? 0 : -2;
			};

			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto }
				}
			};

			grid.Add(new Label { Text = "CollectionView drag and drop selection" });
			grid.Add(new Label { Text = "Selection has not been checked" }, 0, 1);
			grid.Add(collectionView, 0, 2);
			grid.Add(new Button { Text = "Check selection" }, 0, 3);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var handler = collectionView.Handler as CollectionViewHandler;
				Assert.NotNull(handler);

				var recyclerView = handler.PlatformView;
				await recyclerView.WaitForLayoutOrNonZeroSize();
				await AssertEventually(
					() => recyclerView.FindViewHolderForAdapterPosition(0) is not null &&
						itemLabels.Count == 1 &&
						itemLabels[0].Handler?.PlatformView is ATextView textView &&
						textView.Width > 0 &&
						textView.Height > 0,
					message: "The sole CollectionView item was not realized and measured.");

				var adapter = recyclerView.GetAdapter();
				Assert.NotNull(adapter);
				Assert.Equal(1, adapter.ItemCount);

				var viewHolder = recyclerView.FindViewHolderForAdapterPosition(0);
				Assert.NotNull(viewHolder);
				Assert.Equal(0, viewHolder.BindingAdapterPosition);
				Assert.True(viewHolder.ItemView.Width > 0);
				Assert.True(viewHolder.ItemView.Height > 0);
				Assert.True(viewHolder.ItemView.IsShown);

				var itemLabel = Assert.Single(itemLabels);
				Assert.Same(expectedItem, itemLabel.BindingContext);
				Assert.False(itemLabel.IsSet(VisualElement.StyleProperty));
				Assert.Equal(-1d, itemLabel.WidthRequest);
				Assert.Equal(-1d, itemLabel.HeightRequest);
				Assert.Collection(
					itemLabel.GestureRecognizers,
					recognizer => Assert.IsType<DragGestureRecognizer>(recognizer),
					recognizer => Assert.IsType<DropGestureRecognizer>(recognizer));

				var nativeLabel = itemLabel.Handler.PlatformView as ATextView;
				Assert.NotNull(nativeLabel);
				Assert.True(nativeLabel.IsShown);
				Assert.True(IsDescendantOf(nativeLabel, viewHolder.ItemView));

				var recyclerLocation = new int[2];
				var rootLocation = new int[2];
				var itemLocation = new int[2];
				var labelLocation = new int[2];
				var rootView = recyclerView.RootView;
				Assert.NotNull(rootView);
				recyclerView.GetLocationOnScreen(recyclerLocation);
				rootView.GetLocationOnScreen(rootLocation);
				viewHolder.ItemView.GetLocationOnScreen(itemLocation);
				nativeLabel.GetLocationOnScreen(labelLocation);

				var tapScreenX = labelLocation[0] + nativeLabel.Width / 2f;
				var tapScreenY = labelLocation[1] + nativeLabel.Height / 2f;
				var tapX = tapScreenX - rootLocation[0];
				var tapY = tapScreenY - rootLocation[1];
				Assert.InRange(tapX, 0.5f, rootView.Width - 0.5f);
				Assert.InRange(tapY, 0.5f, rootView.Height - 0.5f);
				Assert.InRange(tapScreenX, recyclerLocation[0] + 0.5f, recyclerLocation[0] + recyclerView.Width - 0.5f);
				Assert.InRange(tapScreenY, recyclerLocation[1] + 0.5f, recyclerLocation[1] + recyclerView.Height - 0.5f);
				Assert.InRange(tapScreenX, itemLocation[0] + 0.5f, itemLocation[0] + viewHolder.ItemView.Width - 0.5f);
				Assert.InRange(tapScreenY, itemLocation[1] + 0.5f, itemLocation[1] + viewHolder.ItemView.Height - 0.5f);
				Assert.Null(collectionView.SelectedItem);

				var downTime = global::Android.OS.SystemClock.UptimeMillis();
				using var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, tapX, tapY, 0);
				using var up = AMotionEvent.Obtain(downTime, downTime + 16, AMotionEventActions.Up, tapX, tapY, 0);
				down.SetSource(AInputSourceType.Touchscreen);
				up.SetSource(AInputSourceType.Touchscreen);
				var downDispatched = rootView.DispatchTouchEvent(down);
				var upDispatched = rootView.DispatchTouchEvent(up);

				string GetFailureDetails() =>
					$" callback index={selectionIndex}; selected={collectionView.SelectedItem ?? "<null>"};" +
					$" expected={expectedItem}; item bounds=({itemLocation[0]},{itemLocation[1]}," +
					$"{viewHolder.ItemView.Width},{viewHolder.ItemView.Height}); label bounds=({labelLocation[0]}," +
					$"{labelLocation[1]},{nativeLabel.Width},{nativeLabel.Height}); dispatch=({downDispatched},{upDispatched})";

				await AssertEventually(
					() => selectionChangedRaised,
					message: "CollectionView item tap with drag/drop recognizers did not raise SelectionChanged." + GetFailureDetails());

				Assert.True(selectionIndex == 0, "SelectionChanged did not report the sole item." + GetFailureDetails());
				Assert.True(ReferenceEquals(collectionView.SelectedItem, expectedItem),
					"SelectedItem was not the exact sole source item." + GetFailureDetails());
			});
		}

		static bool IsDescendantOf(AView descendant, AView ancestor)
		{
			var current = descendant;
			while (current is not null)
			{
				if (ReferenceEquals(current, ancestor))
					return true;

				current = current.Parent as AView;
			}

			return false;
		}
	}
}
#endif


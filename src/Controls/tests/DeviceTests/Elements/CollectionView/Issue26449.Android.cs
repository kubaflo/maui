#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using AView = Android.Views.View;
using AViewConfiguration = Android.Views.ViewConfiguration;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26449")]
	public class Issue26449 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task InnerCollectionViewConsumesUpwardDrag()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Toolbar, ToolbarHandler>();
					handlers.AddHandler<NavigationPage, NavigationViewHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var groups = CreateGroups();
			var innerCollections = new List<CollectionView>();
			var groupLabels = new List<Label>();
			var itemLabels = new List<Label>();

			var outerCollection = new CollectionView
			{
				ItemsSource = groups,
				ItemTemplate = new DataTemplate(() =>
				{
					var groupLabel = new Label
					{
						FontAttributes = FontAttributes.Bold,
					};
					groupLabel.SetBinding(Label.TextProperty, nameof(CollectionGroup.Name));
					groupLabels.Add(groupLabel);

					var innerCollection = new CollectionView
					{
						HeightRequest = 300,
						ItemTemplate = new DataTemplate(() =>
						{
							var itemLabel = new Label
							{
								Padding = new Thickness(12, 10),
							};
							itemLabel.SetBinding(Label.TextProperty, ".");
							itemLabels.Add(itemLabel);
							return itemLabel;
						}),
					};
					innerCollection.SetBinding(ItemsView.ItemsSourceProperty, nameof(CollectionGroup.Items));
					innerCollections.Add(innerCollection);

					return new VerticalStackLayout
					{
						Padding = new Thickness(12, 8),
						Spacing = 6,
						Children =
						{
							groupLabel,
							innerCollection,
						},
					};
				}),
			};

			var contentPage = new ContentPage
			{
				Title = "Nested CollectionViews",
				Content = outerCollection,
			};
			var navigationPage = new NavigationPage(contentPage);

			await CreateHandlerAndAddToWindow<IWindowHandler>(navigationPage, async _ =>
			{
				CollectionView innerCollection = null;
				Label groupLabel = null;
				Label startItemLabel = null;

				await AssertEventually(() =>
				{
					innerCollection = FindInnerCollection(innerCollections, "Group 1");
					groupLabel = FindRenderedLabel(groupLabels, "Group 1");
					startItemLabel = FindRenderedLabel(itemLabels, "Inner item 1.5");

					return innerCollection?.Handler?.PlatformView is RecyclerView innerView &&
						innerView.IsAttachedToWindow &&
						innerView.Width > 0 &&
						innerView.Height > 0 &&
						groupLabel?.Handler?.PlatformView is AView groupView &&
						groupView.IsAttachedToWindow &&
						startItemLabel?.Handler?.PlatformView is AView itemView &&
						itemView.IsAttachedToWindow;
				}, timeout: 5000, message: "The recorded Group 1 hierarchy was not rendered.");

				Assert.NotNull(innerCollection);
				Assert.NotNull(groupLabel);
				Assert.NotNull(startItemLabel);

				var outerRecycler = Assert.IsAssignableFrom<RecyclerView>(outerCollection.Handler.PlatformView);
				var innerRecycler = Assert.IsAssignableFrom<RecyclerView>(innerCollection.Handler.PlatformView);
				var groupTextView = Assert.IsAssignableFrom<TextView>(groupLabel.Handler.PlatformView);
				var startTextView = Assert.IsAssignableFrom<TextView>(startItemLabel.Handler.PlatformView);
				var decorView = MauiContext.Context.GetActivity().Window.DecorView;

				Assert.NotNull(decorView);
				Assert.True(outerRecycler.IsAttachedToWindow && outerRecycler.Width > 0 && outerRecycler.Height > 0);
				Assert.True(innerRecycler.IsAttachedToWindow && innerRecycler.Width > 0 && innerRecycler.Height > 0);
				Assert.Equal("Group 1", groupTextView.Text);
				Assert.Equal("Inner item 1.5", startTextView.Text);
				Assert.Equal(0, outerRecycler.ComputeVerticalScrollOffset());
				Assert.Equal(0, innerRecycler.ComputeVerticalScrollOffset());

				int outerRange = outerRecycler.ComputeVerticalScrollRange();
				int outerExtent = outerRecycler.ComputeVerticalScrollExtent();
				int innerRange = innerRecycler.ComputeVerticalScrollRange();
				int innerExtent = innerRecycler.ComputeVerticalScrollExtent();
				Assert.True(outerRange > outerExtent, $"Outer CollectionView must be scrollable. Range={outerRange}, extent={outerExtent}.");
				Assert.True(innerRange > innerExtent, $"Inner CollectionView must be scrollable. Range={innerRange}, extent={innerExtent}.");

				var decorLocation = GetLocation(decorView);
				var innerLocation = GetLocation(innerRecycler);
				var startLocation = GetLocation(startTextView);
				Assert.True(IsInside(startTextView, startLocation, innerRecycler, innerLocation),
					$"Inner item 1.5 must be inside the inner CollectionView. Item={DescribeFrame(startTextView, startLocation)}, inner={DescribeFrame(innerRecycler, innerLocation)}.");

				double innerCallbackOffset = -1;
				double outerCallbackOffset = -1;
				int innerCallbackFirstItem = -1;
				int outerCallbackFirstItem = -1;
				var scrollCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

				void OnInnerScrolled(object sender, ItemsViewScrolledEventArgs args)
				{
					innerCallbackOffset = args.VerticalOffset;
					innerCallbackFirstItem = args.FirstVisibleItemIndex;
					scrollCompletion.TrySetResult(true);
				}

				void OnOuterScrolled(object sender, ItemsViewScrolledEventArgs args)
				{
					outerCallbackOffset = args.VerticalOffset;
					outerCallbackFirstItem = args.FirstVisibleItemIndex;
					scrollCompletion.TrySetResult(true);
				}

				innerCollection.Scrolled += OnInnerScrolled;
				outerCollection.Scrolled += OnOuterScrolled;

				float startX = startLocation[0] - decorLocation[0] + (startTextView.Width / 2f);
				float startY = startLocation[1] - decorLocation[1] + (startTextView.Height / 2f);
				float step = decorView.Height * 0.12f;
				DispatchUpwardDrag(decorView, startX, startY, step);

				await scrollCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				innerCollection.Scrolled -= OnInnerScrolled;
				outerCollection.Scrolled -= OnOuterScrolled;

				Assert.True(
					innerCallbackOffset >= 0 && innerCallbackFirstItem >= 0 ||
					outerCallbackOffset >= 0 && outerCallbackFirstItem >= 0,
					"A post-trigger Scrolled callback must replace its sentinel offset and item position.");

				int innerOffset = -1;
				int outerOffset = -1;
				await AssertEventually(() =>
				{
					innerOffset = innerRecycler.ComputeVerticalScrollOffset();
					outerOffset = outerRecycler.ComputeVerticalScrollOffset();
					return innerOffset > 0 || outerOffset > 0;
				}, timeout: 3000, message: "Neither CollectionView reported native movement after the upward drag.");

				var currentInnerLocation = GetLocation(innerRecycler);
				var laterItemLabel = FindRenderedLabel(itemLabels, "Inner item 1.10");
				var laterTextView = laterItemLabel?.Handler?.PlatformView as TextView;
				bool laterItemVisible = false;
				string laterFrame = "not rendered";
				if (laterTextView is not null && laterTextView.IsAttachedToWindow)
				{
					var laterLocation = GetLocation(laterTextView);
					laterItemVisible = IsInside(laterTextView, laterLocation, innerRecycler, currentInnerLocation);
					laterFrame = DescribeFrame(laterTextView, laterLocation);
				}

				int touchSlop = AViewConfiguration.Get(MauiContext.Context).ScaledTouchSlop;
				const int outerOffsetTolerance = 2;
				var innerLayoutManager = Assert.IsAssignableFrom<LinearLayoutManager>(innerRecycler.GetLayoutManager());
				var outerLayoutManager = Assert.IsAssignableFrom<LinearLayoutManager>(outerRecycler.GetLayoutManager());
				int innerFirstVisibleItem = innerLayoutManager.FindFirstVisibleItemPosition();
				int outerFirstVisibleItem = outerLayoutManager.FindFirstVisibleItemPosition();

				Assert.True(
					innerOffset > touchSlop && outerOffset <= outerOffsetTolerance && laterItemVisible,
					$"Inner CollectionView did not consume the upward drag. Inner offset={innerOffset} (must exceed touch slop {touchSlop}), outer offset={outerOffset} (must be <= {outerOffsetTolerance}), inner range/extent={innerRange}/{innerExtent}, outer range/extent={outerRange}/{outerExtent}, inner first item={innerFirstVisibleItem}, outer first item={outerFirstVisibleItem}, inner callback offset/item={innerCallbackOffset}/{innerCallbackFirstItem}, outer callback offset/item={outerCallbackOffset}/{outerCallbackFirstItem}, start item={DescribeFrame(startTextView, startLocation)}, later item={laterFrame}, initial inner surface={DescribeFrame(innerRecycler, innerLocation)}, current inner surface={DescribeFrame(innerRecycler, currentInnerLocation)}.");
			});
		}

		static IReadOnlyList<CollectionGroup> CreateGroups()
		{
			var groups = new List<CollectionGroup>();
			for (int groupIndex = 1; groupIndex <= 4; groupIndex++)
			{
				var items = new List<string>();
				for (int itemIndex = 1; itemIndex <= 20; itemIndex++)
					items.Add($"Inner item {groupIndex}.{itemIndex}");

				groups.Add(new CollectionGroup
				{
					Name = $"Group {groupIndex}",
					Items = items,
				});
			}

			return groups;
		}

		static CollectionView FindInnerCollection(List<CollectionView> collections, string groupName)
		{
			for (int index = 0; index < collections.Count; index++)
			{
				if (collections[index].BindingContext is CollectionGroup group && group.Name == groupName)
					return collections[index];
			}

			return null;
		}

		static Label FindRenderedLabel(List<Label> labels, string text)
		{
			for (int index = 0; index < labels.Count; index++)
			{
				bool matchesText =
					(labels[index].BindingContext is string bindingText && bindingText == text) ||
					labels[index].Text == text;
				if (matchesText &&
					labels[index].Handler?.PlatformView is AView platformView &&
					platformView.IsAttachedToWindow)
					return labels[index];
			}

			return null;
		}

		static int[] GetLocation(AView view)
		{
			var location = new int[2];
			view.GetLocationOnScreen(location);
			return location;
		}

		static bool IsInside(AView child, int[] childLocation, AView parent, int[] parentLocation) =>
			childLocation[0] >= parentLocation[0] &&
			childLocation[1] >= parentLocation[1] &&
			childLocation[0] + child.Width <= parentLocation[0] + parent.Width &&
			childLocation[1] + child.Height <= parentLocation[1] + parent.Height;

		static string DescribeFrame(AView view, int[] location) =>
			$"[{location[0]},{location[1]},{view.Width},{view.Height}]";

		static void DispatchUpwardDrag(AView root, float startX, float startY, float step)
		{
			long downTime = global::Android.OS.SystemClock.UptimeMillis();

			var down = AMotionEvent.Obtain(downTime, downTime, AMotionEventActions.Down, startX, startY, 0);
			root.DispatchTouchEvent(down);
			down.Recycle();

			var firstMove = AMotionEvent.Obtain(downTime, downTime + 100, AMotionEventActions.Move, startX, startY - step, 0);
			root.DispatchTouchEvent(firstMove);
			firstMove.Recycle();

			var secondMove = AMotionEvent.Obtain(downTime, downTime + 200, AMotionEventActions.Move, startX, startY - (step * 2), 0);
			root.DispatchTouchEvent(secondMove);
			secondMove.Recycle();

			var up = AMotionEvent.Obtain(downTime, downTime + 300, AMotionEventActions.Up, startX, startY - (step * 2), 0);
			root.DispatchTouchEvent(up);
			up.Recycle();
		}

		sealed class CollectionGroup
		{
			public string Name { get; init; }

			public IReadOnlyList<string> Items { get; init; }
		}
	}
}
#endif


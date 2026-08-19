using System.Collections.ObjectModel;

#if ANDROID
using Microsoft.Maui.Platform;
using ATextView = Android.Widget.TextView;
using AViewGroup = Android.Views.ViewGroup;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36736, "Android SwipeItem text and icon are vertically misaligned when SwipeView wraps CollectionView", PlatformAffected.Android)]
public class Issue36736 : ContentPage
{
	readonly CollectionView _issueCollectionView;
	readonly SwipeView _collectionSwipeView;
	readonly Label _itemsStateLabel;
	readonly Label _measurementStateLabel;
	readonly Label _invocationStateLabel;
	int _swipeCallbackCount;
	int _swipeInvocationCount;
	bool _nativeGeometryCaptured;

	public Issue36736()
	{
		_itemsStateLabel = CreateStateLabel("Issue36736ItemsState", "Items=5");
		_measurementStateLabel = CreateStateLabel("Issue36736MeasurementState", "Callbacks=0;Measured=0");
		_invocationStateLabel = CreateStateLabel("Issue36736InvocationState", "Invoked=0");

		var oneItemButton = new Button { Text = "1Data" };
		var fiveItemsButton = new Button { Text = "5Data" };
		var twentyItemsButton = new Button { Text = "20Data", AutomationId = "Issue36736TwentyItems" };

		oneItemButton.Clicked += (sender, args) => InsertItems(1);
		fiveItemsButton.Clicked += (sender, args) => InsertItems(5);
		twentyItemsButton.Clicked += (sender, args) => InsertItems(20);

		var buttons = new HorizontalStackLayout
		{
			Spacing = 10,
			Children = { oneItemButton, fiveItemsButton, twentyItemsButton }
		};

		var backItem = new SwipeItem
		{
			Text = "Back",
			IconImageSource = "shopping_cart.png"
		};
		backItem.Invoked += OnBackItemInvoked;

		_issueCollectionView = new CollectionView
		{
			AutomationId = "Issue36736Collection",
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label { Padding = 10 };
				itemLabel.SetBinding(Label.TextProperty, nameof(Issue36736Item.Name));
				return itemLabel;
			})
		};

		_collectionSwipeView = new SwipeView
		{
			Threshold = 80,
			LeftItems = new SwipeItems { backItem },
			Content = _issueCollectionView
		};
		_collectionSwipeView.LeftItems.Mode = SwipeMode.Execute;
		_collectionSwipeView.SwipeChanging += OnSwipeChanging;

		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		root.Add(buttons, 0, 0);
		root.Add(_itemsStateLabel, 0, 0);
		root.Add(_measurementStateLabel, 0, 0);
		root.Add(_invocationStateLabel, 0, 0);
		root.Add(_collectionSwipeView, 0, 1);

		Content = root;
		InsertItems(5);
	}

	static Label CreateStateLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text,
			FontSize = 1,
			HeightRequest = 1,
			WidthRequest = 1,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start
		};

	void InsertItems(int count)
	{
		_issueCollectionView.ItemsSource = null;
		_issueCollectionView.SelectedItem = null;

		var items = new ObservableCollection<Issue36736Item>();
		for (var index = 1; index <= count; index++)
			items.Add(new Issue36736Item($"{index} recode"));

		_issueCollectionView.ItemsSource = items;
		_itemsStateLabel.Text = $"Items={count}";
		_swipeCallbackCount = 0;
		_swipeInvocationCount = 0;
		_nativeGeometryCaptured = false;
		_measurementStateLabel.Text = "Callbacks=0;Measured=0";
		_invocationStateLabel.Text = "Invoked=0";
	}

	void OnBackItemInvoked(object sender, EventArgs args)
	{
		_swipeInvocationCount++;
		_invocationStateLabel.Text = $"Invoked={_swipeInvocationCount}";
	}

	void OnSwipeChanging(object sender, SwipeChangingEventArgs args)
	{
		_swipeCallbackCount++;
		if (_nativeGeometryCaptured)
			return;

		if (Math.Abs(args.Offset) < 1)
		{
			_measurementStateLabel.Text = $"Callbacks={_swipeCallbackCount};Measured=0";
			return;
		}

#if ANDROID
		CaptureNativeGeometry();
#endif
	}

#if ANDROID
	void CaptureNativeGeometry()
	{
		if (_collectionSwipeView.Handler?.PlatformView is not MauiSwipeView nativeSwipeView)
			return;

		if (!TryFindSwipeButton(nativeSwipeView, out var swipeButton))
			return;

		var textLayout = swipeButton.Layout;
		if (textLayout is null)
			return;

		var drawables = swipeButton.GetCompoundDrawables();
		var iconDrawable = drawables.Length > 1 ? drawables[1] : null;
		if (iconDrawable is null || iconDrawable.Bounds.IsEmpty)
			return;

		if (swipeButton.Parent is not AViewGroup actionContainer || !ReferenceEquals(actionContainer.Parent, nativeSwipeView))
			return;

		var swipeLocation = new int[2];
		var actionLocation = new int[2];
		var buttonLocation = new int[2];
		nativeSwipeView.GetLocationOnScreen(swipeLocation);
		actionContainer.GetLocationOnScreen(actionLocation);
		swipeButton.GetLocationOnScreen(buttonLocation);

		var iconCenterY = buttonLocation[1] + swipeButton.PaddingTop + iconDrawable.Bounds.Height() / 2f;
		var textCenterY = buttonLocation[1] + swipeButton.CompoundPaddingTop + textLayout.Height / 2f;
		var swipeCenterY = swipeLocation[1] + nativeSwipeView.Height / 2f;

		_nativeGeometryCaptured = true;
		_measurementStateLabel.Text = FormattableString.Invariant(
			$"Callbacks={_swipeCallbackCount};Measured=1;Text={swipeButton.Text};Drawable=1;Parent=1;Button={buttonLocation[0]},{buttonLocation[1]},{swipeButton.Width},{swipeButton.Height};Action={actionLocation[0]},{actionLocation[1]},{actionContainer.Width},{actionContainer.Height};Swipe={swipeLocation[0]},{swipeLocation[1]},{nativeSwipeView.Width},{nativeSwipeView.Height};textCenterY={textCenterY:F1};iconCenterY={iconCenterY:F1};swipeCenterY={swipeCenterY:F1}");
	}

	static bool TryFindSwipeButton(AViewGroup parent, out ATextView swipeButton)
	{
		for (var index = 0; index < parent.ChildCount; index++)
		{
			var child = parent.GetChildAt(index);
			if (child is ATextView textView && string.Equals(textView.Text, "Back", StringComparison.Ordinal))
			{
				swipeButton = textView;
				return true;
			}

			if (child is AViewGroup childGroup && TryFindSwipeButton(childGroup, out swipeButton))
				return true;
		}

		swipeButton = null;
		return false;
	}
#endif
}

public class Issue36736Item
{
	public Issue36736Item(string name)
	{
		Name = name;
	}

	public string Name { get; }
}

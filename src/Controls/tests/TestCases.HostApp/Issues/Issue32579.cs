namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32579, "Horizontal scrollbar flickers when opening a horizontal CollectionView", PlatformAffected.UWP)]
public class Issue32579 : ContentPage
{
	readonly Button _openButton;
	readonly Button _resetButton;
	readonly ContentView _scenarioHost;
#if WINDOWS
	Microsoft.UI.Xaml.FrameworkElement _observedNativeList;
	Microsoft.UI.Xaml.Controls.Primitives.ScrollBar _horizontalScrollBar;
	EventHandler<object> _layoutUpdatedHandler;
	string _scrollBarSnapshot;
	int _scrollBarChanges;
	bool _observationCompleted;
#endif

	public Issue32579()
	{
		_openButton = new Button
		{
			AutomationId = "Issue32579OpenButton",
			Text = "Horizontal list (DataTemplate)"
		};
		_openButton.Clicked += OnOpenClicked;

		_resetButton = new Button
		{
			AutomationId = "Issue32579ResetButton",
			IsEnabled = false,
			Text = "Reset"
		};
		_resetButton.Clicked += OnResetClicked;

		_scenarioHost = new ContentView();

		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		root.Add(new HorizontalStackLayout
		{
			Children = { _openButton, _resetButton }
		});
		root.Add(_scenarioHost, 0, 1);
		Content = root;
	}

	void OnOpenClicked(object sender, EventArgs e)
	{
#if WINDOWS
		StopObservingScrollbar();
		_scrollBarSnapshot = null;
		_scrollBarChanges = -1;
		_observationCompleted = false;
#endif
		_resetButton.Text = "Observation pending";

		var collectionView = new CollectionView
		{
			AutomationId = "Issue32579AffectedCollection",
			ItemsLayout = LinearItemsLayout.Horizontal,
			ItemsSource = CreateItems(),
			ItemTemplate = new DataTemplate(CreateItemTemplate)
		};
#if WINDOWS
		collectionView.Loaded += OnCollectionLoaded;
#endif

		var pageGrid = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		pageGrid.Add(new StackLayout
		{
			Children =
			{
				new Label { Text = "1. Confirm that the CollectionView below is populated with Monkeys in a single row list and can be scrolled horizontally." },
				new Label { Text = "2. The test passes if you are able to see the image, name, and location of each monkey." }
			}
		});
		pageGrid.Add(collectionView, 0, 1);
		_scenarioHost.Content = pageGrid;
		_openButton.IsEnabled = false;
		_resetButton.IsEnabled = true;
	}

	void OnResetClicked(object sender, EventArgs e)
	{
#if WINDOWS
		StopObservingScrollbar();
#endif
		_scenarioHost.Content = null;
		_openButton.IsEnabled = true;
		_resetButton.IsEnabled = false;
		_resetButton.Text = "Reset";
	}

	View CreateItemTemplate()
	{
		var image = new Image
		{
			Aspect = Aspect.AspectFill,
			HeightRequest = 60,
			WidthRequest = 60
		};
		image.SetBinding(Image.SourceProperty, nameof(MonkeyItem.Image));

		var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
		nameLabel.SetBinding(Label.TextProperty, nameof(MonkeyItem.Name));
		var locationLabel = new Label
		{
			FontAttributes = FontAttributes.Italic,
			VerticalOptions = LayoutOptions.End
		};
		locationLabel.SetBinding(Label.TextProperty, nameof(MonkeyItem.Location));

		var itemGrid = new Grid
		{
			Padding = 10,
			RowDefinitions = { new RowDefinition(35), new RowDefinition(35) },
			ColumnDefinitions = { new ColumnDefinition(70), new ColumnDefinition(140) }
		};
		itemGrid.SetBinding(AutomationIdProperty, nameof(MonkeyItem.AutomationId));
		itemGrid.Add(image);
		Grid.SetRowSpan(image, 2);
		itemGrid.Add(nameLabel, 1, 0);
		itemGrid.Add(locationLabel, 1, 1);
#if WINDOWS
		itemGrid.Loaded += OnItemLoaded;
#endif
		return itemGrid;
	}

#if WINDOWS
	void OnCollectionLoaded(object sender, EventArgs e)
	{
		if (sender is not CollectionView collectionView ||
			collectionView.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeList)
			return;

		_observedNativeList = nativeList;
		_horizontalScrollBar = FindHorizontalScrollBar(nativeList);
		if (_horizontalScrollBar is null)
			return;

		_scrollBarChanges = 0;
		_scrollBarSnapshot = GetScrollbarSnapshot(_horizontalScrollBar);
		_layoutUpdatedHandler = OnNativeListLayoutUpdated;
		_observedNativeList.LayoutUpdated += _layoutUpdatedHandler;
	}

	void OnNativeListLayoutUpdated(object sender, object e)
	{
		if (_horizontalScrollBar is null)
			return;

		var snapshot = GetScrollbarSnapshot(_horizontalScrollBar);
		if (!string.Equals(_scrollBarSnapshot, snapshot, StringComparison.Ordinal))
		{
			_scrollBarChanges++;
			_scrollBarSnapshot = snapshot;
			if (_observationCompleted)
				_resetButton.Text = $"Observed: scrollbar changes={_scrollBarChanges}";
		}
	}

	void OnItemLoaded(object sender, EventArgs e)
	{
		if (_observationCompleted || sender is not Grid itemGrid ||
			itemGrid.BindingContext is not MonkeyItem { Index: 0 })
			return;

		_observationCompleted = true;
		itemGrid.Dispatcher.Dispatch(() =>
		{
			_resetButton.Text = _scrollBarChanges == 0
				? "Observed: scrollbar stable"
				: $"Observed: scrollbar changes={_scrollBarChanges}";
		});
	}

	void StopObservingScrollbar()
	{
		if (_observedNativeList is not null && _layoutUpdatedHandler is not null)
			_observedNativeList.LayoutUpdated -= _layoutUpdatedHandler;

		_observedNativeList = null;
		_horizontalScrollBar = null;
		_layoutUpdatedHandler = null;
	}

	static string GetScrollbarSnapshot(Microsoft.UI.Xaml.Controls.Primitives.ScrollBar scrollBar) =>
		$"{scrollBar.Visibility}|{scrollBar.ActualWidth:R}|{scrollBar.Maximum:R}|{scrollBar.ViewportSize:R}";

	static Microsoft.UI.Xaml.Controls.Primitives.ScrollBar FindHorizontalScrollBar(Microsoft.UI.Xaml.DependencyObject parent)
	{
		var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childCount; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
			if (child is Microsoft.UI.Xaml.Controls.Primitives.ScrollBar
				{
					Orientation: Microsoft.UI.Xaml.Controls.Orientation.Horizontal
				} scrollBar)
				return scrollBar;

			var descendant = FindHorizontalScrollBar(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}
#endif

	static IReadOnlyList<MonkeyItem> CreateItems() =>
	[
		new("Baboon", "Africa", 0),
		new("Capuchin Monkey", "Central and South America", 1),
		new("Blue Monkey", "Central and East Africa", 2),
		new("Squirrel Monkey", "Central and South America", 3),
		new("Golden Lion Tamarin", "Brazil", 4),
		new("Howler Monkey", "South America", 5),
		new("Japanese Macaque", "Japan", 6),
		new("Mandrill", "Cameroon", 7),
		new("Proboscis Monkey", "Borneo", 8),
		new("Douc Langur", "Vietnam", 9),
		new("Colombian White-Faced Capuchin", "Colombia", 10),
		new("Golden Snub-Nosed Monkey", "China", 11)
	];

	sealed class MonkeyItem(string name, string location, int index)
	{
		public string Name { get; } = name;
		public string Location { get; } = location;
		public string Image { get; } = "dotnet_bot.png";
		public int Index { get; } = index;
		public string AutomationId { get; } = $"Issue32579Monkey{index}";
	}
}


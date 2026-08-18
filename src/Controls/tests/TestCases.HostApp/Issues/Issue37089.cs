namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37089, "SwipeView stops tracking swipe when pointer leaves item bounds during active gesture", PlatformAffected.Android)]
public class Issue37089 : ContentPage
{
	readonly Label _telemetryLabel;
	int _changeCount;
	bool _started;

	public Issue37089()
	{
		_telemetryLabel = new Label
		{
			AutomationId = "SwipeTelemetry",
			FontAttributes = FontAttributes.Bold,
			Text = "started=false|ended=false|count=-1"
		};

		var collectionView = new CollectionView
		{
			SelectionMode = SelectionMode.None,
			ItemsSource = new[] { "Swipe this row" },
			ItemTemplate = new DataTemplate(CreateSwipeView)
		};

		var grid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			],
			RowSpacing = 16
		};

		grid.Add(new Label
		{
			Text = "Swipe left, drag below the row, continue left, then return to the row without lifting."
		});
		grid.Add(_telemetryLabel, 0, 1);
		grid.Add(collectionView, 0, 2);
		Content = grid;
	}

	SwipeView CreateSwipeView()
	{
		var itemLabel = new Label
		{
			FontSize = 20,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		itemLabel.SetBinding(Label.TextProperty, ".");

		var swipeView = new SwipeView
		{
			AutomationId = "Swipe this row",
			RightItems = new SwipeItems
			{
				Mode = SwipeMode.Reveal
			},
			Content = new Border
			{
				HeightRequest = 120,
				Padding = 24,
				Stroke = Colors.Gray,
				Content = itemLabel
			}
		};

		swipeView.RightItems.Add(new SwipeItem
		{
			BackgroundColor = Colors.Red,
			Text = "Delete"
		});
		swipeView.SwipeStarted += OnSwipeStarted;
		swipeView.SwipeChanging += OnSwipeChanging;
		swipeView.SwipeEnded += OnSwipeEnded;
		return swipeView;
	}

	void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
	{
		_started = true;
		_changeCount = 0;
		PublishTelemetry(ended: false);
	}

	void OnSwipeChanging(object sender, SwipeChangingEventArgs e)
	{
		_changeCount++;
		PublishTelemetry(ended: false);
	}

	void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
	{
		PublishTelemetry(ended: true);
	}

	void PublishTelemetry(bool ended) =>
		_telemetryLabel.Text = $"started={_started.ToString().ToLowerInvariant()}|ended={ended.ToString().ToLowerInvariant()}|count={_changeCount}";
}

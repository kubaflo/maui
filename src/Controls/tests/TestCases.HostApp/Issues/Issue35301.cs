#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	readonly CollectionView _issueCollectionView;
	readonly Label _metricsLabel;
	int _selectionChangedCount;

	public Issue35301()
	{
		_metricsLabel = new Label
		{
			AutomationId = "Issue35301Metrics",
			Text = "INITIALIZING"
		};

		_issueCollectionView = new CollectionView
		{
			AutomationId = "Issue35301CollectionView",
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				label.SetBinding(Label.AutomationIdProperty, ".");
				return label;
			})
		};
		_issueCollectionView.SelectionChanged += OnSelectionChanged;
		_issueCollectionView.Loaded += OnCollectionViewLoaded;

		var instructionsLabel = new Label
		{
			Text = "Select Apple. The item template should remain a plain Label."
		};

		var rootGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			Children =
			{
				instructionsLabel,
				_metricsLabel,
				_issueCollectionView
			}
		};
		Grid.SetRow(instructionsLabel, 0);
		Grid.SetRow(_metricsLabel, 1);
		Grid.SetRow(_issueCollectionView, 2);
		Content = rootGrid;
	}

	void OnCollectionViewLoaded(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(() => UpdateMetrics("READY"));
	}

	void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectionChangedCount++;
		Dispatcher.Dispatch(() => UpdateMetrics("COMPLETE"));
	}

	void UpdateMetrics(string state)
	{
		string selectedItem = _issueCollectionView.SelectedItem as string ?? "<null>";
		bool nativeListReady = false;
		bool selectionIndicatorSuppressed = false;
		bool roundedCornersSuppressed = false;

		if (_issueCollectionView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListView listView)
		{
			nativeListReady = true;
			selectionIndicatorSuppressed =
				listView.Resources.TryGetValue("ListViewItemSelectionIndicatorVisualEnabled", out object selectionIndicator) &&
				selectionIndicator is bool enabled &&
				!enabled;
			roundedCornersSuppressed =
				listView.Resources.TryGetValue("ListViewItemCornerRadius", out object itemCornerRadius) &&
				itemCornerRadius is Microsoft.UI.Xaml.CornerRadius cornerRadius &&
				cornerRadius.TopLeft == 0 &&
				cornerRadius.TopRight == 0 &&
				cornerRadius.BottomRight == 0 &&
				cornerRadius.BottomLeft == 0;
		}

		_metricsLabel.Text = $"{state}: callbacks={_selectionChangedCount}; selectedItem={selectedItem}; nativeListReady={nativeListReady}; selectionIndicatorSuppressed={selectionIndicatorSuppressed}; roundedCornersSuppressed={roundedCornersSuppressed}";
	}
}
#endif


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33401, "CollectionView SelectionChanged is suppressed by an ancestor TapGestureRecognizer", PlatformAffected.iOS)]
public class Issue33401 : ContentPage
{
	readonly CollectionView _issueCollectionView;
	readonly Label _interactionStatus;
	readonly Label _readyStatus;
	int _parentTapCount;
	int _selectionChangedCount;

	public Issue33401()
	{
		_readyStatus = new Label
		{
			AutomationId = "Issue33401ReadyStatus",
			Text = "Initializing"
		};

		_interactionStatus = new Label
		{
			AutomationId = "Issue33401InteractionStatus",
			Text = "Parent taps: 0; Selection changes: 0"
		};

		_issueCollectionView = new CollectionView
		{
			AutomationId = "Issue33401CollectionView",
			ItemsSource = new[] { "First item", "Second item", "Third item" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label { FontSize = 20 };
				itemLabel.SetBinding(Label.TextProperty, ".");
				itemLabel.SetBinding(Label.AutomationIdProperty, ".");

				return new Grid
				{
					Padding = 16,
					Children = { itemLabel }
				};
			})
		};
		_issueCollectionView.SelectionChanged += OnSelectionChanged;
		_readyStatus.Text = $"Ready; Selected item: {_issueCollectionView.SelectedItem ?? "<null>"}";

		var border = new Border { Content = _issueCollectionView };
		Grid.SetRow(border, 2);

		var rootGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12,
			Children =
			{
				_readyStatus,
				_interactionStatus,
				border
			}
		};
		Grid.SetRow(_interactionStatus, 1);

		var parentTapGesture = new TapGestureRecognizer();
		parentTapGesture.Tapped += OnParentTapped;
		rootGrid.GestureRecognizers.Add(parentTapGesture);

		Content = rootGrid;
	}

	void OnParentTapped(object sender, TappedEventArgs e)
	{
		_parentTapCount++;
		UpdateStatus();
	}

	void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectionChangedCount++;
		UpdateStatus();
	}

	void UpdateStatus()
	{
		_interactionStatus.Text = $"Parent taps: {_parentTapCount}; Selection changes: {_selectionChangedCount}";
		_readyStatus.Text = $"Ready; Selected item: {_issueCollectionView.SelectedItem ?? "<null>"}";
	}
}

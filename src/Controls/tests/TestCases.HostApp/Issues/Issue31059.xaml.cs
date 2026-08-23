namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31059, "CollectionView center changes after portrait-to-landscape rotation", PlatformAffected.iOS)]
public partial class Issue31059 : ContentPage
{
	const double ItemHeight = 320;
	const double ItemWidth = 300;

	readonly string[] _items = ["Item 1", "Item 2", "Item 3"];
	readonly CollectionView _issueCollectionView;
	int _currentPosition;
	int _landscapeTransition;
	int _layoutGeneration;

	public Issue31059()
	{
		InitializeComponent();

		_issueCollectionView = new CollectionView
		{
			AutomationId = "Issue31059CollectionView",
			ItemTemplate = new DataTemplate(() => new Issue31059ImageViewerImageView
			{
				ItemHeight = ItemHeight,
				ItemWidth = ItemWidth
			}),
			ItemsSource = _items,
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal)
			{
				SnapPointsType = SnapPointsType.MandatorySingle,
				SnapPointsAlignment = SnapPointsAlignment.Center
			},
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Fill,
			SelectionMode = SelectionMode.Single
		};

		_issueCollectionView.Scrolled += OnCollectionViewScrolled;
		SizeChanged += OnPageSizeChanged;
		CollectionHost.Children.Add(_issueCollectionView);
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.CenterItemIndex < 0 || e.CenterItemIndex >= _items.Length)
			return;

		var previousPosition = _currentPosition;
		_currentPosition = e.CenterItemIndex;
		CurrentItemLabel.Text = $"Current item: {_items[_currentPosition]}";

		if (_currentPosition != previousPosition)
			_issueCollectionView.ScrollTo(_currentPosition);
	}

	void OnPageSizeChanged(object sender, EventArgs e)
	{
		if (Width <= 0 || Height <= 0)
			return;

		var generation = ++_layoutGeneration;
		if (Width <= Height)
		{
			TransitionStateLabel.Text = $"Orientation:Portrait;Transition:-1;Stable:0;Size:{Width:F0}x{Height:F0}";
			return;
		}

		_landscapeTransition++;
		UpdateTransitionState("Landscape", _landscapeTransition, 0, _currentPosition);
		ObserveStableLandscapeCenter(generation, _landscapeTransition);
	}

	void ObserveStableLandscapeCenter(int generation, int transition)
	{
		Dispatcher.Dispatch(() =>
		{
			if (generation != _layoutGeneration || Width <= Height)
				return;

			var firstPosition = _currentPosition;
			UpdateTransitionState("Landscape", transition, 1, firstPosition);

			Dispatcher.Dispatch(() =>
			{
				if (generation != _layoutGeneration || Width <= Height)
					return;

				if (firstPosition != _currentPosition)
				{
					UpdateTransitionState("Landscape", transition, 0, _currentPosition);
					ObserveStableLandscapeCenter(generation, transition);
					return;
				}

				UpdateTransitionState("Landscape", transition, 2, _currentPosition);
			});
		});
	}

	void UpdateTransitionState(string orientation, int transition, int stableObservations, int centerPosition)
	{
		TransitionStateLabel.Text =
			$"Orientation:{orientation};Transition:{transition};Stable:{stableObservations};Size:{Width:F0}x{Height:F0};Center:{_items[centerPosition]}";
	}
}

sealed class Issue31059ImageViewerImageView : ContentView
{
	public Issue31059ImageViewerImageView()
	{
		var itemLabel = new Label
		{
			FontSize = 32,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		itemLabel.SetBinding(Label.TextProperty, ".");
		Content = itemLabel;
	}

	public double ItemHeight
	{
		get => HeightRequest;
		set => HeightRequest = value;
	}

	public double ItemWidth
	{
		get => WidthRequest;
		set => WidthRequest = value;
	}
}

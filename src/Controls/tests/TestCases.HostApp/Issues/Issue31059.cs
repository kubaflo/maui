using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31059, "CollectionView changes the centered item when rotating from portrait to landscape", PlatformAffected.iOS)]
public class Issue31059 : ContentPage
{
	const int ItemHeight = 280;
	const int ItemWidth = 180;

	readonly ObservableCollection<string> _items =
	[
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4"
	];

	readonly CollectionView _collectionView;
	readonly Label _positionLabel;
	readonly Label _sizeTransitionLabel;
	int _currentPosition;
	int _sizeTransitionToken;
	bool _orientationCheckArmed;

	public Issue31059()
	{
		_positionLabel = new Label
		{
			AutomationId = "CurrentPosition",
			FontSize = 18,
			Text = "Current item: 0"
		};

		_collectionView = new CollectionView
		{
			AutomationId = "ImageCollection",
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
		_collectionView.Scrolled += OnCollectionViewScrolled;

		var armButton = new Button
		{
			AutomationId = "ArmOrientationCheck",
			Text = "Arm orientation check"
		};
		armButton.Clicked += OnArmClicked;

		_sizeTransitionLabel = new Label
		{
			AutomationId = "SizeTransition",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18,
			Text = "Size transition: 0"
		};

		var pageLayout = new Grid
		{
			Padding = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 8
		};
		pageLayout.Add(_positionLabel);
		pageLayout.Add(_collectionView, 0, 1);
		pageLayout.Add(armButton, 0, 2);
		pageLayout.Add(_sizeTransitionLabel, 0, 3);
		Content = pageLayout;

		SizeChanged += OnPageSizeChanged;
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.CenterItemIndex == _currentPosition)
			return;

		_currentPosition = e.CenterItemIndex;
		_positionLabel.Text = $"Current item: {_currentPosition}";

		if (_currentPosition >= 0 && _currentPosition < _items.Count)
			_collectionView.ScrollTo(_currentPosition);
	}

	void OnArmClicked(object sender, EventArgs e)
	{
		_orientationCheckArmed = _currentPosition == _items.Count - 1;
		_sizeTransitionToken = -1;
		_sizeTransitionLabel.Text = $"Size transition: {_sizeTransitionToken}";
	}

	void OnPageSizeChanged(object sender, EventArgs e)
	{
		if (!_orientationCheckArmed || Width <= Height)
			return;

		_sizeTransitionToken = 1;
		_sizeTransitionLabel.Text = $"Size transition: {_sizeTransitionToken}";
	}
}

sealed class Issue31059ImageViewerImageView : ContentView
{
	public Issue31059ImageViewerImageView()
	{
		var caption = new Label
		{
			FontSize = 28,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		caption.SetBinding(Label.TextProperty, ".");

		Content = new Grid
		{
			BackgroundColor = Colors.LightBlue,
			Children = { caption }
		};
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


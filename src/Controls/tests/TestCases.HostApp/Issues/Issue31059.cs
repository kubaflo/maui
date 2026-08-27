namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31059, "CollectionView changes the centered item when rotating to landscape", PlatformAffected.iOS)]
public class Issue31059 : ContentPage
{
	const int LastItemIndex = 4;

	readonly CollectionView _imageCollection;
	readonly Label _currentItemLabel;
	readonly Label _orientationStateLabel;
	int _currentPosition;
	int _orientationGeneration = -1;

	public Issue31059()
	{
		Title = "CollectionView orientation";

		_currentItemLabel = new Label
		{
			Text = "Current item: 0",
			HorizontalOptions = LayoutOptions.Center,
			FontSize = 18
		};

		_orientationStateLabel = new Label
		{
			AutomationId = "Issue31059OrientationState",
			Text = "-1:0",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18
		};

		_imageCollection = new CollectionView
		{
			AutomationId = "Issue31059Collection",
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Center,
			HeightRequest = 340,
			SelectionMode = SelectionMode.Single,
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal)
			{
				SnapPointsType = SnapPointsType.MandatorySingle,
				SnapPointsAlignment = SnapPointsAlignment.Center
			},
			ItemTemplate = new DataTemplate(() => new Issue31059ImageViewerImageView
			{
				ItemHeight = 320,
				ItemWidth = 320
			}),
			ItemsSource = new[]
			{
				"Image 0",
				"Image 1",
				"Image 2",
				"Image 3",
				"Image 4"
			}
		};
		_imageCollection.Scrolled += OnCollectionScrolled;

		var collectionGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		collectionGrid.Add(_currentItemLabel);
		collectionGrid.Add(_imageCollection, 0, 1);

		var rootGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Padding = 12,
			RowSpacing = 8
		};
		rootGrid.Add(new Label
		{
			Text = "Drag to the last item in portrait, then rotate to landscape.",
			FontSize = 16
		});
		rootGrid.Add(_orientationStateLabel, 0, 1);
		rootGrid.Add(collectionGrid, 0, 2);

		Content = rootGrid;
		SizeChanged += OnPageSizeChanged;
	}

	void OnPageSizeChanged(object sender, EventArgs e)
	{
		_orientationGeneration++;
		UpdateOrientationState();
	}

	void OnCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.CenterItemIndex == _currentPosition)
			return;

		_currentPosition = e.CenterItemIndex;
		_currentItemLabel.Text = $"Current item: {_currentPosition}";
		UpdateOrientationState();

		if (e.CenterItemIndex >= 0 && e.CenterItemIndex <= LastItemIndex)
			_imageCollection.ScrollTo(e.CenterItemIndex);
	}

	void UpdateOrientationState()
	{
		_orientationStateLabel.Text = $"{_orientationGeneration}:{_currentPosition}";
	}
}

sealed class Issue31059ImageViewerImageView : ContentView
{
	public double ItemHeight
	{
		set => HeightRequest = value;
	}

	public double ItemWidth
	{
		set => WidthRequest = value;
	}

	public Issue31059ImageViewerImageView()
	{
		var label = new Label
		{
			FontSize = 32,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		label.SetBinding(Label.TextProperty, ".");
		SetBinding(SemanticProperties.DescriptionProperty, new Binding("."));

		Content = new Grid
		{
			BackgroundColor = Colors.LightBlue,
			Children = { label }
		};
	}
}


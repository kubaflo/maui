using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "Incremental loading resets the visible range", PlatformAffected.WinRT)]
public class Issue29120 : ContentPage
{
	const int PageSize = 10;
	const int MaximumItemCount = 50;

	readonly Label _visibleItemLabel;
	readonly Label _collectionStateLabel;
	int _callbackSequence = -1;
	int _firstVisibleIndex = -1;
	int _loadSequence = -1;
	int _indexBeforeLoad = -1;
	int _postLoadFirstVisibleIndex = -1;
	int _viewportChangeSequence = -1;
	int _thresholdEventCount;
	string _loadedItemName = "None";
	bool _awaitingPostLoadCallback;
	bool _isAddingItems;

	public Issue29120()
	{
		Title = "Incremental loading on scroll";

		_visibleItemLabel = new Label
		{
			AutomationId = "VisibleItemLabel",
			Text = "First visible item: -1"
		};

		_collectionStateLabel = new Label
		{
			AutomationId = "CollectionStateLabel"
		};

		LoadMoreDataCommand = new Command(AddNextPage);
		AddAnimals("Bear", "North America");
		BindingContext = this;
		UpdateCollectionState();

		var collectionView = new CollectionView
		{
			AutomationId = "AnimalsCollectionView",
			RemainingItemsThreshold = 5,
			ItemTemplate = new DataTemplate(CreateAnimalTemplate)
		};
		collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Animals));
		collectionView.SetBinding(ItemsView.RemainingItemsThresholdReachedCommandProperty, nameof(LoadMoreDataCommand));
		collectionView.RemainingItemsThresholdReached += OnRemainingItemsThresholdReached;
		collectionView.Scrolled += OnCollectionViewScrolled;

		var header = new StackLayout
		{
			Children =
			{
				new Label { Text = "Scroll down. More animals are added when five items remain." },
				_visibleItemLabel,
				_collectionStateLabel
			}
		};

		var grid = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		grid.Add(header);
		grid.Add(collectionView, row: 1);
		Content = grid;
	}

	public ObservableCollection<Issue29120AnimalItem> Animals { get; } = new();

	public ICommand LoadMoreDataCommand { get; }

	static View CreateAnimalTemplate()
	{
		var image = new Image
		{
			Aspect = Aspect.AspectFill,
			HeightRequest = 60,
			WidthRequest = 60
		};
		image.SetBinding(Image.SourceProperty, nameof(Issue29120AnimalItem.ImageSource));

		var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
		nameLabel.SetBinding(Label.TextProperty, nameof(Issue29120AnimalItem.Name));

		var locationLabel = new Label
		{
			FontAttributes = FontAttributes.Italic,
			VerticalOptions = LayoutOptions.End
		};
		locationLabel.SetBinding(Label.TextProperty, nameof(Issue29120AnimalItem.Location));

		var itemGrid = new Grid
		{
			Padding = 10,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto }
			},
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto },
				new ColumnDefinition { Width = GridLength.Star }
			}
		};
		Grid.SetRowSpan(image, 2);
		Grid.SetColumn(nameLabel, 1);
		Grid.SetColumn(locationLabel, 1);
		Grid.SetRow(locationLabel, 1);
		itemGrid.Add(image);
		itemGrid.Add(nameLabel);
		itemGrid.Add(locationLabel);
		return itemGrid;
	}

	void OnRemainingItemsThresholdReached(object sender, EventArgs e)
	{
		_thresholdEventCount++;
		UpdateCollectionState();
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_callbackSequence++;
		if (e.FirstVisibleItemIndex != _firstVisibleIndex)
			_viewportChangeSequence = _callbackSequence;

		_firstVisibleIndex = e.FirstVisibleItemIndex;
		_visibleItemLabel.Text = $"First visible item: {_firstVisibleIndex}";

		if (_awaitingPostLoadCallback)
		{
			_postLoadFirstVisibleIndex = _postLoadFirstVisibleIndex < 0
				? e.FirstVisibleItemIndex
				: Math.Min(_postLoadFirstVisibleIndex, e.FirstVisibleItemIndex);
		}

		UpdateCollectionState();
	}

	void AddNextPage()
	{
		if (_isAddingItems || Animals.Count >= MaximumItemCount)
			return;

		_isAddingItems = true;
		var page = Animals.Count / PageSize;
		var firstNewItemIndex = Animals.Count;
		var observeThisLoad = _loadSequence < 0 && _firstVisibleIndex >= 2;
		if (observeThisLoad)
		{
			_loadSequence = _callbackSequence;
			_indexBeforeLoad = _firstVisibleIndex;
			_awaitingPostLoadCallback = true;
		}

		AddAnimals(page == 1 ? "Cat" : $"Animal page {page + 1}", "Newly loaded");
		if (observeThisLoad)
			_loadedItemName = Animals[firstNewItemIndex].Name;

		_isAddingItems = false;
		UpdateCollectionState();
	}

	void AddAnimals(string kind, string location)
	{
		var firstNumber = Animals.Count + 1;

		for (var index = 0; index < PageSize; index++)
		{
			Animals.Add(new Issue29120AnimalItem
			{
				Name = $"{kind} {firstNumber + index}",
				Location = location,
				ImageSource = "dotnet_bot.png"
			});
		}
	}

	void UpdateCollectionState()
	{
		_collectionStateLabel.Text =
			$"{_callbackSequence};{_firstVisibleIndex};{Animals.Count};{_loadSequence};" +
			$"{_indexBeforeLoad};{_postLoadFirstVisibleIndex};{_viewportChangeSequence};" +
			$"{_thresholdEventCount};{_loadedItemName}";
	}
}

public sealed class Issue29120AnimalItem
{
	public required string Name { get; init; }

	public required string Location { get; init; }

	public required string ImageSource { get; init; }
}


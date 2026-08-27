using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "CollectionView jumps to the top during incremental loading", PlatformAffected.UWP)]
public class Issue29120 : ContentPage
{
	const int PageSize = 10;
	const int MaximumItemCount = 50;

	readonly Label _completedLoadLabel;
	readonly Label _scrollGenerationLabel;
	int _itemCount = PageSize;
	int _completedLoadGeneration = -1;
	int _scrollGeneration = -1;
	bool _thresholdReachedAfterScroll;
	bool _awaitingPostLoadScroll;
	bool _hasScrolled;

	public Issue29120()
	{
		Title = "Incremental loading on scroll";

		Animals = new ObservableCollection<Animal>();
		LoadMoreDataCommand = new Command(LoadMoreData);
		AddPage("Bear", "North America");

		_completedLoadLabel = new Label
		{
			AutomationId = "CompletedLoadGeneration",
			Text = "-1"
		};
		_scrollGenerationLabel = new Label
		{
			AutomationId = "ScrollGeneration",
			Text = "-1"
		};

		var instructions = new StackLayout
		{
			Children =
			{
				new Label
				{
					Text = "This case passes if the next animals are added without returning to the first item."
				},
				_completedLoadLabel,
				_scrollGenerationLabel
			}
		};

		var collectionView = new CollectionView
		{
			AutomationId = "AnimalsCollectionView",
			RemainingItemsThreshold = 5,
			ItemTemplate = CreateItemTemplate()
		};
		collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Animals));
		collectionView.SetBinding(ItemsView.RemainingItemsThresholdReachedCommandProperty, nameof(LoadMoreDataCommand));
		collectionView.RemainingItemsThresholdReached += OnRemainingItemsThresholdReached;
		collectionView.Scrolled += OnCollectionViewScrolled;

		var grid = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Add(instructions);
		grid.Add(collectionView, 0, 1);

		Content = grid;
		BindingContext = this;
	}

	public ObservableCollection<Animal> Animals { get; }

	public ICommand LoadMoreDataCommand { get; }

	static DataTemplate CreateItemTemplate()
	{
		return new DataTemplate(() =>
		{
			var nameLabel = new Label
			{
				FontAttributes = FontAttributes.Bold
			};
			nameLabel.SetBinding(Label.TextProperty, nameof(Animal.Name));
			nameLabel.SetBinding(AutomationIdProperty, nameof(Animal.Name));

			var locationLabel = new Label
			{
				FontAttributes = FontAttributes.Italic,
				VerticalOptions = LayoutOptions.End
			};
			locationLabel.SetBinding(Label.TextProperty, nameof(Animal.Location));

			var itemGrid = new Grid
			{
				Padding = 10,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Star)
				}
			};
			itemGrid.Add(new Image
			{
				Source = "groceries.png",
				Aspect = Aspect.AspectFill,
				HeightRequest = 60,
				WidthRequest = 60
			}, 0, 1, 0, 2);
			itemGrid.Add(nameLabel, 1, 0);
			itemGrid.Add(locationLabel, 1, 1);
			return itemGrid;
		});
	}

	void OnRemainingItemsThresholdReached(object sender, EventArgs e)
	{
		_thresholdReachedAfterScroll = _hasScrolled;
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_scrollGeneration++;
		_scrollGenerationLabel.Text = _scrollGeneration.ToString();

		if (e.FirstVisibleItemIndex > 0 && e.VerticalOffset > 0)
			_hasScrolled = true;

		if (!_awaitingPostLoadScroll)
			return;

		_awaitingPostLoadScroll = false;
		_completedLoadGeneration = 0;
		_completedLoadLabel.Text = "0";
	}

	void LoadMoreData()
	{
		if (_itemCount >= MaximumItemCount)
			return;

		var page = _itemCount / PageSize;
		var category = page switch
		{
			1 => "Cat",
			2 => "Dog",
			3 => "Elephant",
			_ => "Monkey"
		};
		var location = page switch
		{
			1 => "Asia",
			2 => "Europe",
			3 => "Africa",
			_ => "South America"
		};

		if (_thresholdReachedAfterScroll && _completedLoadGeneration < 0)
			_awaitingPostLoadScroll = true;

		AddPage(category, location);
		_itemCount += PageSize;
		_thresholdReachedAfterScroll = false;
	}

	void AddPage(string category, string location)
	{
		for (var index = 1; index <= PageSize; index++)
			Animals.Add(new Animal($"{category} {index}", location));
	}

	public sealed class Animal
	{
		public Animal(string name, string location)
		{
			Name = name;
			Location = location;
		}

		public string Name { get; }

		public string Location { get; }
	}
}


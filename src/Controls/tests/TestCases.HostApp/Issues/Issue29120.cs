#if WINDOWS
using System.Collections.ObjectModel;
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "Incremental loading jumps back to the top of the CollectionView", PlatformAffected.UWP)]
public class Issue29120 : ContentPage
{
	readonly ObservableCollection<Animal> _animals = [];
	readonly Label _positionLabel;
	readonly Label _resultLabel;
	int _highestFirstVisibleItem;
	int _nextAnimalIndex = 10;
	int _preLoadFirstVisibleItem = -1;
	bool _awaitingPostLoadScroll;
	bool _trackedLoadStarted;

	public Issue29120()
	{
		var instructionLabel = new Label
		{
			Text = "Scroll down to load more animals. The list should retain its position.",
			AutomationId = "Issue29120InstructionLabel"
		};

		_positionLabel = new Label
		{
			Text = "First visible item: 0",
			AutomationId = "Issue29120PositionLabel"
		};

		_resultLabel = new Label
		{
			Text = "Count=10; Pre=-1; Post=-1; Last=Animal 10@Location 10",
			AutomationId = "Issue29120ResultLabel",
			FontAttributes = FontAttributes.Bold
		};

		var collectionView = new CollectionView
		{
			AutomationId = "Issue29120CollectionView",
			RemainingItemsThreshold = 5,
			ItemTemplate = new DataTemplate(CreateAnimalTemplate)
		};
		collectionView.Scrolled += OnCollectionScrolled;

		AddAnimals(0, 10);
		collectionView.ItemsSource = _animals;
		collectionView.RemainingItemsThresholdReachedCommand = new Command(LoadMoreAnimals);

		var header = new StackLayout
		{
			Padding = new Thickness(12, 8),
			Spacing = 4,
			Children =
			{
				instructionLabel,
				_positionLabel,
				_resultLabel
			}
		};

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		grid.Add(header, 0, 0);
		grid.Add(collectionView, 0, 1);
		Content = grid;
	}

	static View CreateAnimalTemplate()
	{
		var image = new Image
		{
			HeightRequest = 60,
			WidthRequest = 60,
			Aspect = Aspect.AspectFill,
			Source = "dotnet_bot.png"
		};
		Grid.SetRowSpan(image, 2);

		var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
		nameLabel.SetBinding(Label.TextProperty, nameof(Animal.Name));
		nameLabel.SetBinding(Label.AutomationIdProperty, nameof(Animal.AutomationId));

		var locationLabel = new Label();
		locationLabel.SetBinding(Label.TextProperty, nameof(Animal.Location));

		var itemGrid = new Grid
		{
			Padding = 10,
			RowSpacing = 4,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = 60 },
				new ColumnDefinition { Width = GridLength.Star }
			},
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto }
			}
		};
		itemGrid.Add(image, 0, 0);
		itemGrid.Add(nameLabel, 1, 0);
		itemGrid.Add(locationLabel, 1, 1);
		return itemGrid;
	}

	void LoadMoreAnimals()
	{
		if (_trackedLoadStarted)
			return;

		int firstVisibleItem = _highestFirstVisibleItem;
		AddAnimals(_nextAnimalIndex, 10);
		_nextAnimalIndex += 10;

		if (firstVisibleItem >= 3)
		{
			_trackedLoadStarted = true;
			_preLoadFirstVisibleItem = firstVisibleItem;
			_awaitingPostLoadScroll = true;
		}
	}

	void OnCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_positionLabel.Text = $"First visible item: {e.FirstVisibleItemIndex.ToString(CultureInfo.InvariantCulture)}";
		_highestFirstVisibleItem = Math.Max(_highestFirstVisibleItem, e.FirstVisibleItemIndex);

		if (!_awaitingPostLoadScroll)
			return;

		_awaitingPostLoadScroll = false;
		_resultLabel.Text =
			$"Count={_animals.Count.ToString(CultureInfo.InvariantCulture)}; " +
			$"Pre={_preLoadFirstVisibleItem.ToString(CultureInfo.InvariantCulture)}; " +
			$"Post={e.FirstVisibleItemIndex.ToString(CultureInfo.InvariantCulture)}; " +
			$"Last={_animals[^1].Name}@{_animals[^1].Location}";
	}

	void AddAnimals(int start, int count)
	{
		for (int index = start; index < start + count; index++)
		{
			int number = index + 1;
			string displayNumber = number.ToString(CultureInfo.InvariantCulture);
			_animals.Add(new Animal(
				$"Animal {displayNumber}",
				$"Location {displayNumber}",
				$"Issue29120Animal{displayNumber}"));
		}
	}

	sealed class Animal(string name, string location, string automationId)
	{
		public string Name { get; } = name;

		public string Location { get; } = location;

		public string AutomationId { get; } = automationId;
	}
}
#endif


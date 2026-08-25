using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "Incremental loading jumps back to the top of the list", PlatformAffected.UWP)]
public class Issue29120 : ContentPage
{
	readonly CollectionView _animalsCollectionView;
	readonly Label _telemetryLabel;
	int _nextItem;
	int _lastFirstVisibleIndex = -1;
	int _thresholdFirstVisibleIndex = -1;
	int _postAppendFirstVisibleIndex = -1;
	string _postAppendIdentity = "unset";
	bool _thresholdAwayFromTop;
	bool _awayAppendComplete;
	bool _awaitingPostAppendScroll;
	bool _postAppendObserved;

	public Issue29120()
	{
		Animals = new ObservableCollection<Animal>();
		for (var index = 0; index < 10; index++)
			Animals.Add(CreateAnimal("Bear"));

		LoadMoreCommand = new Command(LoadMore);
		BindingContext = this;

		_telemetryLabel = new Label
		{
			AutomationId = "Issue29120Telemetry",
			FontAttributes = FontAttributes.Bold
		};

		_animalsCollectionView = new CollectionView
		{
			AutomationId = "Issue29120Collection",
			RemainingItemsThreshold = 5,
			ItemTemplate = new DataTemplate(CreateItemTemplate)
		};
		_animalsCollectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Animals));
		_animalsCollectionView.SetBinding(ItemsView.RemainingItemsThresholdReachedCommandProperty, nameof(LoadMoreCommand));
		_animalsCollectionView.Scrolled += OnCollectionViewScrolled;

		var rootGrid = new Grid
		{
			Padding = 12,
			RowSpacing = 8,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		rootGrid.Add(new Label
		{
			Text = "Incremental loading on scroll",
			FontAttributes = FontAttributes.Bold,
			FontSize = 20
		});
		rootGrid.Add(_telemetryLabel, 0, 1);
		rootGrid.Add(_animalsCollectionView, 0, 2);

		Content = rootGrid;
		UpdateTelemetry();
	}

	public ObservableCollection<Animal> Animals { get; }

	public ICommand LoadMoreCommand { get; }

	View CreateItemTemplate()
	{
		var itemGrid = new Grid
		{
			HeightRequest = 72,
			Padding = 6,
			ColumnDefinitions =
			{
				new ColumnDefinition(72),
				new ColumnDefinition(GridLength.Star)
			},
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		var image = new Image
		{
			Source = "dotnet_bot.png",
			HeightRequest = 60,
			WidthRequest = 60,
			Aspect = Aspect.AspectFit
		};
		Grid.SetRowSpan(image, 2);

		var nameLabel = new Label
		{
			FontAttributes = FontAttributes.Bold
		};
		nameLabel.SetBinding(Label.TextProperty, nameof(Animal.Name));

		var locationLabel = new Label
		{
			FontAttributes = FontAttributes.Italic
		};
		locationLabel.SetBinding(Label.TextProperty, nameof(Animal.Location));

		itemGrid.Add(image);
		itemGrid.Add(nameLabel, 1, 0);
		itemGrid.Add(locationLabel, 1, 1);
		return itemGrid;
	}

	void LoadMore()
	{
		var observeThisLoad = !_thresholdAwayFromTop && _lastFirstVisibleIndex > 0;
		if (observeThisLoad)
		{
			_thresholdFirstVisibleIndex = _lastFirstVisibleIndex;
			_thresholdAwayFromTop = true;
			_awayAppendComplete = false;
			_awaitingPostAppendScroll = true;
		}

		for (var index = 0; index < 10; index++)
			Animals.Add(CreateAnimal("Animal"));

		if (observeThisLoad)
			_awayAppendComplete = true;

		UpdateTelemetry();
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_lastFirstVisibleIndex = e.FirstVisibleItemIndex;
		if (_awayAppendComplete && _awaitingPostAppendScroll)
		{
			_postAppendFirstVisibleIndex = e.FirstVisibleItemIndex;
			_postAppendIdentity = e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex < Animals.Count
				? Animals[e.FirstVisibleItemIndex].Name
				: "invalid";
			_postAppendObserved = true;
			_awaitingPostAppendScroll = false;
		}

		UpdateTelemetry();
	}

	void UpdateTelemetry()
	{
		_telemetryLabel.Text =
			$"ThresholdAway={_thresholdAwayFromTop};AppendComplete={_awayAppendComplete};" +
			$"PostObserved={_postAppendObserved};" +
			$"Threshold={_thresholdFirstVisibleIndex};Post={_postAppendFirstVisibleIndex};" +
			$"Identity={_postAppendIdentity};Count={Animals.Count}";
	}

	Animal CreateAnimal(string kind)
	{
		var number = ++_nextItem;
		return new Animal($"{kind} {number}", $"Habitat {number}");
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


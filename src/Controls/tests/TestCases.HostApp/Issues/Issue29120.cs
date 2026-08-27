using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "Incremental loading on scroll jumps back to the top", PlatformAffected.WinRT)]
public class Issue29120 : ContentPage
{
	const int PageSize = 10;

	readonly Label _itemCountLabel;
	readonly Label _loadGenerationLabel;

	int _firstVisibleItemIndex;
	double _verticalOffset;
	int _firstVisibleBeforeLoad;
	double _offsetBeforeLoad;
	bool _observingLoad;
	bool _viewportResetObserved;

	public Issue29120()
	{
		Title = "Incremental loading on scroll";

		_itemCountLabel = new Label
		{
			AutomationId = "Issue29120ItemCount",
			Text = PageSize.ToString()
		};

		_loadGenerationLabel = new Label
		{
			AutomationId = "Issue29120LoadGeneration",
			Text = "-1"
		};

		LoadMoreDataCommand = new Command(LoadMoreData);
		AddPage("Bear", "North America");
		Animals[0] = new AnimalItem("American Black Bear", "North America");
		BindingContext = this;

		var collectionView = new CollectionView
		{
			AutomationId = "Issue29120CollectionView",
			RemainingItemsThreshold = 5,
			ItemTemplate = new DataTemplate(() =>
			{
				var image = new Image
				{
					Aspect = Aspect.AspectFill,
					HeightRequest = 60,
					WidthRequest = 60
				};

				var nameLabel = new Label
				{
					FontAttributes = FontAttributes.Bold
				};
				nameLabel.SetBinding(Label.TextProperty, nameof(AnimalItem.Name));

				var locationLabel = new Label
				{
					FontAttributes = FontAttributes.Italic,
					VerticalOptions = LayoutOptions.End
				};
				locationLabel.SetBinding(Label.TextProperty, nameof(AnimalItem.Location));

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
				itemGrid.Add(image, 0, 0);
				Grid.SetRowSpan(image, 2);
				itemGrid.Add(nameLabel, 1, 0);
				itemGrid.Add(locationLabel, 1, 1);
				return itemGrid;
			})
		};
		collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Animals));
		collectionView.SetBinding(ItemsView.RemainingItemsThresholdReachedCommandProperty, nameof(LoadMoreDataCommand));
		collectionView.RemainingItemsThresholdReached += OnRemainingItemsThresholdReached;
		collectionView.Scrolled += OnCollectionViewScrolled;

		var statusLayout = new StackLayout
		{
			Children =
			{
				new Label
				{
					Text = "New animals should be added when five items remain without changing the viewport."
				},
				_itemCountLabel,
				_loadGenerationLabel
			}
		};

		var rootGrid = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		rootGrid.Add(statusLayout, 0, 0);
		rootGrid.Add(collectionView, 0, 1);
		Content = rootGrid;
	}

	public ObservableCollection<AnimalItem> Animals { get; } = [];

	public ICommand LoadMoreDataCommand { get; }

	void OnRemainingItemsThresholdReached(object sender, EventArgs e)
	{
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		_firstVisibleItemIndex = e.FirstVisibleItemIndex;
		_verticalOffset = e.VerticalOffset;

		if (!_observingLoad)
			return;

		if (e.FirstVisibleItemIndex < _firstVisibleBeforeLoad || e.VerticalOffset + 20 < _offsetBeforeLoad)
			_viewportResetObserved = true;

		_loadGenerationLabel.Text = _viewportResetObserved ? "1: RESET" : "1: PRESERVED";
	}

	void LoadMoreData()
	{
		if (Animals.Count != PageSize)
			return;

		_firstVisibleBeforeLoad = _firstVisibleItemIndex;
		_offsetBeforeLoad = _verticalOffset;
		_observingLoad = _firstVisibleBeforeLoad > 0 || _offsetBeforeLoad > 1;

		AddPage("Cat", "Africa");
		_itemCountLabel.Text = Animals.Count.ToString();
	}

	void AddPage(string category, string location)
	{
		for (int index = 1; index <= PageSize; index++)
			Animals.Add(new AnimalItem($"{category} {index}", location));
	}

	public sealed class AnimalItem
	{
		public AnimalItem(string name, string location)
		{
			Name = name;
			Location = location;
		}

		public string Name { get; }

		public string Location { get; }
	}
}


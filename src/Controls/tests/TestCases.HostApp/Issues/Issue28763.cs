using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28763, "Multiple notifications for SelectionChanged in a CollectionView when reusing a singleton view model", PlatformAffected.Android)]
public class Issue28763 : TestNavigationPage
{
	protected override void Init()
	{
		var sharedDetailViewModel = new Issue28763DetailViewModel();
		PushAsync(new Issue28763TaskPage(sharedDetailViewModel));
	}
}

sealed class Issue28763TaskPage : ContentPage
{
	readonly CollectionView _taskCollection;
	readonly Issue28763DetailViewModel _sharedDetailViewModel;

	public Issue28763TaskPage(Issue28763DetailViewModel sharedDetailViewModel)
	{
		Title = "Tasks";
		_sharedDetailViewModel = sharedDetailViewModel;

		_taskCollection = new CollectionView
		{
			AutomationId = "MainTaskCollection",
			ItemsSource = new[] { "Task 1", "Task 2", "Task 3" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};
		_taskCollection.SelectionChanged += OnTaskSelectionChanged;

		var grid = new Grid
		{
			Padding = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12,
			Children =
			{
				new Label
				{
					Text = "Tasks",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				},
				_taskCollection
			}
		};

		Grid.SetRow(_taskCollection, 1);
		Content = grid;
	}

	async void OnTaskSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.Count == 0 || e.CurrentSelection[0] is not string task)
			return;

		_taskCollection.SelectedItem = null;
		_sharedDetailViewModel.PrepareVisit(task);
		await Navigation.PushAsync(new Issue28763DetailPage(_sharedDetailViewModel));
	}
}

sealed class Issue28763DetailPage : ContentPage
{
	public Issue28763DetailPage(Issue28763DetailViewModel viewModel)
	{
		Title = viewModel.Task;
		BindingContext = viewModel;

		var detailState = new Label
		{
			AutomationId = "DetailState",
			FontSize = 22,
			FontAttributes = FontAttributes.Bold
		};
		detailState.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.DetailState));

		var commandState = new Label
		{
			AutomationId = "CommandState"
		};
		commandState.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.CommandState));

		var collection = new CollectionView
		{
			AutomationId = "DetailItemCollection",
			ItemsSource = viewModel.Items,
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};
		collection.SetBinding(
			SelectableItemsView.SelectedItemProperty,
			nameof(Issue28763DetailViewModel.SelectedItem),
			BindingMode.TwoWay);
		collection.SetBinding(
			SelectableItemsView.SelectionChangedCommandProperty,
			nameof(Issue28763DetailViewModel.SelectionChangedCommand));

		Content = new Grid
		{
			Padding = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				detailState,
				commandState,
				collection
			}
		};

		Grid.SetRow(commandState, 1);
		Grid.SetRow(collection, 2);
	}
}

sealed class Issue28763DetailViewModel : BindableObject
{
	object _selectedItem = null!;
	string _detailState = string.Empty;
	string _commandState = "Command: pending";
	int _notificationCount;
	int _visit;

	public Issue28763DetailViewModel()
	{
		Items = new ObservableCollection<string> { "Item A", "Item B", "Item C" };
		SelectionChangedCommand = new Command(OnSelectionChanged);
	}

	public ObservableCollection<string> Items { get; }

	public Command SelectionChangedCommand { get; }

	public string Task { get; private set; } = string.Empty;

	public object SelectedItem
	{
		get => _selectedItem;
		set
		{
			if (_selectedItem == value)
				return;

			_selectedItem = value;
			OnPropertyChanged();
		}
	}

	public string DetailState
	{
		get => _detailState;
		private set
		{
			if (_detailState == value)
				return;

			_detailState = value;
			OnPropertyChanged();
		}
	}

	public string CommandState
	{
		get => _commandState;
		private set
		{
			if (_commandState == value)
				return;

			_commandState = value;
			OnPropertyChanged();
		}
	}

	public void PrepareVisit(string task)
	{
		_visit++;
		Task = task;
		SelectedItem = null!;
		_notificationCount = 0;
		DetailState = $"Detail visit {_visit}: Detail items for {task}; Notifications: 0";
		CommandState = "Command: pending";
	}

	void OnSelectionChanged()
	{
		_notificationCount++;
		DetailState = $"Detail visit {_visit}: Detail items for {Task}; Notifications: {_notificationCount}";
		CommandState = "Command: observed";
	}
}


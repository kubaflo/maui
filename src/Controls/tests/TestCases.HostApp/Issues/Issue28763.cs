#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28763, "CollectionView SelectionChangedCommand runs multiple times with a singleton view model", PlatformAffected.Android)]
public class Issue28763 : NavigationPage
{
	public Issue28763() : base(new Issue28763TaskListPage())
	{
	}
}

public class Issue28763TaskListPage : ContentPage
{
	readonly System.Collections.ObjectModel.ObservableCollection<Issue28763TaskItem> _tasks = [];
	readonly Issue28763DetailViewModel _singletonDetailViewModel = new();
	readonly Entry _taskEntry;
	readonly CollectionView _tasksView;
	int _nextTaskId = 1;
	int _nextDetailPageId = 1;

	public Issue28763TaskListPage()
	{
		Title = "Tasks";

		_taskEntry = new Entry
		{
			AutomationId = "TaskEntry",
			Placeholder = "Task name"
		};

		var addTaskButton = new Button
		{
			AutomationId = "AddTaskButton",
			HorizontalOptions = LayoutOptions.End,
			Text = "Add task"
		};
		addTaskButton.Clicked += OnAddTaskClicked;

		_tasksView = new CollectionView
		{
			ItemsSource = _tasks,
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { Padding = 12 };
				label.SetBinding(Label.TextProperty, nameof(Issue28763TaskItem.Name));
				label.SetBinding(AutomationIdProperty, nameof(Issue28763TaskItem.AutomationId));
				return label;
			})
		};
		_tasksView.SelectionChanged += OnTaskSelected;

		var grid = new Grid
		{
			Padding = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12,
			Children =
			{
				new Label { Text = "Task list", FontSize = 24 },
				_taskEntry,
				addTaskButton,
				_tasksView
			}
		};
		Grid.SetRow(_taskEntry, 1);
		Grid.SetRow(addTaskButton, 1);
		Grid.SetRow(_tasksView, 2);
		Content = grid;
	}

	void OnAddTaskClicked(object sender, EventArgs e)
	{
		var name = _taskEntry.Text?.Trim();
		if (string.IsNullOrEmpty(name))
			return;

		_tasks.Add(new Issue28763TaskItem(name, $"Task-{_nextTaskId++}"));
		_taskEntry.Text = string.Empty;
	}

	async void OnTaskSelected(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not Issue28763TaskItem task)
			return;

		_tasksView.SelectedItem = null;
		var detailPage = new Issue28763DetailPage(
			task,
			_singletonDetailViewModel,
			_nextDetailPageId++);
		await Navigation.PushAsync(detailPage);
	}
}

public sealed record Issue28763TaskItem(string Name, string AutomationId);

public sealed record Issue28763DetailItem(string Name, string AutomationId);

public class Issue28763DetailPage : ContentPage
{
	public Issue28763DetailPage(
		Issue28763TaskItem task,
		Issue28763DetailViewModel viewModel,
		int pageInstanceId)
	{
		Title = task.Name;
		viewModel.StartVisit();

		var collectionView = new CollectionView
		{
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { Padding = 12 };
				label.SetBinding(Label.TextProperty, nameof(Issue28763DetailItem.Name));
				label.SetBinding(AutomationIdProperty, nameof(Issue28763DetailItem.AutomationId));
				return label;
			})
		};
		collectionView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Issue28763DetailViewModel.Items));
		collectionView.SetBinding(
			SelectableItemsView.SelectedItemProperty,
			nameof(Issue28763DetailViewModel.SelectedItem),
			mode: BindingMode.TwoWay);
		collectionView.SetBinding(
			SelectableItemsView.SelectionChangedCommandProperty,
			nameof(Issue28763DetailViewModel.SelectionChangedCommand));

		var callbackCount = new Label { AutomationId = "CallbackCount" };
		callbackCount.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.CallbackCountText));

		var callbackToken = new Label { AutomationId = "CallbackToken" };
		callbackToken.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.CallbackTokenText));

		var selectedItem = new Label { AutomationId = "SelectedItemText" };
		selectedItem.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.SelectedItemText));

		var heading = new Label
		{
			AutomationId = "DetailHeading",
			FontSize = 24,
			Text = $"Details for {task.Name}"
		};
		var pageInstance = new Label
		{
			AutomationId = "PageInstance",
			Text = $"Page instance: {pageInstanceId}"
		};
		var viewModelInstance = new Label
		{
			AutomationId = "ViewModelInstance",
			Text = $"View model instance: {viewModel.InstanceId}"
		};

		var grid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				heading,
				pageInstance,
				viewModelInstance,
				collectionView,
				callbackCount,
				callbackToken,
				selectedItem
			}
		};
		Grid.SetRow(pageInstance, 1);
		Grid.SetRow(viewModelInstance, 2);
		Grid.SetRow(collectionView, 3);
		Grid.SetRow(callbackCount, 4);
		Grid.SetRow(callbackToken, 5);
		Grid.SetRow(selectedItem, 6);

		Content = grid;
		BindingContext = viewModel;
	}
}

public class Issue28763DetailViewModel : BindableObject
{
	static int s_nextInstanceId;

	readonly System.Collections.ObjectModel.ObservableCollection<Issue28763DetailItem> _items =
	[
		new("Detail item A", "Detail-A"),
		new("Detail item B", "Detail-B")
	];
	Issue28763DetailItem _selectedItem = null!;
	int _callbackCount;
	string _callbackCountText = "Callbacks this visit: 0";
	string _callbackTokenText = "Callback token: -1";
	string _selectedItemText = "Selected item: none";

	public Issue28763DetailViewModel()
	{
		InstanceId = System.Threading.Interlocked.Increment(ref s_nextInstanceId);
		SelectionChangedCommand = new Command(OnSelectionChanged);
	}

	public int InstanceId { get; }

	public System.Collections.ObjectModel.ObservableCollection<Issue28763DetailItem> Items => _items;

	public Issue28763DetailItem SelectedItem
	{
		get => _selectedItem;
		set
		{
			if (_selectedItem == value)
				return;

			if (value is null)
			{
				_selectedItem = null!;
				SelectedItemText = "Selected item: none";
				OnPropertyChanged();
				return;
			}

			_selectedItem = value;
			SelectedItemText = $"Selected item: {value.Name}";
			OnPropertyChanged();
		}
	}

	public Command SelectionChangedCommand { get; }

	public string CallbackCountText
	{
		get => _callbackCountText;
		private set
		{
			if (_callbackCountText == value)
				return;

			_callbackCountText = value;
			OnPropertyChanged();
		}
	}

	public string CallbackTokenText
	{
		get => _callbackTokenText;
		private set
		{
			if (_callbackTokenText == value)
				return;

			_callbackTokenText = value;
			OnPropertyChanged();
		}
	}

	public string SelectedItemText
	{
		get => _selectedItemText;
		private set
		{
			if (_selectedItemText == value)
				return;

			_selectedItemText = value;
			OnPropertyChanged();
		}
	}

	public void StartVisit()
	{
		_callbackCount = 0;
		CallbackCountText = "Callbacks this visit: 0";
		CallbackTokenText = "Callback token: -1";
	}

	void OnSelectionChanged()
	{
		_callbackCount++;
		CallbackCountText = $"Callbacks this visit: {_callbackCount}";
		CallbackTokenText = "Callback token: observed";
	}
}
#endif


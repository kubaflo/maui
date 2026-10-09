using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28763, "Multiple SelectionChanged notifications with a singleton view model", PlatformAffected.iOS)]
public class Issue28763 : NavigationPage
{
	public Issue28763()
		: base(new Issue28763TaskPage(new Issue28763DetailViewModel()))
	{
	}
}

public class Issue28763TaskPage : ContentPage
{
	readonly ObservableCollection<Issue28763TaskItem> _tasks = [];
	readonly Issue28763DetailViewModel _detailViewModel;
	readonly Entry _taskEntry;
	readonly CollectionView _taskList;

	public Issue28763TaskPage(Issue28763DetailViewModel detailViewModel)
	{
		Title = "Tasks";
		_detailViewModel = detailViewModel;
		_taskEntry = new Entry
		{
			AutomationId = "TaskEntry",
			Placeholder = "Task name"
		};
		_taskList = new CollectionView
		{
			AutomationId = "TaskList",
			ItemsSource = _tasks,
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					InputTransparent = true,
					Padding = 12
				};
				label.SetBinding(Label.TextProperty, nameof(Issue28763TaskItem.Name));
				label.SetBinding(AutomationIdProperty, nameof(Issue28763TaskItem.AutomationId));
				return label;
			})
		};
		_taskList.SelectionChanged += OnTaskSelected;

		var addButton = new Button
		{
			AutomationId = "AddTask",
			Text = "Add task"
		};
		addButton.Clicked += OnAddTaskClicked;

		var grid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				_taskEntry,
				addButton,
				_taskList
			}
		};
		Grid.SetRow(addButton, 1);
		Grid.SetRow(_taskList, 2);
		Content = grid;
	}

	void OnAddTaskClicked(object sender, EventArgs e)
	{
		var name = _taskEntry.Text?.Trim();
		if (string.IsNullOrEmpty(name))
			return;

		_tasks.Add(new Issue28763TaskItem(name, $"TaskRow{_tasks.Count + 1}"));
		_taskEntry.Text = string.Empty;
	}

	async void OnTaskSelected(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not Issue28763TaskItem task)
			return;

		_taskList.SelectedItem = null;
		var detailPage = new Issue28763DetailPage(_detailViewModel);
		detailPage.SetTask(task.Name);
		await Navigation.PushAsync(detailPage);
	}
}

public class Issue28763DetailPage : ContentPage
{
	static int s_nextPageIdentity;

	public Issue28763DetailPage(Issue28763DetailViewModel viewModel)
	{
		BindingContext = viewModel;
		viewModel.BeginSelectionCycle();

		var pageIdentity = new Label
		{
			AutomationId = "DetailPageIdentity",
			Text = $"Detail page: {++s_nextPageIdentity}"
		};
		var viewModelIdentity = new Label
		{
			AutomationId = "ViewModelIdentity",
			Text = viewModel.Identity
		};
		var readyState = new Label
		{
			AutomationId = "ReadyState",
			Text = $"Ready: Items={viewModel.Items.Count}; SelectedItem={(viewModel.SelectedItem is null ? "null" : "set")}; Command={(viewModel.SelectionChangedCommand is null ? "null" : "set")}"
		};
		var commandCount = new Label
		{
			AutomationId = "CommandCount"
		};
		commandCount.SetBinding(Label.TextProperty, nameof(Issue28763DetailViewModel.CommandCount));

		var detailList = new CollectionView
		{
			AutomationId = "DetailList",
			ItemsSource = viewModel.Items,
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					InputTransparent = true,
					Padding = 12
				};
				label.SetBinding(Label.TextProperty, nameof(Issue28763DetailItem.Name));
				label.SetBinding(AutomationIdProperty, nameof(Issue28763DetailItem.AutomationId));
				return label;
			})
		};
		detailList.SetBinding(
			CollectionView.SelectedItemProperty,
			new Binding(nameof(Issue28763DetailViewModel.SelectedItem), mode: BindingMode.TwoWay));
		detailList.SetBinding(
			SelectableItemsView.SelectionChangedCommandProperty,
			nameof(Issue28763DetailViewModel.SelectionChangedCommand));
		detailList.Loaded += (_, _) => viewModel.BeginSelectionCycle();

		var grid = new Grid
		{
			Padding = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				pageIdentity,
				viewModelIdentity,
				readyState,
				commandCount,
				detailList
			}
		};
		Grid.SetRow(viewModelIdentity, 1);
		Grid.SetRow(readyState, 2);
		Grid.SetRow(commandCount, 3);
		Grid.SetRow(detailList, 4);
		Content = grid;
	}

	public void SetTask(string taskName)
	{
		Title = taskName;
	}
}

public class Issue28763DetailViewModel : INotifyPropertyChanged
{
	static int s_nextIdentity;
	object _selectedItem;
	string _commandCount = "SelectionChangedCommand calls: 0";
	int _cycleCalls;

	public Issue28763DetailViewModel()
	{
		Identity = $"Singleton view model: {++s_nextIdentity}";
		Items =
		[
			new Issue28763DetailItem("Detail item 1", "DetailItem1"),
			new Issue28763DetailItem("Detail item 2", "DetailItem2")
		];
		SelectionChangedCommand = new Command(OnSelectionChanged);
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public string Identity { get; }

	public ObservableCollection<Issue28763DetailItem> Items { get; }

	public Command SelectionChangedCommand { get; }

	public object SelectedItem
	{
		get => _selectedItem;
		set => SetProperty(ref _selectedItem, value);
	}

	public string CommandCount
	{
		get => _commandCount;
		private set => SetProperty(ref _commandCount, value);
	}

	public void BeginSelectionCycle()
	{
		SelectedItem = null;
		_cycleCalls = 0;
		CommandCount = "SelectionChangedCommand calls: 0";
	}

	void OnSelectionChanged()
	{
		_cycleCalls++;
		CommandCount = $"SelectionChangedCommand calls: {_cycleCalls}";
	}

	void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return;

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public record Issue28763TaskItem(string Name, string AutomationId);

public record Issue28763DetailItem(string Name, string AutomationId);

static class Issue28763Extensions
{
	public static MauiAppBuilder Issue28763RegisterServices(this MauiAppBuilder builder)
	{
		builder.Services.AddTransient<Issue28763DetailPage>();
		builder.Services.AddSingleton<Issue28763DetailViewModel>();
		return builder;
	}
}


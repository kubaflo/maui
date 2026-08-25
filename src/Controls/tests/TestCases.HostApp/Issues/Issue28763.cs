#if WINDOWS
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28763, "Multiple notifications for SelectionChanged in a CollectionView when the view model is added with addSingleton", PlatformAffected.WinRT)]
public class Issue28763 : NavigationPage
{
	public Issue28763() : base(new TaskPage(new DetailPageService()))
	{
	}

	sealed class TaskPage : ContentPage
	{
		readonly DetailPageService _detailPageService;

		public TaskPage(DetailPageService detailPageService)
		{
			_detailPageService = detailPageService;
			BindingContext = this;

			var taskCollection = new CollectionView
			{
				ItemsSource = Tasks,
				SelectionMode = SelectionMode.Single,
				ItemTemplate = new DataTemplate(CreateTaskItem)
			};
			taskCollection.SelectionChanged += OnTaskSelectionChanged;

			Content = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				RowSpacing = 16,
				Children =
				{
					new Label
					{
						Text = "Tasks",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					taskCollection
				}
			};

			Grid.SetRow(taskCollection, 1);
		}

		public ObservableCollection<TaskItem> Tasks { get; } =
		[
			new("Task 1", "Task1Text"),
			new("Task 2", "Task2Text")
		];

		static View CreateTaskItem()
		{
			var label = new Label
			{
				Padding = 16,
				FontSize = 18
			};
			label.SetBinding(Label.TextProperty, nameof(TaskItem.Name));
			label.SetBinding(AutomationIdProperty, nameof(TaskItem.AutomationId));
			return label;
		}

		async void OnTaskSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.CurrentSelection.Count == 0 || e.CurrentSelection[0] is not TaskItem task)
				return;

			await Navigation.PushAsync(_detailPageService.CreatePage(task.Name));
		}
	}

	public sealed record TaskItem(string Name, string AutomationId);

	sealed class DetailPageService
	{
		readonly DetailViewModel _singletonViewModel = new();

		public ContentPage CreatePage(string taskName) => new DetailPage(taskName, _singletonViewModel);
	}

	sealed class DetailPage : ContentPage
	{
		public DetailPage(string taskName, DetailViewModel viewModel)
		{
			Title = taskName;
			BindingContext = viewModel;

			var heading = new Label
			{
				Text = taskName,
				FontSize = 24,
				FontAttributes = FontAttributes.Bold,
				AutomationId = "DetailHeading"
			};

			var selectedItemLabel = new Label
			{
				AutomationId = "SelectedItemStatus",
				FontAttributes = FontAttributes.Bold
			};
			selectedItemLabel.SetBinding(Label.TextProperty, nameof(DetailViewModel.SelectedItemText));

			var commandCountLabel = new Label { AutomationId = "CommandCount" };
			commandCountLabel.SetBinding(Label.TextProperty, nameof(DetailViewModel.CommandCountText));

			var selectionDeltaLabel = new Label { AutomationId = "SelectionDelta" };
			selectionDeltaLabel.SetBinding(Label.TextProperty, nameof(DetailViewModel.SelectionDeltaText));

			var armButton = new Button
			{
				Text = "Arm selection check",
				AutomationId = "ArmTriggerButton"
			};
			armButton.Clicked += (_, _) => viewModel.BeginSelectionCheck();

			var detailCollection = new CollectionView
			{
				ItemsSource = viewModel.Items,
				SelectionMode = SelectionMode.Single,
				ItemTemplate = new DataTemplate(CreateDetailItem)
			};
			detailCollection.SetBinding(
				SelectableItemsView.SelectedItemProperty,
				nameof(DetailViewModel.SelectedItem),
				mode: BindingMode.TwoWay);
			detailCollection.SetBinding(
				SelectableItemsView.SelectionChangedCommandProperty,
				nameof(DetailViewModel.SelectionChangedCommand));

			var backButton = new Button
			{
				Text = "Back to tasks",
				AutomationId = "BackToTasksButton"
			};
			backButton.Clicked += OnBackClicked;

			Content = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				},
				RowSpacing = 12,
				Children =
				{
					heading,
					selectedItemLabel,
					commandCountLabel,
					selectionDeltaLabel,
					armButton,
					detailCollection,
					backButton
				}
			};

			Grid.SetRow(selectedItemLabel, 1);
			Grid.SetRow(commandCountLabel, 2);
			Grid.SetRow(selectionDeltaLabel, 3);
			Grid.SetRow(armButton, 4);
			Grid.SetRow(detailCollection, 5);
			Grid.SetRow(backButton, 6);
		}

		static View CreateDetailItem()
		{
			var label = new Label
			{
				Padding = 16,
				FontSize = 18
			};
			label.SetBinding(Label.TextProperty, nameof(DetailItem.Name));
			label.SetBinding(AutomationIdProperty, nameof(DetailItem.AutomationId));
			return label;
		}

		async void OnBackClicked(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}

	sealed class DetailViewModel : BindableObject
	{
		DetailItem _selectedItem = null!;
		int _commandCount;
		int _selectionCheckBaseline = -1;
		int _selectionDelta = -1;

		public DetailViewModel()
		{
			SelectionChangedCommand = new Command(OnSelectionChanged);
		}

		public ObservableCollection<DetailItem> Items { get; } =
		[
			new("Detail item 1", "DetailItem1"),
			new("Detail item 2", "DetailItem2")
		];

		public ICommand SelectionChangedCommand { get; }

		public DetailItem SelectedItem
		{
			get => _selectedItem;
			set
			{
				if (_selectedItem == value)
					return;

				_selectedItem = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SelectedItemText));
			}
		}

		public string SelectedItemText => $"Selected item: {_selectedItem?.Name ?? "none"}";

		public string CommandCountText => $"SelectionChangedCommand calls: {_commandCount}";

		public string SelectionDeltaText => $"SelectionChangedCommand delta: {_selectionDelta}";

		public void BeginSelectionCheck()
		{
			_selectionCheckBaseline = _commandCount;
			_selectionDelta = -1;
			OnPropertyChanged(nameof(SelectionDeltaText));
		}

		void OnSelectionChanged()
		{
			_commandCount++;
			OnPropertyChanged(nameof(CommandCountText));

			if (_selectionCheckBaseline < 0)
				return;

			_selectionDelta = _commandCount - _selectionCheckBaseline;
			OnPropertyChanged(nameof(SelectionDeltaText));
		}
	}

	sealed record DetailItem(string Name, string AutomationId);
}
#endif


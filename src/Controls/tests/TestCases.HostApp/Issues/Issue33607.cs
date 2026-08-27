#if WINDOWS
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	readonly Button _openWindowButton;
	readonly Button _updateItemsButton;
	readonly Label _closureStatusLabel;
	readonly Label _mutationResultLabel;
	ObservableCollection<string> _closedWindowItems = new();
	Microsoft.Maui.ILayout _closedWindowLayout = null!;
	NotifyCollectionChangedAction _lastAppliedAction = (NotifyCollectionChangedAction)(-1);
	int _closedWindowCount;

	public Issue33607()
	{
		_openWindowButton = new Button
		{
			Text = "Open and close secondary window",
			AutomationId = "OpenWindowButton"
		};
		_openWindowButton.Clicked += OnOpenWindowClicked;

		_updateItemsButton = new Button
		{
			Text = "Update closed window items",
			AutomationId = "UpdateItemsButton",
			IsEnabled = false
		};
		_updateItemsButton.Clicked += OnUpdateItemsClicked;

		_closureStatusLabel = new Label
		{
			Text = "Destroyed windows: 0",
			AutomationId = "ClosureStatusLabel"
		};

		_mutationResultLabel = new Label
		{
			Text = "Cycle 0: not run",
			AutomationId = "MutationResultLabel"
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "Update a BindableLayout after its secondary window closes" },
					_openWindowButton,
					_updateItemsButton,
					_closureStatusLabel,
					_mutationResultLabel
				}
			}
		};
	}

	void OnOpenWindowClicked(object sender, EventArgs e)
	{
		var application = Application.Current;
		if (application is null)
		{
			_closureStatusLabel.Text = "Application unavailable";
			return;
		}

		var cycle = _closedWindowCount + 1;
		_closedWindowItems = new ObservableCollection<string> { $"Initial item {cycle}" };

		var itemsLayout = new VerticalStackLayout();
		_closedWindowLayout = itemsLayout;
		_lastAppliedAction = (NotifyCollectionChangedAction)(-1);
		_closedWindowItems.CollectionChanged += (_, e) =>
		{
			_lastAppliedAction = e.Apply(
				(_, _, _) => { },
				(_, _) => { },
				() => { });
		};
		BindableLayout.SetItemsSource(itemsLayout, _closedWindowItems);
		BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
		{
			var label = new Label();
			label.SetBinding(Label.TextProperty, ".");

			return new ContentView
			{
				Content = new Border
				{
					Content = label
				}
			};
		}));

		var page = new ContentPage
		{
			Title = "Issue 33607 secondary window",
			Content = itemsLayout
		};
		var window = new Window
		{
			Page = page,
			Title = page.Title
		};

		EventHandler activatedHandler = null!;
		activatedHandler = (_, _) =>
		{
			window.Activated -= activatedHandler;
			window.Dispatcher.Dispatch(() => application.CloseWindow(window));
		};

		EventHandler destroyingHandler = null!;
		destroyingHandler = (_, _) =>
		{
			window.Destroying -= destroyingHandler;
			_closedWindowCount++;
			_closureStatusLabel.Text = $"Destroyed windows: {_closedWindowCount}";
			_openWindowButton.IsEnabled = true;
			_updateItemsButton.IsEnabled = true;
		};

		window.Activated += activatedHandler;
		window.Destroying += destroyingHandler;
		_openWindowButton.IsEnabled = false;
		_updateItemsButton.IsEnabled = false;
		_closureStatusLabel.Text = $"Secondary window {cycle} opened";
		application.OpenWindow(window);
	}

	void OnUpdateItemsClicked(object sender, EventArgs e)
	{
		var cycle = _closedWindowCount;

		try
		{
			_closedWindowItems.Add($"Post-close item {cycle}");

			if (_lastAppliedAction != NotifyCollectionChangedAction.Add)
				_mutationResultLabel.Text = $"Cycle {cycle}: collection notification not applied";
			else if (_closedWindowLayout.Count != 2)
				_mutationResultLabel.Text = $"Cycle {cycle}: ILayout insertion missing";
			else
				_mutationResultLabel.Text = $"Cycle {cycle}: no exception";
		}
		catch (ObjectDisposedException)
		{
			_mutationResultLabel.Text = _lastAppliedAction == NotifyCollectionChangedAction.Add
				? $"Cycle {cycle}: ObjectDisposedException"
				: $"Cycle {cycle}: collection notification not applied before exception";
		}

		_updateItemsButton.IsEnabled = false;
	}
}
#endif


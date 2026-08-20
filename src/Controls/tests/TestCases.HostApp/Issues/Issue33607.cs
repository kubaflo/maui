#if WINDOWS
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	const int CycleCount = 3;

	readonly Label _runStateLabel;
	readonly Label _completedLabel;
	readonly Label _pagesLoadedLabel;
	readonly Label _attachedHierarchiesLabel;
	readonly Label _initialItemsRenderedLabel;
	readonly Label _destroyingCallbacksLabel;
	readonly Label _attemptedUpdatesLabel;
	readonly Label _objectDisposedExceptionsLabel;
	readonly Button _runButton;

	int _currentCycle = -1;
	int _completed = -1;
	int _pagesLoaded = -1;
	int _attachedHierarchies = -1;
	int _initialItemsRendered = -1;
	int _destroyingCallbacks = -1;
	int _attemptedUpdates = -1;
	int _objectDisposedExceptions = -1;

	public Issue33607()
	{
		Title = "Issue 33607";

		_runStateLabel = CreateStatusLabel("Issue33607RunState", "not-started");
		_completedLabel = CreateStatusLabel("Issue33607Completed", "completed=-1");
		_pagesLoadedLabel = CreateStatusLabel("Issue33607PagesLoaded", "pagesLoaded=-1");
		_attachedHierarchiesLabel = CreateStatusLabel("Issue33607AttachedHierarchies", "attachedHierarchies=-1");
		_initialItemsRenderedLabel = CreateStatusLabel("Issue33607InitialItemsRendered", "initialItemsRendered=-1");
		_destroyingCallbacksLabel = CreateStatusLabel("Issue33607DestroyingCallbacks", "destroyingCallbacks=-1");
		_attemptedUpdatesLabel = CreateStatusLabel("Issue33607AttemptedUpdates", "attemptedUpdates=-1");
		_objectDisposedExceptionsLabel = CreateStatusLabel("Issue33607ObjectDisposedExceptions", "objectDisposedExceptions=-1");
		_runButton = new Button
		{
			AutomationId = "Issue33607Run",
			Text = "Run 3 window cycles"
		};
		_runButton.Clicked += OnRunClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Issue 33607: close a window, then apply a collection change to its ILayout.",
					FontSize = 18
				},
				_runStateLabel,
				_completedLabel,
				_pagesLoadedLabel,
				_attachedHierarchiesLabel,
				_initialItemsRenderedLabel,
				_destroyingCallbacksLabel,
				_attemptedUpdatesLabel,
				_objectDisposedExceptionsLabel,
				_runButton
			}
		};
	}

	static Label CreateStatusLabel(string automationId, string text)
	{
		return new Label
		{
			AutomationId = automationId,
			Text = text
		};
	}

	void OnRunClicked(object sender, EventArgs e)
	{
		_runButton.IsEnabled = false;
		_runStateLabel.Text = "started";
		_currentCycle = 0;
		_completed = 0;
		_pagesLoaded = 0;
		_attachedHierarchies = 0;
		_initialItemsRendered = 0;
		_destroyingCallbacks = 0;
		_attemptedUpdates = 0;
		_objectDisposedExceptions = 0;
		UpdateStatusLabels();
		StartNextCycle();
	}

	void StartNextCycle()
	{
		var cycle = ++_currentCycle;
		var initialItem = $"Initial item for cycle {cycle}";
		var items = new ObservableCollection<string>();
		var itemsLayout = new VerticalStackLayout();
		Microsoft.Maui.ILayout layout = itemsLayout;
		items.CollectionChanged += (_, e) => ApplyCollectionChange(e, layout);
		items.Add(initialItem);

		var border = new Border { Content = itemsLayout };
		var contentView = new ContentView { Content = border };
		var page = new ContentPage
		{
			Title = $"Issue 33607 cycle {cycle}",
			Content = contentView
		};
		var childWindow = new Window
		{
			Page = page,
			Title = page.Title
		};
		var closeQueued = false;

		page.Loaded += (_, _) =>
		{
			if (closeQueued)
				return;

			closeQueued = true;
			_pagesLoaded++;

			var renderedLabel = itemsLayout.Children.Count == 1 ? itemsLayout.Children[0] as Label : null;
			if (renderedLabel?.Text == initialItem && renderedLabel.Handler is not null)
				_initialItemsRendered++;

			if (page.Handler is not null &&
				contentView.Handler is not null &&
				border.Handler is not null &&
				itemsLayout.Handler is not null &&
				renderedLabel?.Handler is not null)
			{
				_attachedHierarchies++;
			}

			UpdateStatusLabels();
			page.Dispatcher.Dispatch(() => Application.Current.CloseWindow(childWindow));
		};

		childWindow.Destroying += (_, _) =>
		{
			_destroyingCallbacks++;
			UpdateStatusLabels();
			Dispatcher.Dispatch(() => UpdateAfterDestroy(items, cycle));
		};

		Application.Current.OpenWindow(childWindow);
	}

	static void ApplyCollectionChange(NotifyCollectionChangedEventArgs e, Microsoft.Maui.ILayout layout)
	{
		var controlsLayout = (Microsoft.Maui.Controls.Layout)layout;
		NotifyCollectionChangedEventArgsExtensions.Apply(
			e,
			insert: (item, index, _) => controlsLayout.Insert(index, new Label { Text = item.ToString() }),
			removeAt: (_, index) => controlsLayout.RemoveAt(index),
			reset: controlsLayout.Clear);
	}

	void UpdateAfterDestroy(ObservableCollection<string> items, int cycle)
	{
		_attemptedUpdates++;
		try
		{
			items.Add($"Post-destroy item for cycle {cycle}");
		}
		catch (ObjectDisposedException)
		{
			_objectDisposedExceptions++;
		}

		_completed++;
		UpdateStatusLabels();

		if (_completed < CycleCount)
		{
			StartNextCycle();
			return;
		}

		_runButton.IsEnabled = true;
	}

	void UpdateStatusLabels()
	{
		_completedLabel.Text = $"completed={_completed}";
		_pagesLoadedLabel.Text = $"pagesLoaded={_pagesLoaded}";
		_attachedHierarchiesLabel.Text = $"attachedHierarchies={_attachedHierarchies}";
		_initialItemsRenderedLabel.Text = $"initialItemsRendered={_initialItemsRendered}";
		_destroyingCallbacksLabel.Text = $"destroyingCallbacks={_destroyingCallbacks}";
		_attemptedUpdatesLabel.Text = $"attemptedUpdates={_attemptedUpdates}";
		_objectDisposedExceptionsLabel.Text = $"objectDisposedExceptions={_objectDisposedExceptions}";
	}
}
#endif

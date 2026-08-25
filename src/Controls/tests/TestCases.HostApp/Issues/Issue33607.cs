#if WINDOWS
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "ObjectDisposedException after closing a window while applying an ILayout collection change", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	readonly Button _runCycleButton;
	readonly Label _completedCyclesLabel;
	readonly Label _loadedPagesLabel;
	readonly Label _closedWindowsLabel;
	readonly Label _mutationCallbacksLabel;
	readonly Label _itemCountLabel;
	readonly Label _exceptionCountLabel;
	readonly List<ObservableCollection<string>> _retainedCollections = [];
	int _completedCycles;
	int _loadedPages;
	int _closedWindows;
	int _mutationCallbacks;
	int _itemCount;
	int _exceptionCount = -1;
	bool _cycleRunning;

	public Issue33607()
	{
		Title = "Issue 33607";

		_completedCyclesLabel = CreateStatusLabel("CompletedCyclesStatus", "Completed cycles: 0");
		_loadedPagesLabel = CreateStatusLabel("LoadedPagesStatus", "Loaded pages: 0");
		_closedWindowsLabel = CreateStatusLabel("ClosedWindowsStatus", "Closed windows: 0");
		_mutationCallbacksLabel = CreateStatusLabel("MutationCallbacksStatus", "Mutation callbacks: 0");
		_itemCountLabel = CreateStatusLabel("ItemCountStatus", "Item count: 0");
		_exceptionCountLabel = CreateStatusLabel("ExceptionCountStatus", "Post-close exceptions: -1");

		_runCycleButton = new Button
		{
			AutomationId = "RunCycleButton",
			Text = "Run window close cycle"
		};
		_runCycleButton.Clicked += OnRunCycleClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 12,
			Children =
			{
				_runCycleButton,
				_completedCyclesLabel,
				_loadedPagesLabel,
				_closedWindowsLabel,
				_mutationCallbacksLabel,
				_itemCountLabel,
				_exceptionCountLabel
			}
		};
	}

	static Label CreateStatusLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	void OnRunCycleClicked(object sender, EventArgs e)
	{
		if (_cycleRunning)
			return;

		_cycleRunning = true;
		_runCycleButton.IsEnabled = false;
		if (_exceptionCount < 0)
			_exceptionCount = 0;

		var items = new ObservableCollection<string> { "Before close" };
		_retainedCollections.Add(items);
		Microsoft.Maui.ILayout itemsLayout = new VerticalStackLayout();
		var mutableLayout = (Layout)itemsLayout;
		mutableLayout.Add(CreateItem(items[0]));
		items.CollectionChanged += (_, e) =>
		{
			try
			{
				NotifyCollectionChangedEventArgsExtensions.Apply(
					e,
					(item, index, _) => mutableLayout.Insert(index, CreateItem((string)item)),
					(_, index) => mutableLayout.RemoveAt(index),
					mutableLayout.Clear);
			}
			catch (ObjectDisposedException)
			{
				_exceptionCount++;
			}
			finally
			{
				_mutationCallbacks++;
			}
		};
		var secondaryPage = new ContentPage
		{
			Title = "Issue 33607 secondary",
			Content = (View)itemsLayout
		};
		var secondaryWindow = new Window
		{
			Page = secondaryPage,
			Title = secondaryPage.Title
		};

		var currentApplication = Microsoft.Maui.Controls.Application.Current
			?? throw new InvalidOperationException("Application.Current is required to test window disposal.");
		secondaryPage.Loaded += OnSecondaryPageLoaded;
		currentApplication.OpenWindow(secondaryWindow);

		void OnSecondaryPageLoaded(object loadedSender, EventArgs loadedArgs)
		{
			secondaryPage.Loaded -= OnSecondaryPageLoaded;
			_loadedPages++;

			currentApplication.CloseWindow(secondaryWindow);
			_closedWindows++;

			items.Add("After close");

			_itemCount = items.Count;
			_completedCycles++;
			PublishState();
			_cycleRunning = false;
			_runCycleButton.IsEnabled = true;
		}
	}

	static ContentView CreateItem(string text) =>
		new()
		{
			Content = new Border
			{
				Content = new Label
				{
					Text = text
				}
			}
		};

	void PublishState()
	{
		_completedCyclesLabel.Text = $"Completed cycles: {_completedCycles}";
		_loadedPagesLabel.Text = $"Loaded pages: {_loadedPages}";
		_closedWindowsLabel.Text = $"Closed windows: {_closedWindows}";
		_mutationCallbacksLabel.Text = $"Mutation callbacks: {_mutationCallbacks}";
		_itemCountLabel.Text = $"Item count: {_itemCount}";
		_exceptionCountLabel.Text = $"Post-close exceptions: {_exceptionCount}";
	}
}
#endif


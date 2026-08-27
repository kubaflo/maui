#if WINDOWS
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Internals;
using ILayout = Microsoft.Maui.ILayout;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "ObjectDisposedException when ILayout is updated after closing a Window", PlatformAffected.WinRT)]
public class Issue33607 : ContentPage
{
	const int CycleCount = 2;

	readonly List<ObservableCollection<string>> _retainedCollections = [];
	readonly Label _loadedWindowCountLabel;
	readonly Label _destroyedWindowCountLabel;
	readonly Label _successfulMutationCountLabel;
	readonly Label _cycleCompletionLabel;
	int _loadedWindowCount;
	int _destroyedWindowCount;
	int _successfulMutationCount;
	int _failedMutationCount;

	public Issue33607()
	{
		_loadedWindowCountLabel = CreateCounterLabel("LoadedWindowCount");
		_destroyedWindowCountLabel = CreateCounterLabel("DestroyedWindowCount");
		_successfulMutationCountLabel = CreateCounterLabel("SuccessfulMutationCount");
		_cycleCompletionLabel = new Label
		{
			AutomationId = "CycleCompletionLabel",
			Text = "Not completed"
		};

		var runCyclesButton = new Button
		{
			AutomationId = "RunCyclesButton",
			Text = "Run two window cycles"
		};
		runCyclesButton.Clicked += (sender, args) =>
		{
			runCyclesButton.IsEnabled = false;
			var application = Application.Current ?? throw new InvalidOperationException("The test requires a running Application.");
			StartCycle(application, 1);
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Children =
			{
				new Label { Text = "Open and close two secondary windows, then apply each retained collection change to its ILayout." },
				runCyclesButton,
				_loadedWindowCountLabel,
				_destroyedWindowCountLabel,
				_successfulMutationCountLabel,
				_cycleCompletionLabel
			}
		};
	}

	static Label CreateCounterLabel(string automationId) =>
		new()
		{
			AutomationId = automationId,
			Text = "0"
		};

	void StartCycle(Application application, int cycle)
	{
		var items = new ObservableCollection<string> { "Initial item" };
		_retainedCollections.Add(items);

		var itemsHost = new VerticalStackLayout();
		ILayout itemsLayout = itemsHost;
		itemsLayout.Add(CreateItemView(items[0]));
		items.CollectionChanged += OnItemsCollectionChanged;

		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Children =
				{
					new Label { Text = $"Secondary ILayout window {cycle}" },
					itemsHost
				}
			}
		};
		var window = new Window(page);

		itemsHost.Loaded += OnItemsHostLoaded;
		window.Destroying += OnWindowDestroying;
		application.OpenWindow(window);

		void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			NotifyCollectionChangedEventArgsExtensions.Apply(
				args,
				insert: (item, index, _) => itemsLayout.Insert(index, CreateItemView(item)),
				removeAt: (_, index) => itemsLayout.RemoveAt(index),
				reset: () =>
				{
					itemsLayout.Clear();
					foreach (var item in items)
						itemsLayout.Add(CreateItemView(item));
				});
		}

		void OnItemsHostLoaded(object sender, EventArgs args)
		{
			itemsHost.Loaded -= OnItemsHostLoaded;
			_loadedWindowCount++;
			_loadedWindowCountLabel.Text = _loadedWindowCount.ToString();
			Dispatcher.Dispatch(() => application.CloseWindow(window));
		}

		void OnWindowDestroying(object sender, EventArgs args)
		{
			window.Destroying -= OnWindowDestroying;
			_destroyedWindowCount++;
			_destroyedWindowCountLabel.Text = _destroyedWindowCount.ToString();
			Dispatcher.Dispatch(CompleteCycle);
		}

		void CompleteCycle()
		{
			try
			{
				items.Add($"Added item {items.Count}");
				_successfulMutationCount++;
				_successfulMutationCountLabel.Text = _successfulMutationCount.ToString();
			}
			catch (ObjectDisposedException)
			{
				_failedMutationCount++;
			}

			if (cycle < CycleCount)
			{
				StartCycle(application, cycle + 1);
			}
			else
			{
				_cycleCompletionLabel.Text = $"Completed {CycleCount} cycles; ObjectDisposedExceptions: {_failedMutationCount}";
			}
		}
	}

	static ContentView CreateItemView(object item) =>
		new()
		{
			Content = new Border
			{
				Content = new Label { Text = item.ToString() }
			}
		};
}
#endif


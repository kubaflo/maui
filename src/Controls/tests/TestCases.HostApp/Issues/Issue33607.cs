#if WINDOWS
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	const int AttemptCount = 3;
	readonly Button _runAttemptButton;
	readonly Label _attemptStatus;
	readonly Label _mutationCount;
	readonly Label _attemptResults;
	readonly List<string> _results = [];
	int _attempt;
	int _successfulMutations;

	public Issue33607()
	{
		_runAttemptButton = new Button
		{
			AutomationId = "RunAttemptButton",
			Text = "Run attempt"
		};
		_attemptStatus = new Label
		{
			AutomationId = "AttemptStatus",
			Text = "Ready for attempt 1"
		};
		_mutationCount = new Label
		{
			AutomationId = "MutationCount",
			Text = "Successful mutations: 0"
		};
		_attemptResults = new Label
		{
			AutomationId = "AttemptResults",
			Text = "No attempts completed"
		};

		_runAttemptButton.Clicked += OnRunAttemptClicked;

		Content = new VerticalStackLayout
		{
			Children =
			{
				_attemptStatus,
				_runAttemptButton,
				_mutationCount,
				_attemptResults
			}
		};
	}

	void OnRunAttemptClicked(object sender, EventArgs e)
	{
		if (_attempt >= AttemptCount)
			return;

		var application = Application.Current;
		if (application is null)
			throw new InvalidOperationException("A running application is required to open the test window.");

		var attempt = ++_attempt;
		_runAttemptButton.IsEnabled = false;
		_attemptStatus.Text = $"Attempt {attempt} running";

		var items = new ObservableCollection<string> { "Initial item" };
		var itemHostView = new VerticalStackLayout();
		Microsoft.Maui.ILayout itemHost = itemHostView;
		var itemTemplate = new DataTemplate(() =>
		{
			var label = new Label();
			label.SetBinding(Label.TextProperty, ".");

			return new ContentView
			{
				Content = new Border
				{
					Content = new VerticalStackLayout
					{
						Children = { label }
					}
				}
			};
		});

		itemHost.Add(CreateItem(items[0]));
		items.CollectionChanged += (_, args) => NotifyCollectionChangedEventArgsExtensions.Apply(
			args,
			(item, index, _) => itemHost.Insert(index, CreateItem(item)),
			(_, index) => itemHost.RemoveAt(index),
			() =>
			{
				itemHost.Clear();
				foreach (var item in items)
					itemHost.Add(CreateItem(item));
			});

		var page = new ContentPage
		{
			Title = $"Issue 33607 attempt {attempt}",
			Content = itemHostView
		};
		var secondaryWindow = new Window(page)
		{
			Title = page.Title
		};

		page.Loaded += OnPageLoaded;
		application.OpenWindow(secondaryWindow);

		void OnPageLoaded(object loadedSender, EventArgs loadedArgs)
		{
			page.Loaded -= OnPageLoaded;
			page.Dispatcher.Dispatch(() =>
			{
				application.CloseWindow(secondaryWindow);

				string result;
				try
				{
					items.Add($"Post-close item {attempt}");
					_successfulMutations++;
					result = $"Attempt {attempt}: mutation succeeded";
				}
				catch (ObjectDisposedException exception)
				{
					result = $"Attempt {attempt}: ObjectDisposedException: {exception.Message}";
				}

				_results.Add(result);
				_mutationCount.Text = $"Successful mutations: {_successfulMutations}";
				_attemptResults.Text = string.Join(" | ", _results);
				_attemptStatus.Text = $"Attempt {attempt} complete";
				_runAttemptButton.IsEnabled = attempt < AttemptCount;
			});
		}

		View CreateItem(object item)
		{
			var view = (View)itemTemplate.CreateContent();
			view.BindingContext = item;
			return view;
		}
	}
}
#endif


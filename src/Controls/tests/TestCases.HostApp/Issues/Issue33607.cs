#if WINDOWS
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;
using ILayout = Microsoft.Maui.ILayout;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	const int MaximumCloseChecks = 100;

	readonly Button _runAttemptButton;
	readonly Label _stateLabel;
	readonly Label[] _loadedMarkers;
	readonly Label[] _closedMarkers;
	readonly Label[] _completedMarkers;
	bool _attemptRunning;
	int _activeAttempt = -1;
	int _completedAttempts;
	bool _loaded;
	bool _closed;
	bool _collectionChangeApplied;
	bool _insertReturned;
	int _collectionCount;
	string _exceptionType = "none";
	string _exceptionMessage = string.Empty;

	public Issue33607()
	{
		_stateLabel = new Label
		{
			AutomationId = "Issue33607State",
			Text = InitialState
		};

		_runAttemptButton = new Button
		{
			AutomationId = "Issue33607RunAttempt",
			Text = "Run window close attempt"
		};
		_runAttemptButton.Clicked += OnRunAttemptClicked;

		_loadedMarkers = CreateMarkers("Loaded");
		_closedMarkers = CreateMarkers("Closed");
		_completedMarkers = CreateMarkers("Complete");

		var rootLayout = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "Issue 33607 window disposal scenario" },
				_stateLabel,
				_runAttemptButton
			}
		};

		foreach (var marker in _loadedMarkers)
			rootLayout.Children.Add(marker);

		foreach (var marker in _closedMarkers)
			rootLayout.Children.Add(marker);

		foreach (var marker in _completedMarkers)
			rootLayout.Children.Add(marker);

		Content = rootLayout;
	}

	static string InitialState =>
		"attempt=0; loaded=false; closed=false; collectionChangeApplied=false; insertReturned=false; collectionCount=0; exception=none";

	static Label[] CreateMarkers(string stage)
	{
		var markers = new Label[3];
		for (var index = 0; index < markers.Length; index++)
		{
			var attempt = index + 1;
			markers[index] = new Label
			{
				AutomationId = $"Issue33607Attempt{attempt}{stage}",
				Text = $"Attempt {attempt} {stage.ToLowerInvariant()}",
				IsVisible = false
			};
		}

		return markers;
	}

	void OnRunAttemptClicked(object sender, EventArgs e)
	{
		if (_attemptRunning || _completedAttempts >= 3)
			return;

		var application = Application.Current;
		if (application is null)
		{
			_exceptionType = "ApplicationUnavailable";
			CompleteAttempt(_completedAttempts + 1);
			return;
		}

		_attemptRunning = true;
		_runAttemptButton.IsEnabled = false;
		_activeAttempt = _completedAttempts + 1;
		_loaded = false;
		_closed = false;
		_collectionChangeApplied = false;
		_insertReturned = false;
		_collectionCount = 1;
		_exceptionType = "none";
		_exceptionMessage = string.Empty;
		UpdateState();

		var items = new ObservableCollection<string> { "Existing item" };
		items.CollectionChanged += OnItemsCollectionChanged;

		ILayout itemLayout = new VerticalStackLayout();
		var bindableItemLayout = (BindableObject)itemLayout;
		BindableLayout.SetItemsSource(bindableItemLayout, items);
		BindableLayout.SetItemTemplate(bindableItemLayout, new DataTemplate(() =>
		{
			var itemLabel = new Label();
			itemLabel.SetBinding(Label.TextProperty, ".");

			return new ContentView
			{
				Content = new Border
				{
					Content = new VerticalStackLayout
					{
						Children = { itemLabel }
					}
				}
			};
		}));

		var page = new ContentPage
		{
			Title = $"Issue 33607 attempt {_activeAttempt}",
			Content = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "Secondary Window with BindableLayout" },
					(View)itemLayout
				}
			}
		};

		var secondaryWindow = new Window
		{
			Page = page,
			Title = page.Title
		};

		void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventArgsExtensions.Apply(
				e,
				(item, index, create) =>
					_collectionChangeApplied = create && Equals(item, "Inserted after close"),
				(item, index) => _collectionChangeApplied = false,
				() => _collectionChangeApplied = false);
		}

		page.Loaded += OnSecondaryPageLoaded;
		application.OpenWindow(secondaryWindow);

		void OnSecondaryPageLoaded(object loadedSender, EventArgs loadedArgs)
		{
			page.Loaded -= OnSecondaryPageLoaded;
			_loaded = true;
			_loadedMarkers[_activeAttempt - 1].IsVisible = true;
			UpdateState();

			page.Dispatcher.Dispatch(() =>
			{
				application.CloseWindow(secondaryWindow);
				Dispatcher.Dispatch(() => WaitForWindowClosed(application, secondaryWindow, items, MaximumCloseChecks));
			});
		}
	}

	void WaitForWindowClosed(Application application, Window secondaryWindow, ObservableCollection<string> items, int remainingChecks)
	{
		var windowIsOpen = false;
		foreach (var candidate in application.Windows)
		{
			if (ReferenceEquals(candidate, secondaryWindow))
			{
				windowIsOpen = true;
				break;
			}
		}

		if (windowIsOpen && remainingChecks > 0)
		{
			Dispatcher.Dispatch(() => WaitForWindowClosed(application, secondaryWindow, items, remainingChecks - 1));
			return;
		}

		_closed = !windowIsOpen;
		if (!_closed)
		{
			_exceptionType = "WindowStillOpen";
			CompleteAttempt(_activeAttempt);
			return;
		}

		_closedMarkers[_activeAttempt - 1].IsVisible = true;
		UpdateState();
		Dispatcher.Dispatch(() => InsertAfterClose(items));
	}

	void InsertAfterClose(ObservableCollection<string> items)
	{
		try
		{
			items.Add("Inserted after close");
			_insertReturned = true;
		}
		catch (ObjectDisposedException exception)
		{
			_exceptionType = exception.GetType().Name;
			_exceptionMessage = exception.Message;
		}

		_collectionCount = items.Count;
		CompleteAttempt(_activeAttempt);
	}

	void CompleteAttempt(int attempt)
	{
		_completedAttempts = attempt;
		_completedMarkers[attempt - 1].IsVisible = true;
		_attemptRunning = false;
		_runAttemptButton.IsEnabled = _completedAttempts < 3;
		UpdateState();
	}

	void UpdateState()
	{
		var exception = _exceptionType;
		if (!string.IsNullOrEmpty(_exceptionMessage))
			exception = $"{exception}: {_exceptionMessage}";

		_stateLabel.Text =
			$"attempt={Math.Max(_activeAttempt, _completedAttempts)}; " +
			$"loaded={_loaded.ToString().ToLowerInvariant()}; " +
			$"closed={_closed.ToString().ToLowerInvariant()}; " +
			$"collectionChangeApplied={_collectionChangeApplied.ToString().ToLowerInvariant()}; " +
			$"insertReturned={_insertReturned.ToString().ToLowerInvariant()}; " +
			$"collectionCount={_collectionCount}; exception={exception}";
	}
}
#endif


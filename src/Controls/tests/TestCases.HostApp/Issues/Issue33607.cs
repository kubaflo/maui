#if WINDOWS
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	readonly Label _cycleStatusLabel;
	readonly Label _exceptionStatusLabel;
	readonly Label _apiStatusLabel;
	readonly Button _runCycleButton;
	int _completedCycles;

	public Issue33607()
	{
		_cycleStatusLabel = new Label
		{
			AutomationId = "Issue33607CycleStatus",
			Text = "Cycle=-1"
		};

		_exceptionStatusLabel = new Label
		{
			AutomationId = "Issue33607ExceptionStatus",
			Text = "NotRun"
		};

		_apiStatusLabel = new Label
		{
			AutomationId = "Issue33607ApiStatus",
			Text = "ILayout.Apply=NotRun"
		};

		_runCycleButton = new Button
		{
			AutomationId = "Issue33607RunCycleButton",
			Text = "Run close-window cycle"
		};
		_runCycleButton.Clicked += OnRunCycleClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 12,
			Children =
			{
				new Label { Text = "Open and close a window, then update its BindableLayout source." },
				_cycleStatusLabel,
				_exceptionStatusLabel,
				_apiStatusLabel,
				_runCycleButton
			}
		};
	}

	void OnRunCycleClicked(object sender, EventArgs e)
	{
		var application = Application.Current;
		if (application is null)
			throw new InvalidOperationException("A running application is required to open the child window.");

		_runCycleButton.IsEnabled = false;
		_exceptionStatusLabel.Text = "NotRun";
		_apiStatusLabel.Text = "ILayout.Apply=NotRun";

		var items = new ObservableCollection<string> { "Item before window close" };
		var nextCycle = _completedCycles + 1;
		var page = CreateChildPage(items, nextCycle);
		var window = new Window
		{
			Page = page,
			Title = page.Title
		};

		page.Loaded += OnChildPageLoaded;
		application.OpenWindow(window);

		void OnChildPageLoaded(object loadedSender, EventArgs loadedArgs)
		{
			page.Loaded -= OnChildPageLoaded;
			application.CloseWindow(window);

			Dispatcher.Dispatch(() =>
			{
				try
				{
					items.Add("Item inserted after window close");
					_exceptionStatusLabel.Text = "None";
				}
				catch (ObjectDisposedException)
				{
					_exceptionStatusLabel.Text = nameof(ObjectDisposedException);
				}

				_completedCycles++;
				_cycleStatusLabel.Text = $"Cycle={_completedCycles}";
				_runCycleButton.IsEnabled = true;
			});
		}
	}

	ContentPage CreateChildPage(ObservableCollection<string> items, int cycle)
	{
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

		var bindableLayout = new VerticalStackLayout
		{
			Spacing = 8
		};
		BindableLayout.SetItemTemplate(bindableLayout, itemTemplate);

		Microsoft.Maui.ILayout handlerLayout = bindableLayout;
		items.CollectionChanged += (_, args) =>
		{
			NotifyCollectionChangedEventArgsExtensions.Apply(
				args,
				(_, _, _) => _apiStatusLabel.Text = $"ILayout.Apply=Insert;Cycle={cycle};Children={handlerLayout.Count}",
				(_, _) => _apiStatusLabel.Text = $"ILayout.Apply=Remove;Cycle={cycle};Children={handlerLayout.Count}",
				() => _apiStatusLabel.Text = $"ILayout.Apply=Reset;Cycle={cycle};Children={handlerLayout.Count}");
		};

		BindableLayout.SetItemsSource(bindableLayout, items);

		return new ContentPage
		{
			Title = "Child window",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label { Text = "Child window loaded with BindableLayout content" },
					bindableLayout
				}
			}
		};
	}
}
#endif


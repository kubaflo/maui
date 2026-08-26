#if WINDOWS
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	readonly Label _createdCycleLabel;
	readonly Label _destroyingCycleLabel;
	readonly Label _completedCycleLabel;
	readonly Label[] _cycleResultLabels;
	readonly Button _runCycleButton;
	int _nextCycle;

	public Issue33607()
	{
		_createdCycleLabel = new Label
		{
			Text = "-1",
			AutomationId = "CreatedCycleLabel"
		};

		_destroyingCycleLabel = new Label
		{
			Text = "-1",
			AutomationId = "DestroyingCycleLabel"
		};

		_completedCycleLabel = new Label
		{
			Text = "-1",
			AutomationId = "CompletedCycleLabel"
		};

		_cycleResultLabels =
		[
			new Label { Text = "Not run", AutomationId = "Cycle1ResultLabel" },
			new Label { Text = "Not run", AutomationId = "Cycle2ResultLabel" },
			new Label { Text = "Not run", AutomationId = "Cycle3ResultLabel" }
		];

		_runCycleButton = new Button
		{
			Text = "Run window close cycle",
			AutomationId = "RunCycleButton"
		};
		_runCycleButton.Clicked += OnRunCycleClicked;

		Content = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "Created cycle" },
				_createdCycleLabel,
				new Label { Text = "Destroying cycle" },
				_destroyingCycleLabel,
				new Label { Text = "Completed cycle" },
				_completedCycleLabel,
				_cycleResultLabels[0],
				_cycleResultLabels[1],
				_cycleResultLabels[2],
				_runCycleButton
			}
		};
	}

	void OnRunCycleClicked(object sender, EventArgs e)
	{
		var application = Application.Current ?? throw new InvalidOperationException("Application.Current must be available.");
		var cycle = ++_nextCycle;
		_runCycleButton.IsEnabled = false;

		var items = new ObservableCollection<string>();
		var affectedLayout = new VerticalStackLayout();
		Microsoft.Maui.ILayout layout = affectedLayout;

		items.CollectionChanged += (_, args) =>
			NotifyCollectionChangedEventArgsExtensions.Apply(
				args,
				(item, index, _) => ((Layout)layout).Insert(index, new Label { Text = (string)item }),
				(_, index) => ((Layout)layout).RemoveAt(index),
				() =>
				{
					((Layout)layout).Clear();
					foreach (var item in items)
						((Layout)layout).Add(new Label { Text = item });
				});
		items.Add("Affected item");

		var page = new ContentPage
		{
			Title = $"Affected window {cycle}",
			Content = new ContentView
			{
				Content = new Border
				{
					Content = affectedLayout
				}
			}
		};

		var affectedWindow = new Window
		{
			Page = page,
			Title = page.Title
		};

		affectedWindow.Created += (_, _) =>
		{
			_createdCycleLabel.Text = cycle.ToString();
			affectedWindow.Dispatcher.Dispatch(() => application.CloseWindow(affectedWindow));
		};

		affectedWindow.Destroying += (_, _) =>
		{
			_destroyingCycleLabel.Text = cycle.ToString();
			Dispatcher.Dispatch(() =>
			{
				var result = "Completed";
				try
				{
					items.Add($"Post-close item {cycle}");
				}
				catch (ObjectDisposedException)
				{
					result = nameof(ObjectDisposedException);
				}

				_cycleResultLabels[cycle - 1].Text = result;
				_completedCycleLabel.Text = cycle.ToString();
				_runCycleButton.IsEnabled = true;
			});
		};

		application.OpenWindow(affectedWindow);
	}
}
#endif


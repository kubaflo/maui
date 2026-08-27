#if WINDOWS
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	readonly List<ContentPage> _retainedPages = [];
	readonly Button _runCycleButton;
	readonly Label _stateLabel;
	readonly Label _completionLabel;
	int _cycleIndex = -1;

	public Issue33607()
	{
		_runCycleButton = new Button
		{
			Text = "Run window lifecycle cycle",
			AutomationId = "RunCycleButton"
		};
		_runCycleButton.Clicked += (_, _) => RunCycle();

		_stateLabel = new Label
		{
			Text = "Retained=0;Cycle=-1;Lifecycle=NotStarted;Exception=NotObserved;ObjectName=NotObserved;Mutation=False;InitialCount=-1;InitialText=None;FinalCount=-1;SecondText=None;Identity=False",
			AutomationId = "StateLabel"
		};
		_completionLabel = new Label
		{
			Text = "NotStarted",
			AutomationId = "CompletionLabel"
		};

		Content = new VerticalStackLayout
		{
			Children =
			{
				_runCycleButton,
				_stateLabel,
				_completionLabel
			}
		};
	}

	void RunCycle()
	{
		_runCycleButton.IsEnabled = false;
		_cycleIndex++;
		var cycle = _cycleIndex;
		var items = new ObservableCollection<string>
		{
			"Initial ILayout item"
		};
		var itemsLayout = new VerticalStackLayout();
		Microsoft.Maui.ILayout mauiLayout = itemsLayout;
		itemsLayout.Add(CreateItemLabel(items[0]));
		items.CollectionChanged += (_, args) => ApplyCollectionChange(args, mauiLayout, items);

		var page = new ContentPage
		{
			Title = "Issue 33607 secondary window",
			Content = new ContentView
			{
				Content = new Border
				{
					Content = itemsLayout
				}
			}
		};
		var window = new Window
		{
			Page = page,
			Title = page.Title
		};
		_retainedPages.Add(page);

		var loaded = false;
		var destroying = false;
		window.Destroying += (destroyingSender, destroyingArgs) =>
			destroying = ReferenceEquals(destroyingSender, window);

		var application = Application.Current;
		if (application is null)
			throw new InvalidOperationException("The test requires a running application.");

		EventHandler pageLoaded = null!;
		pageLoaded = (loadedSender, loadedArgs) =>
		{
			page.Loaded -= pageLoaded;
			loaded = ReferenceEquals(loadedSender, page);
			var initialCount = itemsLayout.Children.Count;
			var initialLabel = initialCount == 1 ? itemsLayout.Children[0] as Label : null;
			var initialText = initialLabel?.Text ?? "None";

			page.Dispatcher.Dispatch(() =>
			{
				application.CloseWindow(window);

				var mutationAttempted = true;
				var exceptionType = "NotObserved";
				var objectName = "NotObserved";
				try
				{
					items.Add($"Post-close item {cycle + 1}");
					exceptionType = "None";
					objectName = "None";
				}
				catch (ObjectDisposedException exception)
				{
					exceptionType = nameof(ObjectDisposedException);
					objectName = exception.ObjectName ?? "None";
				}

				var finalCount = items.Count;
				var secondLabel = itemsLayout.Children.Count > 1 ? itemsLayout.Children[1] as Label : null;
				var secondText = secondLabel?.Text ?? "None";
				var retainedIdentity = ReferenceEquals(_retainedPages[cycle], page);
				var lifecycle = $"{(loaded ? "Loaded" : "NotLoaded")},{(destroying ? "Destroying" : "NotDestroying")}";

				_stateLabel.Text = $"Retained={_retainedPages.Count};Cycle={cycle};Lifecycle={lifecycle};Exception={exceptionType};ObjectName={objectName};Mutation={mutationAttempted};InitialCount={initialCount};InitialText={initialText};FinalCount={finalCount};SecondText={secondText};Identity={retainedIdentity}";
				_completionLabel.Text = $"Cycle {cycle} complete";
				_runCycleButton.IsEnabled = true;
			});
		};
		page.Loaded += pageLoaded;

		application.OpenWindow(window);
	}

	static void ApplyCollectionChange(NotifyCollectionChangedEventArgs args, Microsoft.Maui.ILayout layout, IReadOnlyList<string> items)
	{
		if (layout is not Layout controlsLayout)
			throw new InvalidOperationException("The ILayout must be backed by a Controls Layout.");

		NotifyCollectionChangedEventArgsExtensions.Apply(
			args,
			(item, index, _) => controlsLayout.Insert(index, CreateItemLabel((string)item)),
			(_, index) => controlsLayout.RemoveAt(index),
			() =>
			{
				controlsLayout.Clear();
				foreach (var item in items)
					controlsLayout.Add(CreateItemLabel(item));
			});
	}

	static Label CreateItemLabel(string item)
	{
		var label = new Label
		{
			BindingContext = item
		};
		label.SetBinding(Label.TextProperty, ".");
		return label;
	}
}
#endif


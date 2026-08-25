using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35775, "IndicatorView leaks when linked to a CarouselView using a shared ObservableCollection", PlatformAffected.Android)]
public class Issue35775 : NavigationPage
{
	public Issue35775() : base(new Issue35775RootPage())
	{
	}
}

sealed class Issue35775RootPage : ContentPage
{
	const int VisitCount = 3;
	const int ExpectedReferenceCount = VisitCount * 2;
	const int FeedUpdateCount = 250;

	static readonly ObservableCollection<string> SharedFeed =
		new(Enumerable.Range(1, 120).Select(index => $"Feed item {index}"));

	readonly List<WeakReference> _snapshotReferences = [];
	readonly List<WeakReference> _sharedReferences = [];
	readonly Button _snapshotButton;
	readonly Button _sharedButton;
	readonly Button _retentionButton;
	readonly Label _snapshotCompletion;
	readonly Label _sharedCompletion;
	readonly Label _snapshotAlive;
	readonly Label _sharedAlive;
	readonly Label _retentionCompletion;
	int _snapshotPushes;
	int _snapshotPops;
	int _sharedPushes;
	int _sharedPops;

	public Issue35775RootPage()
	{
		Title = "IndicatorView collection retention";

		_snapshotCompletion = CreateStatusLabel("Issue35775SnapshotCompletion", "Snapshot pushes: 0; pops: 0; references: 0");
		_sharedCompletion = CreateStatusLabel("Issue35775SharedCompletion", "Shared pushes: 0; pops: 0; references: 0");
		_snapshotAlive = CreateStatusLabel("Issue35775SnapshotAlive", "Snapshot alive: -1");
		_sharedAlive = CreateStatusLabel("Issue35775SharedAlive", "Shared alive: -1");
		_retentionCompletion = CreateStatusLabel("Issue35775RetentionCompletion", "Retention check: not run");

		_snapshotButton = new Button
		{
			Text = "Run snapshot baseline",
			AutomationId = "Issue35775SnapshotButton"
		};
		_snapshotButton.Clicked += OnRunSnapshotClicked;

		_sharedButton = new Button
		{
			Text = "Run shared observable",
			AutomationId = "Issue35775SharedButton",
			IsEnabled = false
		};
		_sharedButton.Clicked += OnRunSharedClicked;

		_retentionButton = new Button
		{
			Text = "Check retained controls",
			AutomationId = "Issue35775RetentionButton",
			IsEnabled = false
		};
		_retentionButton.Clicked += OnCheckRetentionClicked;

		Content = new VerticalStackLayout
		{
			Padding = new Thickness(18),
			Spacing = 6,
			Children =
			{
				new Label
				{
					Text = "IndicatorView shared collection leak",
					FontAttributes = FontAttributes.Bold,
					FontSize = 20
				},
				_snapshotAlive,
				_sharedAlive,
				_snapshotButton,
				_snapshotCompletion,
				_sharedButton,
				_sharedCompletion,
				_retentionButton,
				_retentionCompletion
			}
		};
	}

	static Label CreateStatusLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			FontSize = 12,
			Text = text
		};

	async void OnRunSnapshotClicked(object sender, EventArgs e)
	{
		_snapshotButton.IsEnabled = false;
		var snapshot = SharedFeed.ToList();

		for (var visit = 1; visit <= VisitCount; visit++)
		{
			await VisitAndPopAsync(snapshot, _snapshotReferences, visit);
			_snapshotPushes++;
			_snapshotPops++;
		}

		_snapshotCompletion.Text = $"Snapshot pushes: {_snapshotPushes}; pops: {_snapshotPops}; references: {_snapshotReferences.Count}";
		_sharedButton.IsEnabled = true;
	}

	async void OnRunSharedClicked(object sender, EventArgs e)
	{
		_sharedButton.IsEnabled = false;

		for (var visit = 1; visit <= VisitCount; visit++)
		{
			await VisitAndPopAsync(SharedFeed, _sharedReferences, visit);
			_sharedPushes++;
			_sharedPops++;
		}

		_sharedCompletion.Text = $"Shared pushes: {_sharedPushes}; pops: {_sharedPops}; references: {_sharedReferences.Count}";
		_retentionButton.IsEnabled = true;
	}

	async Task VisitAndPopAsync(IEnumerable<string> items, List<WeakReference> references, int visit)
	{
		var indicator = new IndicatorView
		{
			HorizontalOptions = LayoutOptions.Center
		};
		var carousel = new CarouselView
		{
			BindingContext = items,
			HeightRequest = 180,
			IndicatorView = indicator,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				label.SetBinding(Label.TextProperty, ".");
				return new Border { Content = label };
			})
		};
		carousel.SetBinding(ItemsView.ItemsSourceProperty, ".");

		var page = new ContentPage
		{
			Title = $"Visit {visit}",
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 20,
				Children =
				{
					new Label
					{
						Text = $"Carousel and indicator visit {visit}",
						FontSize = 20
					},
					carousel,
					indicator
				}
			}
		};

		references.Add(new WeakReference(indicator));
		references.Add(new WeakReference(carousel));
		await Navigation.PushAsync(page);
		await Navigation.PopAsync();
	}

	async void OnCheckRetentionClicked(object sender, EventArgs e)
	{
		_retentionButton.IsEnabled = false;

		var snapshotAlive = await CountAliveAfterGcAsync(_snapshotReferences);
		var sharedAlive = await CountAliveAfterGcAsync(_sharedReferences);

		for (var update = 0; update < FeedUpdateCount; update++)
			SharedFeed[0] = $"Feed update {update + 1}";

		_snapshotAlive.Text = $"Snapshot alive: {snapshotAlive}/{ExpectedReferenceCount}";
		_sharedAlive.Text = $"Shared alive: {sharedAlive}/{ExpectedReferenceCount}";
		_retentionCompletion.Text =
			$"Retention checked: snapshot references {_snapshotReferences.Count}; shared references {_sharedReferences.Count}; updates {FeedUpdateCount}; navigation root {Navigation.NavigationStack.Count == 1}";
	}

	async Task<int> CountAliveAfterGcAsync(IReadOnlyList<WeakReference> references)
	{
		const int collectionAttempts = 5;

		for (var attempt = 0; attempt < collectionAttempts; attempt++)
		{
			GarbageCollectionHelper.Collect();
			await DispatchNextAsync();
		}

		return references.Count(reference => reference.IsAlive);
	}

	Task DispatchNextAsync()
	{
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!Dispatcher.Dispatch(() => completion.SetResult(true)))
			throw new InvalidOperationException("Unable to dispatch the next GC check.");

		return completion.Task;
	}
}


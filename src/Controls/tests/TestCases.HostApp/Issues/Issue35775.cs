#if ANDROID
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35775, "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection", PlatformAffected.Android)]
public class Issue35775 : NavigationPage
{
	static readonly ObservableCollection<string> s_sharedFeed = CreateSharedFeed();

	public Issue35775() : base(new Issue35775HomePage(s_sharedFeed))
	{
	}

	static ObservableCollection<string> CreateSharedFeed()
	{
		var feed = new ObservableCollection<string>();
		for (int i = 1; i <= 120; i++)
			feed.Add($"Feed item {i}");

		return feed;
	}
}

sealed class Issue35775HomePage : ContentPage
{
	readonly ObservableCollection<string> _sharedFeed;
	readonly List<WeakReference> _carouselReferences = [];
	readonly List<WeakReference> _indicatorReferences = [];
	readonly Label _completedVisitsLabel;
	readonly Label _mutationGenerationLabel;
	readonly Label _sourceCountLabel;
	readonly Label _carouselAliveLabel;
	readonly Label _indicatorAliveLabel;
	readonly Label _retiredUpdatesLabel;
	int _completedVisits;

	public Issue35775HomePage(ObservableCollection<string> sharedFeed)
	{
		_sharedFeed = sharedFeed;
		TrackingIssue35775IndicatorView.Reset();

		_completedVisitsLabel = CreateMeasurementLabel("Issue35775CompletedVisits", "0");
		_mutationGenerationLabel = CreateMeasurementLabel("Issue35775MutationGeneration", "-1");
		_sourceCountLabel = CreateMeasurementLabel("Issue35775SourceCount", _sharedFeed.Count.ToString(CultureInfo.InvariantCulture));
		_carouselAliveLabel = CreateMeasurementLabel("Issue35775CarouselAlive", "0");
		_indicatorAliveLabel = CreateMeasurementLabel("Issue35775IndicatorAlive", "0");
		_retiredUpdatesLabel = CreateMeasurementLabel("Issue35775RetiredUpdates", "0");

		var createButton = new Button
		{
			AutomationId = "Issue35775CreateButton",
			Text = "Create leak page"
		};
		createButton.Clicked += OnCreateClicked;

		var updateButton = new Button
		{
			AutomationId = "Issue35775UpdateButton",
			Text = "Update shared feed"
		};
		updateButton.Clicked += OnUpdateClicked;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "IndicatorView shared feed leak"
					},
					new Label
					{
						Text = "Each visit creates linked controls bound to the same rooted collection."
					},
					createButton,
					updateButton,
					_completedVisitsLabel,
					_mutationGenerationLabel,
					_sourceCountLabel,
					_carouselAliveLabel,
					_indicatorAliveLabel,
					_retiredUpdatesLabel
				}
			}
		};
	}

	static Label CreateMeasurementLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	async void OnCreateClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new Issue35775LeakPage(_sharedFeed, OnPagePopped), false);
	}

	void OnPagePopped(WeakReference carouselReference, WeakReference indicatorReference)
	{
		_carouselReferences.Add(carouselReference);
		_indicatorReferences.Add(indicatorReference);
		_completedVisits++;
		_completedVisitsLabel.Text = _completedVisits.ToString(CultureInfo.InvariantCulture);
	}

	async void OnUpdateClicked(object sender, EventArgs e)
	{
		var references = new List<WeakReference>(_carouselReferences.Count + _indicatorReferences.Count);
		references.AddRange(_carouselReferences);
		references.AddRange(_indicatorReferences);
		await AssertionExtensions.WaitForGC(references.ToArray());

		TrackingIssue35775IndicatorView.Reset();
		_sharedFeed.Add($"Feed item {_sharedFeed.Count + 1}");

		_carouselAliveLabel.Text = CountAlive(_carouselReferences).ToString(CultureInfo.InvariantCulture);
		_indicatorAliveLabel.Text = CountAlive(_indicatorReferences).ToString(CultureInfo.InvariantCulture);
		_retiredUpdatesLabel.Text = TrackingIssue35775IndicatorView.RetiredUpdates.ToString(CultureInfo.InvariantCulture);
		_sourceCountLabel.Text = _sharedFeed.Count.ToString(CultureInfo.InvariantCulture);
		_mutationGenerationLabel.Text = "1";
	}

	static int CountAlive(List<WeakReference> references)
	{
		int count = 0;
		foreach (var reference in references)
		{
			if (reference.IsAlive)
				count++;
		}

		return count;
	}
}

sealed class Issue35775LeakPage : ContentPage
{
	readonly CarouselView _carouselView;
	readonly TrackingIssue35775IndicatorView _indicatorView;
	readonly Action<WeakReference, WeakReference> _onPopped;

	public Issue35775LeakPage(
		ObservableCollection<string> sharedFeed,
		Action<WeakReference, WeakReference> onPopped)
	{
		_onPopped = onPopped;
		Title = "Shared observable feed";

		var payload = new Issue35775RetentionPayload();
		_carouselView = new CarouselView
		{
			AutomationId = "Issue35775CarouselView",
			HeightRequest = 220,
			ItemsSource = sharedFeed,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		_indicatorView = new TrackingIssue35775IndicatorView
		{
			AutomationId = "Issue35775IndicatorView",
			HorizontalOptions = LayoutOptions.Center
		};
		_carouselView.IndicatorView = _indicatorView;
		_carouselView.Behaviors.Add(new Issue35775RetentionPayloadBehavior(payload));
		_indicatorView.Behaviors.Add(new Issue35775RetentionPayloadBehavior(payload));

		var readyLabel = new Label
		{
			AutomationId = "Issue35775Ready",
			Text = "Waiting for loaded handlers"
		};

		var popButton = new Button
		{
			AutomationId = "Issue35775PopButton",
			Text = "Pop leak page"
		};
		popButton.Clicked += OnPopClicked;

		Content = new VerticalStackLayout
		{
			Padding = new Thickness(24),
			Spacing = 16,
			Children =
			{
				readyLabel,
				new Label { Text = "Shared feed: 120 items" },
				_carouselView,
				_indicatorView,
				popButton
			}
		};

		Loaded += OnLoaded;

		void OnLoaded(object sender, EventArgs e)
		{
			Loaded -= OnLoaded;
			bool handlersReady =
				_carouselView.Handler?.PlatformView is not null &&
				_indicatorView.Handler?.PlatformView is not null;
			bool sourceReady =
				ReferenceEquals(_carouselView.ItemsSource, sharedFeed) &&
				sharedFeed.Count == 120;
			bool linkReady = ReferenceEquals(_indicatorView.ItemsSource, sharedFeed);

			readyLabel.Text = handlersReady && sourceReady && linkReady
				? "Leak page ready"
				: $"Setup invalid: handlers={handlersReady}, source={sourceReady}, link={linkReady}";
		}
	}

	async void OnPopClicked(object sender, EventArgs e)
	{
		_indicatorView.MarkRetired();
		await Navigation.PopAsync(false);
		_onPopped(new WeakReference(_carouselView), new WeakReference(_indicatorView));
	}
}

sealed class Issue35775RetentionPayload
{
	public byte[] Data { get; } = new byte[1024 * 1024];
}

sealed class Issue35775RetentionPayloadBehavior : Behavior<VisualElement>
{
	public Issue35775RetentionPayloadBehavior(Issue35775RetentionPayload payload)
	{
		Payload = payload;
	}

	public Issue35775RetentionPayload Payload { get; }
}

sealed class TrackingIssue35775IndicatorView : IndicatorView
{
	static int s_retiredUpdates;
	bool _retired;

	public static int RetiredUpdates => s_retiredUpdates;

	public static void Reset() => s_retiredUpdates = 0;

	public void MarkRetired() => _retired = true;

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (_retired && propertyName == nameof(Count))
			s_retiredUpdates++;
	}
}

static class AssertionExtensions
{
	public static async Task WaitForGC(WeakReference[] references)
	{
		for (int attempt = 0; attempt < 40 && AnyAlive(references); attempt++)
		{
			await Task.Yield();
			GarbageCollectionHelper.Collect();
			await Task.Yield();
		}
	}

	static bool AnyAlive(WeakReference[] references)
	{
		foreach (var reference in references)
		{
			if (reference.IsAlive)
				return true;
		}

		return false;
	}
}
#endif


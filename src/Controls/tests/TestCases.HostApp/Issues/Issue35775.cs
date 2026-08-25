#if ANDROID
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35775, "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection", PlatformAffected.Android)]
public class Issue35775 : NavigationPage
{
	public Issue35775() : base(new Issue35775RootPage())
	{
	}
}

sealed class Issue35775RootPage : ContentPage
{
	const int MeasuredVisits = 2;
	const int TotalVisits = 3;

	static readonly ObservableCollection<string> SharedFeed =
		new(Enumerable.Range(1, 120).Select(index => $"Feed item {index}"));

	readonly List<WeakReference> _controlReferences = [];
	readonly List<WeakReference> _payloadReferences = [];
	readonly Button _checkButton;
	readonly Label _stateLabel;
	int _visits;
	int _completedPops;
	int _collectionGeneration = -1;
	int _aliveControls = -1;
	int _alivePayloads = -1;

	public Issue35775RootPage()
	{
		Title = "IndicatorView leak";

		var openButton = new Button
		{
			AutomationId = "Issue35775OpenButton",
			Text = "Open shared collection page"
		};
		openButton.Clicked += OnOpenClicked;

		_checkButton = new Button
		{
			AutomationId = "Issue35775CheckButton",
			Text = "Check collected controls",
			IsEnabled = false
		};
		_checkButton.Clicked += OnCheckClicked;

		_stateLabel = new Label
		{
			AutomationId = "Issue35775State",
			HorizontalTextAlignment = TextAlignment.Center
		};
		UpdateState();

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "IndicatorView shared collection leak",
					FontSize = 22,
					HorizontalOptions = LayoutOptions.Center
				},
				openButton,
				_checkButton,
				_stateLabel
			}
		};
	}

	void UpdateState()
	{
		_stateLabel.Text =
			$"Completed pops: {_completedPops}\n" +
			$"Tracked controls: {_controlReferences.Count}\n" +
			$"Tracked payloads: {_payloadReferences.Count}\n" +
			$"Collection generation: {_collectionGeneration}\n" +
			$"Alive controls: {_aliveControls}\n" +
			$"Alive payloads: {_alivePayloads}";
	}

	async void OnOpenClicked(object sender, EventArgs e)
	{
		if (_visits >= TotalVisits)
			return;

		_visits++;
		var page = new Issue35775LeakPage(
			SharedFeed,
			_visits <= MeasuredVisits,
			TrackPageObjects,
			OnPagePopped);
		await Navigation.PushAsync(page);
	}

	void TrackPageObjects(CarouselView carousel, IndicatorView indicator, Behavior carouselPayload, Behavior indicatorPayload)
	{
		_controlReferences.Add(new WeakReference(carousel));
		_controlReferences.Add(new WeakReference(indicator));
		_payloadReferences.Add(new WeakReference(carouselPayload));
		_payloadReferences.Add(new WeakReference(indicatorPayload));
		UpdateState();
	}

	void OnPagePopped()
	{
		_completedPops++;
		UpdateState();
		_checkButton.IsEnabled = _completedPops == TotalVisits;
	}

	async void OnCheckClicked(object sender, EventArgs e)
	{
		_checkButton.IsEnabled = false;

		var references = _controlReferences.Concat(_payloadReferences).ToArray();
		try
		{
			await GarbageCollectionHelper.WaitForGC(references);
		}
		catch (Exception exception) when (exception.Message == "Assertion timed out")
		{
			// A timeout is the measured leak outcome; unexpected exceptions must still fail the test.
		}

		_aliveControls = _controlReferences.Count(reference => reference.IsAlive);
		_alivePayloads = _payloadReferences.Count(reference => reference.IsAlive);
		_collectionGeneration = 1;
		UpdateState();
	}
}

sealed class Issue35775LeakPage : ContentPage
{
	public Issue35775LeakPage(
		ObservableCollection<string> sharedFeed,
		bool trackObjects,
		Action<CarouselView, IndicatorView, Behavior, Behavior> trackPageObjects,
		Action pagePopped)
	{
		Title = "Shared collection page";

		var carousel = new CarouselView
		{
			AutomationId = "Issue35775Carousel",
			ItemsSource = sharedFeed,
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				itemLabel.SetBinding(Label.TextProperty, ".");
				return itemLabel;
			})
		};

		var indicator = new IndicatorView
		{
			AutomationId = "Issue35775Indicator"
		};
		carousel.IndicatorView = indicator;

		var carouselPayload = new Issue35775PayloadBehavior();
		var indicatorPayload = new Issue35775PayloadBehavior();
		carousel.Behaviors.Add(carouselPayload);
		indicator.Behaviors.Add(indicatorPayload);
		if (trackObjects)
			trackPageObjects(carousel, indicator, carouselPayload, indicatorPayload);

		var closeButton = new Button
		{
			AutomationId = "Issue35775PopButton",
			Text = "Pop shared collection page"
		};
		closeButton.Clicked += async (sender, args) =>
		{
			await Navigation.PopAsync();
			pagePopped();
		};

		var layout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};

		layout.Add(new Label
		{
			Text = "Shared rooted ObservableCollection: 120 items",
			HorizontalOptions = LayoutOptions.Center
		}, 0, 0);
		layout.Add(carousel, 0, 1);
		layout.Add(indicator, 0, 2);
		layout.Add(new Label
		{
			Text = "Default IndicatorView linked to CarouselView",
			HorizontalOptions = LayoutOptions.Center
		}, 0, 3);
		layout.Add(closeButton, 0, 4);
		Content = layout;
	}
}

sealed class Issue35775PayloadBehavior : Behavior<VisualElement>
{
	public byte[] Payload { get; } = new byte[512 * 1024];
}
#endif


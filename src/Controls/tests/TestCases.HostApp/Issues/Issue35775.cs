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
	const int RequiredVisits = 4;

	readonly ObservableCollection<string> _sharedFeed =
		new(Enumerable.Range(1, 120).Select(index => $"Item {index}"));
	readonly List<WeakReference> _indicatorReferences = [];
	readonly List<WeakReference> _carouselReferences = [];
	readonly Label _visitsLabel;
	readonly Label _collectionResultLabel;
	int _completedVisits;
	int _createdVisits;

	public Issue35775RootPage()
	{
		Title = "IndicatorView leak";

		_visitsLabel = new Label
		{
			AutomationId = "Issue35775VisitsLabel",
			Text = $"Completed visits: 0 of {RequiredVisits}"
		};

		_collectionResultLabel = new Label
		{
			AutomationId = "Issue35775CollectionResultLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "Collection check pending"
		};

		var createButton = new Button
		{
			AutomationId = "Issue35775CreateButton",
			Text = "Create leak page"
		};
		createButton.Clicked += OnCreatePageClicked;

		var checkButton = new Button
		{
			AutomationId = "Issue35775CheckButton",
			Text = "Check collected controls"
		};
		checkButton.Clicked += OnCheckCollectedControlsClicked;

		Content = new VerticalStackLayout
		{
			Padding = new Thickness(24),
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 22,
					Text = "IndicatorView shared-feed leak"
				},
				new Label
				{
					Text = "Each visit creates a CarouselView linked to an IndicatorView using the same rooted ObservableCollection."
				},
				createButton,
				checkButton,
				_visitsLabel,
				_collectionResultLabel
			}
		};
	}

	async void OnCreatePageClicked(object sender, EventArgs e)
	{
		_createdVisits++;
		await Navigation.PushAsync(CreateLeakPage(_createdVisits));
	}

	ContentPage CreateLeakPage(int visit)
	{
		var indicatorView = new IndicatorView
		{
			AutomationId = $"Issue35775IndicatorView{visit}"
		};
		var carouselView = new CarouselView
		{
			AutomationId = $"Issue35775CarouselView{visit}",
			BindingContext = _sharedFeed
		};
		carouselView.SetBinding(ItemsView.ItemsSourceProperty, new Binding("."));
		carouselView.IndicatorView = indicatorView;

		_indicatorReferences.Add(new WeakReference(indicatorView));
		_carouselReferences.Add(new WeakReference(carouselView));

		var loadedLabel = new Label
		{
			AutomationId = "Issue35775LoadedLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "Shared observable leak page"
		};
		var carouselLoaded = false;
		var indicatorLoaded = false;
		void UpdateLoadedState()
		{
			if (carouselLoaded && indicatorLoaded)
				loadedLabel.Text = $"Loaded linked controls: {visit}";
		}

		carouselView.Loaded += (_, _) =>
		{
			carouselLoaded = true;
			UpdateLoadedState();
		};
		indicatorView.Loaded += (_, _) =>
		{
			indicatorLoaded = true;
			UpdateLoadedState();
		};

		var popButton = new Button
		{
			AutomationId = "Issue35775PopButton",
			Text = "Pop leak page"
		};
		popButton.Clicked += async (_, _) =>
		{
			await Navigation.PopAsync();
			_completedVisits++;
			_visitsLabel.Text = $"Completed visits: {_completedVisits} of {RequiredVisits}";
		};

		return new ContentPage
		{
			Title = "Shared observable leak page",
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 16,
				Children =
				{
					loadedLabel,
					new Label
					{
						Text = "CarouselView and linked IndicatorView use the rooted shared feed."
					},
					carouselView,
					indicatorView,
					popButton
				}
			}
		};
	}

	async void OnCheckCollectedControlsClicked(object sender, EventArgs e)
	{
		_collectionResultLabel.Text = "Collection check running";

		var references = _indicatorReferences.Concat(_carouselReferences).ToArray();
		try
		{
			await GarbageCollectionHelper.WaitForGC(5000, references);
		}
		catch (Exception exception) when (exception.Message == "Assertion timed out")
		{
		}

		var retainedIndicators = _indicatorReferences.Count(reference => reference.IsAlive);
		var retainedCarousels = _carouselReferences.Count(reference => reference.IsAlive);
		_collectionResultLabel.Text =
			$"IndicatorViews {retainedIndicators}/{_indicatorReferences.Count}; " +
			$"CarouselViews {retainedCarousels}/{_carouselReferences.Count}";
	}
}


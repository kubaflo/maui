#if ANDROID
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35775, "IndicatorView leaks when linked to a CarouselView with a shared ObservableCollection", PlatformAffected.Android)]
public class Issue35775 : NavigationPage
{
	static readonly ObservableCollection<string> SharedFeed =
		new(Enumerable.Range(1, 120).Select(index => $"Item {index}"));

	public Issue35775() : base(new LeakRootPage())
	{
	}

	sealed class LeakRootPage : ContentPage
	{
		readonly List<WeakReference> _carouselReferences = [];
		readonly List<WeakReference> _indicatorReferences = [];
		readonly Label _visitCountLabel;
		readonly Label _linkedStateLabel;
		readonly Label _gcStateLabel;
		readonly Label _carouselCountLabel;
		readonly Label _indicatorCountLabel;
		int _completedVisits;
		bool _lastControlsLinked;
		bool _lastSourceShared;

		public LeakRootPage()
		{
			_visitCountLabel = new Label
			{
				AutomationId = "Issue35775VisitCount",
				Text = "Completed visits: 0"
			};
			_linkedStateLabel = new Label
			{
				AutomationId = "Issue35775LinkedState",
				Text = "Linked controls: False; shared source: False"
			};
			_gcStateLabel = new Label
			{
				AutomationId = "Issue35775GcState",
				Text = "GC state: Not checked"
			};
			_carouselCountLabel = new Label
			{
				AutomationId = "Issue35775CarouselCount",
				Text = "Retained CarouselViews: not checked"
			};
			_indicatorCountLabel = new Label
			{
				AutomationId = "Issue35775IndicatorCount",
				Text = "Retained IndicatorViews: not checked"
			};

			var openButton = new Button
			{
				AutomationId = "Issue35775OpenButton",
				Text = "Open leak page"
			};
			openButton.Clicked += OnOpenLeakPage;

			var collectButton = new Button
			{
				AutomationId = "Issue35775CollectButton",
				Text = "Force GC and check"
			};
			collectButton.Clicked += OnCollect;

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label { Text = "Shared ObservableCollection leak", FontSize = 20 },
					new Label
					{
						AutomationId = "Issue35775FeedCount",
						Text = $"Shared feed count: {SharedFeed.Count}"
					},
					_visitCountLabel,
					_linkedStateLabel,
					_gcStateLabel,
					_carouselCountLabel,
					_indicatorCountLabel,
					openButton,
					collectButton
				}
			};
		}

		async void OnOpenLeakPage(object sender, EventArgs e)
		{
			var indicator = new IndicatorView();
			var carousel = new CarouselView
			{
				ItemsSource = SharedFeed,
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label
					{
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					};
					label.SetBinding(Label.TextProperty, ".");
					return label;
				})
			};
			carousel.IndicatorView = indicator;

			_lastControlsLinked = ReferenceEquals(indicator.ItemsSource, SharedFeed);
			_lastSourceShared = ReferenceEquals(carousel.ItemsSource, SharedFeed);
			_linkedStateLabel.Text = $"Linked controls: {_lastControlsLinked}; shared source: {_lastSourceShared}";
			_carouselReferences.Add(new WeakReference(carousel));
			_indicatorReferences.Add(new WeakReference(indicator));

			var readyLabel = new Label
			{
				AutomationId = "Issue35775LeakPageReady",
				HorizontalOptions = LayoutOptions.Center,
				Text = "Leak page loading"
			};
			var content = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				},
				Children = { readyLabel, carousel, indicator }
			};
			Grid.SetRow(carousel, 1);
			Grid.SetRow(indicator, 2);

			var page = new ContentPage
			{
				Title = "Leak page",
				Content = content
			};
			page.Loaded += (s, args) => readyLabel.Text = "Leak page loaded";
			page.Disappearing += (s, args) =>
			{
				_completedVisits++;
				_visitCountLabel.Text = $"Completed visits: {_completedVisits}";
			};

			await Navigation.PushAsync(page);
		}

		async void OnCollect(object sender, EventArgs e)
		{
			_gcStateLabel.Text = "GC state: Running";
			await GarbageCollectionHelper.WaitForGC(Array.Empty<WeakReference>());
			GarbageCollectionHelper.Collect();

			var retainedCarousels = _carouselReferences.Count(reference => reference.IsAlive);
			var retainedIndicators = _indicatorReferences.Count(reference => reference.IsAlive);
			_carouselCountLabel.Text = $"Retained CarouselViews: {retainedCarousels}/{_carouselReferences.Count}";
			_indicatorCountLabel.Text = $"Retained IndicatorViews: {retainedIndicators}/{_indicatorReferences.Count}";
			_gcStateLabel.Text = "GC state: Complete";
		}
	}
}
#endif


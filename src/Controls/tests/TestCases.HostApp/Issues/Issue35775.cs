#if ANDROID
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35775, "IndicatorView leaks when CarouselView.IndicatorView is bound to a shared ObservableCollection", PlatformAffected.Android)]
public class Issue35775 : NavigationPage
{
	public Issue35775() : base(CreateRootPage())
	{
	}

	static ContentPage CreateRootPage()
	{
		var sharedFeed = new ObservableCollection<string>();
		var indicatorReferences = new List<WeakReference>();
		var carouselReferences = new List<WeakReference>();
		var behaviorReferences = new List<WeakReference>();
		var allReferences = new List<WeakReference>();
		var pagesVisited = 0;

		for (int i = 1; i <= 120; i++)
			sharedFeed.Add($"Feed item {i}");

		var visitCountLabel = new Label
		{
			AutomationId = "Issue35775VisitCount",
			Text = "Pages visited: 0",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var gcStatusLabel = new Label
		{
			AutomationId = "Issue35775GcStatus",
			Text = "GC not started",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var trackedCountLabel = new Label
		{
			AutomationId = "Issue35775TrackedCount",
			Text = "Tracked: IndicatorViews=0, CarouselViews=0, behaviors=0",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var indicatorAliveLabel = new Label
		{
			AutomationId = "Issue35775IndicatorAlive",
			Text = "IndicatorViews alive: 0",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var carouselAliveLabel = new Label
		{
			AutomationId = "Issue35775CarouselAlive",
			Text = "CarouselViews alive: 0",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var behaviorAliveLabel = new Label
		{
			AutomationId = "Issue35775BehaviorAlive",
			Text = "Payload behaviors alive: 0",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var resultLabel = new Label
		{
			AutomationId = "Issue35775Result",
			Text = "Collection check not run",
			HorizontalTextAlignment = TextAlignment.Center
		};

		var openPageButton = new Button
		{
			AutomationId = "Issue35775OpenPage",
			Text = "Open shared feed page"
		};
		openPageButton.Clicked += async (_, _) =>
		{
			var indicatorView = new IndicatorView
			{
				HorizontalOptions = LayoutOptions.Center
			};
			var indicatorBehavior = new ControlPayloadBehavior();
			indicatorView.Behaviors.Add(indicatorBehavior);

			var carouselView = new CarouselView
			{
				HeightRequest = 250,
				HorizontalOptions = LayoutOptions.Fill,
				IndicatorView = indicatorView,
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
			var carouselBehavior = new ControlPayloadBehavior();
			carouselView.Behaviors.Add(carouselBehavior);

			Track(indicatorView, indicatorReferences, allReferences);
			Track(carouselView, carouselReferences, allReferences);
			Track(indicatorBehavior, behaviorReferences, allReferences);
			Track(carouselBehavior, behaviorReferences, allReferences);

			pagesVisited++;
			visitCountLabel.Text = $"Pages visited: {pagesVisited}";

			await openPageButton.Navigation.PushAsync(new ContentPage
			{
				Title = "Shared observable feed",
				Content = new VerticalStackLayout
				{
					Padding = 20,
					Spacing = 16,
					Children =
					{
						new Label
						{
							AutomationId = "Issue35775LoadedMarker",
							Text = "Shared observable feed controls",
							FontAttributes = FontAttributes.Bold,
							FontSize = 20,
							HorizontalTextAlignment = TextAlignment.Center
						},
						carouselView,
						indicatorView
					}
				}
			});
		};

		var checkCollectionButton = new Button
		{
			AutomationId = "Issue35775CheckCollection",
			Text = "Check collected controls"
		};
		checkCollectionButton.Clicked += async (_, _) =>
		{
			gcStatusLabel.Text = "GC running";
			var gcTask = GarbageCollectionHelper.WaitForGC(5000, allReferences.ToArray());
			await Task.WhenAny(gcTask);

			if (gcTask.IsFaulted)
				_ = gcTask.Exception;

			trackedCountLabel.Text = $"Tracked: IndicatorViews={indicatorReferences.Count}, CarouselViews={carouselReferences.Count}, behaviors={behaviorReferences.Count}";
			indicatorAliveLabel.Text = $"IndicatorViews alive: {CountAlive(indicatorReferences)}";
			carouselAliveLabel.Text = $"CarouselViews alive: {CountAlive(carouselReferences)}";
			behaviorAliveLabel.Text = $"Payload behaviors alive: {CountAlive(behaviorReferences)}";
			resultLabel.Text = "Collection check completed";
			gcStatusLabel.Text = "GC completed";
		};

		return new ContentPage
		{
			Title = "IndicatorView leak",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "IndicatorView shared feed leak",
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
						HorizontalTextAlignment = TextAlignment.Center
					},
					openPageButton,
					checkCollectionButton,
					visitCountLabel,
					gcStatusLabel,
					trackedCountLabel,
					indicatorAliveLabel,
					carouselAliveLabel,
					behaviorAliveLabel,
					resultLabel
				}
			}
		};
	}

	static void Track(object target, List<WeakReference> categoryReferences, List<WeakReference> allReferences)
	{
		var reference = new WeakReference(target);
		categoryReferences.Add(reference);
		allReferences.Add(reference);
	}

	static int CountAlive(List<WeakReference> references) =>
		references.Count(reference => reference.IsAlive);

	sealed class ControlPayloadBehavior : Behavior<VisualElement>
	{
		readonly byte[] _controlPayload = new byte[512 * 1024];

		protected override void OnAttachedTo(VisualElement bindable)
		{
			base.OnAttachedTo(bindable);
			GC.KeepAlive(_controlPayload);
		}
	}
}
#endif


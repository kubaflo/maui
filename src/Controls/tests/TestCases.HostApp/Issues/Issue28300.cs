#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28300, "Custom busy indicator remains visible after loading completes", PlatformAffected.iOS)]
public class Issue28300 : NavigationPage
{
	const string AnimationName = "CustomBusyIndicatorAnimation";

	public Issue28300() : base(new MainPage())
	{
	}

	sealed class MainPage : ContentPage
	{
		public MainPage()
		{
			Title = "Wizard";

			var startWizardButton = new Button
			{
				AutomationId = "StartWizardButton",
				Text = "Start Wizard (TabView)"
			};

			startWizardButton.Clicked += async (_, _) =>
			{
				startWizardButton.IsEnabled = false;
				await Navigation.PushAsync(new WizardPage());
			};

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 24,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Wizard sample"
					},
					startWizardButton,
					new Label
					{
						AutomationId = "NavigationStatus",
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "NavigationPending"
					}
				}
			};
		}
	}

	sealed class WizardPage : ContentPage
	{
		readonly CustomBusyIndicator _busyIndicator;
		readonly Label _indicatorStatus;
		readonly Label _animationStatus;
		readonly Label _stopStatus;
		bool _loadingCompleted;

		public WizardPage()
		{
			Title = "Wizard";

			_busyIndicator = new CustomBusyIndicator();
			_indicatorStatus = CreateStatusLabel("IndicatorStatus", "IndicatorAttachedAndVisible=NotObserved");
			_animationStatus = CreateStatusLabel("AnimationStatus", "AnimationStarted=NotObserved");
			_stopStatus = CreateStatusLabel("StopStatus", "StopRequested=NotObserved");

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						AutomationId = "WizardPageLoadedLabel",
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Wizard page loaded"
					},
					_busyIndicator,
					new Label
					{
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Loading is complete; the busy indicator should already be hidden."
					},
					_indicatorStatus,
					_animationStatus,
					_stopStatus
				}
			};

			Loaded += (_, _) => OnLoaded();
		}

		static Label CreateStatusLabel(string automationId, string text) =>
			new()
			{
				AutomationId = automationId,
				HorizontalTextAlignment = TextAlignment.Center,
				Text = text
			};

		void OnLoaded()
		{
			if (_loadingCompleted)
				return;

			_loadingCompleted = true;
			_busyIndicator.Start();

			if (_busyIndicator.Handler is not null && _busyIndicator.IsVisible)
				_indicatorStatus.Text = "IndicatorAttachedAndVisible=True";

			if (_busyIndicator.AnimationIsRunning(AnimationName))
				_animationStatus.Text = "AnimationStarted=True";

			Dispatcher.Dispatch(() =>
			{
				_busyIndicator.RequestStop();
				_stopStatus.Text = "StopRequested=True";
			});
		}
	}

	sealed class CustomBusyIndicator : Grid
	{
		bool _isRunning;

		public CustomBusyIndicator()
		{
			AutomationId = "CustomBusyIndicator";
			HeightRequest = 72;
			HorizontalOptions = LayoutOptions.Center;
			WidthRequest = 72;

			Children.Add(new BoxView
			{
				Color = Colors.DodgerBlue,
				CornerRadius = 4,
				HeightRequest = 56,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 10
			});
			Children.Add(new BoxView
			{
				Color = Colors.DodgerBlue,
				CornerRadius = 4,
				HeightRequest = 10,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 56
			});
		}

		public void Start()
		{
			_isRunning = true;
			IsVisible = true;

			var animation = new Animation(value => Rotation = value, 0, 360);
			animation.Commit(
				this,
				AnimationName,
				rate: 16,
				length: 10000,
				easing: Easing.Linear,
				finished: (_, _) =>
				{
					if (!_isRunning)
						IsVisible = false;
				},
				repeat: () => _isRunning);
		}

		public void RequestStop()
		{
			_isRunning = false;
		}
	}
}
#endif


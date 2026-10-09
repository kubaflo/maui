#if IOS
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29628, "Deadlock with modal navigation and animation", PlatformAffected.iOS)]
public class Issue29628 : ContentPage
{
	const string AnimationName = "LoadingBannerAnimation";

	readonly Grid _root;
	readonly VerticalStackLayout _loadingBanner;
	readonly Label _loadingLabel;
	readonly Button _openModalButton;
	readonly VerticalStackLayout _telemetry;
	readonly Label _animationStartedLabel;
	readonly Label _cancellationCountLabel;

	bool _animationRunning = true;
	bool _cancellingBanner;
	int _cancelledCompletions;

	public Issue29628()
	{
		_loadingLabel = new Label
		{
			Text = "Loading..."
		};

		_loadingBanner = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new Label { Text = "Background work is loading" },
				_loadingLabel
			}
		};

		_openModalButton = new Button
		{
			Text = "Open fast-loading modal",
			AutomationId = "OpenFastModal"
		};
		_openModalButton.Clicked += OnOpenModalClicked;

		_animationStartedLabel = new Label
		{
			Text = "AnimationNotStarted",
			AutomationId = "AnimationStartedToken"
		};
		_cancellationCountLabel = new Label
		{
			Text = "-1",
			AutomationId = "CancellationCount"
		};
		_telemetry = new VerticalStackLayout
		{
			Children =
			{
				_animationStartedLabel,
				_cancellationCountLabel
			}
		};

		_root = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 24,
			Children =
			{
				_loadingBanner,
				_openModalButton,
				_telemetry
			}
		};
		Grid.SetRow(_openModalButton, 1);
		Grid.SetRow(_telemetry, 2);

		Content = _root;
		Loaded += OnPageLoaded;
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		Loaded -= OnPageLoaded;
		StartBannerAnimation();
		_animationStartedLabel.Text = "AttachedAnimationStarted";
	}

	void StartBannerAnimation()
	{
		_loadingLabel.Animate(
			AnimationName,
			value => _loadingLabel.TranslationX = (value * 40) - 20,
			rate: 16,
			length: 300,
			easing: Easing.Linear,
			finished: (_, cancelled) =>
			{
				if (!_animationRunning)
					return;

				if (cancelled && _cancellingBanner)
				{
					_cancelledCompletions++;
					_cancellationCountLabel.Text = _cancelledCompletions.ToString(CultureInfo.InvariantCulture);

					if (_cancelledCompletions >= 2)
					{
						_animationRunning = false;
						return;
					}
				}

				StartBannerAnimation();
			});
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		_openModalButton.IsEnabled = false;

		var modalLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 24,
			Children =
			{
				new Label { Text = "Loading page..." }
			}
		};
		var modal = new ContentPage
		{
			Content = modalLayout
		};

		modal.Loaded += (_, _) =>
		{
			_cancellingBanner = true;
			_cancelledCompletions = 0;
			_root.Children.Remove(_loadingBanner);
			_root.Children.Remove(_telemetry);
			modalLayout.Children.Add(_telemetry);
			modalLayout.Children.Add(new Label
			{
				Text = "ModalLoaded",
				AutomationId = "ModalLoadedToken"
			});

			_loadingLabel.AbortAnimation(AnimationName);

			Dispatcher.Dispatch(() => modalLayout.Children.Add(new Label
			{
				Text = "PostCancellationDispatchCompleted",
				AutomationId = "PostCancellationDispatchToken"
			}));
		};

		await Navigation.PushModalAsync(modal);
	}
}
#endif


#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	const string InitialResult = "Destroying=-1;Attempts=-1";

	readonly Label _lifecycleResultLabel;
	readonly VerticalStackLayout _primaryLayout;
	Window _childWindow;
	Border _savePopup;
	bool _closeQueued;
	int _destroyingCount = -1;
	int _animationAttempts = -1;

	public Issue34057()
	{
		Title = "Issue 34057";

		_lifecycleResultLabel = new Label
		{
			AutomationId = "Issue34057Result",
			FontAttributes = FontAttributes.Bold,
			Text = InitialResult
		};

		var triggerButton = new Button
		{
			AutomationId = "Issue34057Trigger",
			Text = "Run child window close scenario"
		};
		triggerButton.Clicked += OnRunScenarioClicked;

		_primaryLayout = new VerticalStackLayout
		{
			Padding = 32,
			Spacing = 20,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					Text = "Child window animation disposal"
				},
				new Label
				{
					Text = "The child window briefly shows a popup, closes, and then runs its pending hide animation."
				},
				_lifecycleResultLabel,
				triggerButton
			}
		};

		Content = _primaryLayout;
	}

	void OnRunScenarioClicked(object sender, EventArgs e)
	{
		_destroyingCount = 0;
		_animationAttempts = 0;
		_closeQueued = false;
		_lifecycleResultLabel.Text = "Destroying=0;Attempts=0;Exception=Pending";

		_savePopup = new Border
		{
			AutomationId = "Issue34057Popup",
			BackgroundColor = Colors.White,
			Content = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 22,
				Text = "Save popup"
			},
			HeightRequest = 180,
			HorizontalOptions = LayoutOptions.Center,
			Padding = 32,
			Stroke = Colors.DarkGray,
			VerticalOptions = LayoutOptions.Center,
			WidthRequest = 320
		};

		var childPage = new ContentPage
		{
			BackgroundColor = Colors.LightGray,
			Content = new Grid
			{
				Children = { _savePopup }
			},
			Title = "Image viewer"
		};

		childPage.Loaded += OnChildPageLoaded;
		_childWindow = new Window(childPage)
		{
			Title = "Image viewer"
		};
		_childWindow.Destroying += OnChildWindowDestroying;

		Application.Current.OpenWindow(_childWindow);
	}

	void OnChildPageLoaded(object sender, EventArgs e)
	{
		if (_closeQueued)
			return;

		_closeQueued = true;
		Dispatcher.Dispatch(() => Application.Current.CloseWindow(_childWindow));
	}

	void OnChildWindowDestroying(object sender, EventArgs e)
	{
		_destroyingCount++;
		Dispatcher.Dispatch(RunPendingPopupAnimation);
	}

	void RunPendingPopupAnimation()
	{
		_animationAttempts++;
		var exceptionResult = "None";

		try
		{
			IAnimatable animatable = _savePopup;
			AnimationExtensions.Animate(
				animatable,
				"HidePopup",
				value => 1 - value,
				value => _savePopup.Opacity = value,
				length: 250);
		}
		catch (ObjectDisposedException exception)
		{
			exceptionResult = $"ObjectDisposedException;ObjectName={exception.ObjectName}";
		}

		var result = $"Destroying={_destroyingCount};Attempts={_animationAttempts};Exception={exceptionResult}";
		_lifecycleResultLabel.Text = result;
		_primaryLayout.Children.Add(new Label
		{
			AutomationId = "Issue34057Completion",
			Text = result
		});
	}
}
#endif

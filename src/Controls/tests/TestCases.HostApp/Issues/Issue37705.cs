#if ANDROID
using Android.Content.Res;
using AndroidX.Core.View;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37705, "Status bar icons are unreadable when Material 3 is enabled", PlatformAffected.Android)]
public class Issue37705 : ContentPage
{
	const string PendingObservation = "Token=Pending; Action=Waiting";
	readonly Label _observationLabel;
	string _capturedWindowState = "Token=Pending";
	string _actionState = "Waiting";

	public Issue37705()
	{
		Title = "Status Bar Contrast";

		var application = Application.Current;
		if (application is not null)
			application.UserAppTheme = AppTheme.Light;

		_observationLabel = new Label
		{
			Text = PendingObservation,
			AutomationId = "Issue37705Observation",
			FontAttributes = FontAttributes.Bold
		};

		var checkButton = new Button
		{
			Text = "Check status bar contrast",
			AutomationId = "Issue37705CheckButton"
		};
		checkButton.Clicked += OnCheckStatusBarContrastClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Status bar readability check",
					FontSize = 24
				},
				new Label
				{
					Text = "The Android status bar above must display icons and text that contrast with its background."
				},
				_observationLabel,
				checkButton
			}
		};

		Loaded += OnPageLoaded;
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
		var platformWindow = activity?.Window;
		var decorView = platformWindow?.DecorView;

		if (activity is null || platformWindow is null || decorView is null)
		{
			_capturedWindowState = "Token=1; SetupError=NativeWindowUnavailable";
			UpdateObservation();
			return;
		}

		decorView.Post(() =>
		{
			var configuration = activity.Resources?.Configuration;
			var rootInsets = ViewCompat.GetRootWindowInsets(decorView);
			var controller = WindowCompat.GetInsetsController(platformWindow, decorView);

			if (configuration is null || rootInsets is null || controller is null)
			{
				_capturedWindowState = "Token=1; SetupError=NativeStateUnavailable";
				UpdateObservation();
				return;
			}

			var statusBarInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.StatusBars());
			var imeVisible = rootInsets.IsVisible(WindowInsetsCompat.Type.Ime());
			var isLightMode = (configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;
			var isPortrait = configuration.Orientation == Orientation.Portrait;
			_capturedWindowState =
				$"Token=1; UiMode={(isLightMode ? "Light" : "Dark")}; Orientation={(isPortrait ? "Portrait" : configuration.Orientation)}; " +
				$"StatusBarInset={statusBarInsets.Top}; ImeVisible={imeVisible}; Attached={decorView.IsAttachedToWindow}; " +
				$"Decor={decorView.Width}x{decorView.Height}; LightStatusBars={controller.AppearanceLightStatusBars}";
			UpdateObservation();
		});
	}

	void OnCheckStatusBarContrastClicked(object sender, EventArgs e)
	{
		_actionState = "Tapped";
		UpdateObservation();
	}

	void UpdateObservation()
	{
		_observationLabel.Text = $"{_capturedWindowState}; Action={_actionState}";
	}
}
#endif


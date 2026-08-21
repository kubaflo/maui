#if ANDROID
using Android.Content.Res;
using AndroidX.Core.View;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37705, "[Android] Status bar icons are unreadable when Material 3 is enabled", PlatformAffected.Android)]
public class Issue37705 : ContentPage
{
	readonly Label _observationLabel;
#if ANDROID
	int _callbackCount;
#endif

	public Issue37705()
	{
		_observationLabel = new Label
		{
			Text = "UNOBSERVED:-1",
			AutomationId = "StatusBarObservation",
			FontAttributes = FontAttributes.Bold
		};

		Content = new Grid
		{
			Padding = 24,
			RowSpacing = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				new Label
				{
					Text = "Material 3 status bar contrast",
					FontAttributes = FontAttributes.Bold,
					FontSize = 24
				},
				new Label
				{
					Text = "The Android status bar icons and text should contrast with its background."
				}.Row(1),
				new Button
				{
					Text = "Status bar icons should remain readable"
				}.Row(2),
				_observationLabel.Row(3)
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

#if ANDROID
		var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
		var platformWindow = activity?.Window;
		var decorView = platformWindow?.DecorView;
		if (activity is null || platformWindow is null || decorView is null)
		{
			_observationLabel.Text = "ERROR:Android window or decor view was unavailable.";
			return;
		}

		decorView.Post(() =>
		{
			_callbackCount++;

			var controller = WindowCompat.GetInsetsController(platformWindow, decorView);
			if (controller is null)
			{
				_observationLabel.Text = "ERROR:WindowInsetsController was unavailable.";
				return;
			}

			var nightMode = activity.Resources?.Configuration?.UiMode & UiMode.NightMask;
			string mode = nightMode switch
			{
				UiMode.NightNo => "Light",
				UiMode.NightYes => "Dark",
				_ => "Unknown"
			};
			bool expectedLightStatusBars = nightMode == UiMode.NightNo;

			_observationLabel.Text =
				$"MODE:{mode};OBSERVED:{controller.AppearanceLightStatusBars};EXPECTED:{expectedLightStatusBars};" +
				$"ATTACHED:{decorView.IsAttachedToWindow};CALLBACKS:{_callbackCount};" +
				$"WIDTH:{decorView.Width};HEIGHT:{decorView.Height}";
		});
#endif
	}
}


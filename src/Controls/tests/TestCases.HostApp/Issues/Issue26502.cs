namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26502, "WindowManagerFlags.Secure does not block screenshots on modal pages", PlatformAffected.Android)]
public class Issue26502 : ContentPage
{
#if ANDROID
	bool _secureFlagSet;
#endif

	public Issue26502()
	{
		var openModalButton = new Button
		{
			Text = "Open secure modal",
			AutomationId = "Issue26502OpenModal"
		};
		openModalButton.Clicked += OnOpenModalClicked;

		Content = new VerticalStackLayout
		{
			AutomationId = "Issue26502RootSurface",
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					Text = "Secure activity page",
					AutomationId = "Issue26502RootTitle",
					FontSize = 24
				},
				new Label
				{
					Text = "The activity window has FLAG_SECURE.",
					AutomationId = "Issue26502RootDescription"
				},
				openModalButton
			}
		};
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		var modalPage = CreateModalPage(out var statusLabel);
		await Navigation.PushModalAsync(modalPage, false);

#if ANDROID
		var secureFlagPresent =
			modalPage.Handler?.PlatformView is Android.Views.View modalView &&
			modalView.RootView.LayoutParameters is Android.Views.WindowManagerLayoutParams attributes &&
			attributes.Flags.HasFlag(Android.Views.WindowManagerFlags.Secure);
		statusLabel.Text = $"Modal secure flag evaluated: {secureFlagPresent}";
#endif
	}

#if ANDROID
	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (!_secureFlagSet)
		{
			var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window
				?? throw new InvalidOperationException("The Android activity window is unavailable.");
			window.SetFlags(
				Android.Views.WindowManagerFlags.Secure,
				Android.Views.WindowManagerFlags.Secure);
			_secureFlagSet = true;
		}
	}
#endif

	static ContentPage CreateModalPage(out Label statusLabel)
	{
		statusLabel = new Label
		{
			Text = "Modal secure flag: NOT_EVALUATED",
			AutomationId = "Issue26502ModalDescription"
		};

		return new ContentPage
		{
			Content = new VerticalStackLayout
			{
				AutomationId = "Issue26502ModalSurface",
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						Text = "Secure modal page",
						AutomationId = "Issue26502ModalTitle",
						FontSize = 24
					},
					statusLabel,
					new Button
					{
						Text = "Close secure modal",
						AutomationId = "Issue26502ModalButton"
					}
				}
			}
		};
	}
}


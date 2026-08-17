namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37280, "Crash at Window Close", PlatformAffected.UWP)]
public class Issue37280 : ContentPage
{
	bool deviceEjected;

	public Issue37280()
	{
		var importStatusLabel = new Label
		{
			Text = "Import is idle.",
			AutomationId = "ImportStatus"
		};

		var startImportButton = new Button
		{
			Text = "Start USB import",
			AutomationId = "StartImportButton"
		};

		var ejectDeviceButton = new Button
		{
			Text = "Eject device during import",
			AutomationId = "EjectDeviceButton",
			IsEnabled = false
		};

		var retryImportButton = new Button
		{
			Text = "Retry import",
			AutomationId = "RetryImportButton",
			IsEnabled = false
		};

		var logoutButton = new Button
		{
			Text = "Logout",
			AutomationId = "LogoutButton",
			IsEnabled = false
		};

		startImportButton.Clicked += (sender, args) =>
		{
			importStatusLabel.Text = "Import is active.";
			startImportButton.IsEnabled = false;
			ejectDeviceButton.IsEnabled = true;
		};

		ejectDeviceButton.Clicked += (sender, args) =>
		{
			deviceEjected = true;
			importStatusLabel.Text = "Import failure was caught after device ejection.";
			ejectDeviceButton.IsEnabled = false;
			retryImportButton.IsEnabled = true;
		};

		retryImportButton.Clicked += (sender, args) =>
		{
			if (!deviceEjected)
				return;

			importStatusLabel.Text = "Retry failed because the device remains ejected.";
			retryImportButton.IsEnabled = false;
			logoutButton.IsEnabled = true;
		};

		logoutButton.Clicked += (sender, args) =>
		{
			var app = Application.Current;
			app.CloseWindow(Window);
			app.OpenWindow(CreateLoginWindow());
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					new Label
					{
						Text = "USB import and logout",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					importStatusLabel,
					startImportButton,
					ejectDeviceButton,
					retryImportButton,
					logoutButton,
					new Label
					{
						Text = "NO BUG: The application is ready for the close-window trigger.",
						AutomationId = "ResultStatus",
						FontAttributes = FontAttributes.Bold
					}
				}
			}
		};
	}

	static Window CreateLoginWindow()
	{
		var loginStatusLabel = new Label
		{
			Text = "Login window not loaded.",
			AutomationId = "LoginWindowStatus",
			FontSize = 24
		};

		var loginPage = new ContentPage
		{
			Title = "Login",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Children =
				{
					loginStatusLabel
				}
			}
		};

		loginPage.Loaded += (sender, args) => loginStatusLabel.Text = "Login window loaded.";

		return new Window(loginPage);
	}
}

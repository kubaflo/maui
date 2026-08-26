namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 25202, "Custom Shell TitleView geometry changes after registered-route navigation", PlatformAffected.Android)]
public partial class Issue25202Shell : Shell
{
	public Issue25202Shell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Issue25202LoginPage), typeof(Issue25202LoginPage));
		Navigated += OnNavigated;
	}

	void OnNavigated(object sender, ShellNavigatedEventArgs e)
	{
		if (CurrentPage is Page currentPage)
			ToolbarTitleLabel.Text = currentPage.Title;
	}
}

public sealed class Issue25202SettingsPage : ContentPage
{
	public string[] Languages { get; } = ["English"];

	public Issue25202SettingsPage()
	{
		Title = "Settings";
		BindingContext = this;

		var settingsLabel = new Label
		{
			AutomationId = "Issue25202SettingsTitle",
			Text = "Settings",
			FontSize = 24,
			FontAttributes = FontAttributes.Bold
		};

		var languagePicker = new Picker
		{
			AutomationId = "Issue25202LanguagePicker",
			Title = "Language",
			ItemsSource = Languages,
			SelectedIndex = 0
		};

		var navigationButton = new Button
		{
			AutomationId = "Issue25202NavigateButton",
			Text = "Navigate to login"
		};
		navigationButton.Clicked += OnNavigateToLoginClicked;

		Content = new VerticalStackLayout
		{
			AutomationId = "Issue25202SettingsContent",
			Padding = 24,
			Spacing = 24,
			Children =
			{
				settingsLabel,
				languagePicker,
				navigationButton
			}
		};
	}

	async void OnNavigateToLoginClicked(object sender, EventArgs e)
	{
		var shell = Shell.Current;
		if (shell is null)
			throw new InvalidOperationException("The Settings page must be hosted in a Shell.");

		await shell.GoToAsync(nameof(Issue25202LoginPage));
	}
}

public sealed class Issue25202LoginPage : ContentPage
{
	public Issue25202LoginPage()
	{
		Title = "Log in";
		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 20,
			Children =
			{
				new Label
				{
					AutomationId = "Issue25202LoginContent",
					Text = "Login route",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				},
				new Entry
				{
					Placeholder = "Username"
				}
			}
		};
	}
}

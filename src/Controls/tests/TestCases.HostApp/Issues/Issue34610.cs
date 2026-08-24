namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34610, "Shell TitleView on iOS has unremovable horizontal margins and vertical gap", PlatformAffected.iOS)]
public class Issue34610 : ContentPage
{
	public Issue34610()
	{
		var bootstrapResult = new Label
		{
			Text = "Ready",
			AutomationId = "BootstrapResult",
			FontSize = 18,
			HorizontalTextAlignment = TextAlignment.Center
		};

		var scenarioDescription = new Label
		{
			Text = "Shell TitleView margin reproduction",
			FontSize = 24,
			HorizontalTextAlignment = TextAlignment.Center,
			AutomationId = "ScenarioDescription"
		};

		var openScenarioButton = new Button
		{
			Text = "Open Shell scenario",
			VerticalOptions = LayoutOptions.Start,
			AutomationId = "OpenShellScenario",
			Command = new Command(OpenShellScenario)
		};

		Content = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16,
			Children =
			{
				scenarioDescription,
				bootstrapResult,
				openScenarioButton
			}
		};

		Grid.SetRow(scenarioDescription, 1);
		Grid.SetRow(bootstrapResult, 2);
		Grid.SetRow(openScenarioButton, 3);
	}

	void OpenShellScenario()
	{
		var titleView = new Grid
		{
			AutomationId = "AffectedTitleView",
			BackgroundColor = Colors.Red,
			Padding = 0,
			Margin = 0,
			ColumnSpacing = 0,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};

		var menuLabel = new Label
		{
			AutomationId = "MenuLabel",
			Text = "☰",
			FontSize = 24,
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(10, 0)
		};

		var titleLabel = new Label
		{
			AutomationId = "TitleLabel",
			Text = "MY APP TITLE",
			TextColor = Colors.White,
			FontSize = 16,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		Grid.SetColumn(titleLabel, 1);

		var settingsLabel = new Label
		{
			AutomationId = "SettingsLabel",
			Text = "⚙",
			FontSize = 24,
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(10, 0)
		};
		Grid.SetColumn(settingsLabel, 2);

		titleView.Children.Add(menuLabel);
		titleView.Children.Add(titleLabel);
		titleView.Children.Add(settingsLabel);

		var contentBox = new BoxView
		{
			AutomationId = "AffectedPageContent",
			Color = Colors.DodgerBlue
		};

		var layoutGenerationLabel = new Label
		{
			AutomationId = "LayoutGeneration",
			Text = "Layout generation: -1",
			FontSize = 18,
			TextColor = Colors.Black,
			BackgroundColor = Colors.White,
			Padding = 8,
			Margin = 12,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.End
		};

		var pageContent = new Grid
		{
			Children =
			{
				contentBox,
				layoutGenerationLabel
			}
		};

		var page = new ContentPage
		{
			Padding = 0,
			Content = pageContent
		};
		Shell.SetNavBarHasShadow(page, false);
		Shell.SetTitleView(page, titleView);

		int layoutGeneration = -1;
		titleView.SizeChanged += (_, _) =>
		{
			layoutGeneration++;
			layoutGenerationLabel.Text = $"Layout generation: {layoutGeneration}";
		};

		var shell = new Shell
		{
			FlyoutBehavior = FlyoutBehavior.Disabled,
			Items =
			{
				new ShellContent
				{
					Content = page
				}
			}
		};

		var currentWindow = Window ?? throw new InvalidOperationException("The issue page must be attached to a window.");
		currentWindow.Page = shell;
	}
}


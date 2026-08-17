namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37138, "Shell gradient color not working", PlatformAffected.iOS)]
public class Issue37138 : ContentPage
{
	public Issue37138()
	{
		var statusLabel = new Label
		{
			AutomationId = "Issue37138Result",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Text = "NO BUG:"
		};

		var showShellButton = new Button
		{
			AutomationId = "Issue37138ShowGradientShell",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Show gradient Shell"
		};

		showShellButton.Clicked += (_, _) =>
		{
			showShellButton.IsEnabled = false;
			((Grid)Content).Children.Remove(statusLabel);
			Window.Page = CreateGradientShell(statusLabel);
		};

		var launcherLayout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16
		};

		launcherLayout.Add(statusLabel, 0, 1);
		launcherLayout.Add(showShellButton, 0, 2);
		Content = launcherLayout;
	}

	static Shell CreateGradientShell(Label statusLabel)
	{
		statusLabel.FontAttributes = FontAttributes.Bold;
		statusLabel.FontSize = 18;
		statusLabel.HorizontalTextAlignment = TextAlignment.Center;

		var expectedGradient = new Grid
		{
			AutomationId = "Issue37138ExpectedGradient",
			Background = CreateGradient(),
			HeightRequest = 120,
			Children =
			{
				new Label
				{
					AutomationId = "Issue37138ExpectedGradientLabel",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Text = "Expected toolbar and tab bar gradient",
					TextColor = Colors.White
				}
			}
		};

		var homePage = new ContentPage
		{
			Title = "Gradient Shell",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 24,
				Children =
				{
					new Label
					{
						FontSize = 20,
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Shell.Background should match this gradient"
					},
					expectedGradient,
					statusLabel
				}
			}
		};

		var tabBar = new TabBar
		{
			Items =
			{
				new Tab
				{
					Title = "Home",
					Items =
					{
						new ShellContent
						{
							Title = "Home",
							Content = homePage
						}
					}
				},
				new Tab
				{
					Title = "Second",
					Items =
					{
						new ShellContent
						{
							Title = "Second",
							Content = new ContentPage
							{
								Title = "Second",
								Content = new Label
								{
									HorizontalOptions = LayoutOptions.Center,
									VerticalOptions = LayoutOptions.Center,
									Text = "Second tab"
								}
							}
						}
					}
				}
			}
		};

		var shell = new Shell
		{
			Background = CreateGradient(),
			FlyoutBehavior = FlyoutBehavior.Disabled,
			Items = { tabBar }
		};

		shell.Loaded += (_, _) => statusLabel.Text = "SHELL LOADED";
		return shell;
	}

	static LinearGradientBrush CreateGradient() =>
		new()
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 0),
			GradientStops =
			{
				new GradientStop(Colors.DeepPink, 0),
				new GradientStop(Colors.DeepSkyBlue, 1)
			}
		};
}

#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37149, "Shell Background does not apply to the TabBar on Windows", PlatformAffected.WinRT)]
public class Issue37149 : ContentPage
{
	readonly VerticalStackLayout _setupLayout;
	readonly Label _resultLabel;

	public Issue37149()
	{
		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "NO BUG:",
			FontAttributes = FontAttributes.Bold
		};

		var openShellButton = new Button
		{
			AutomationId = "OpenShellButton",
			Text = "Open Shell scenario"
		};
		openShellButton.Clicked += OnOpenShellClicked;

		_setupLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Issue 37149: Shell background and TabBar background",
					FontSize = 24
				},
				new Label
				{
					Text = "Open a default-styled Shell whose Background is an orange-red to purple gradient. The navigation area and tab bar should use the same background."
				},
				_resultLabel,
				openShellButton
			}
		};

		Content = _setupLayout;
	}

	void OnOpenShellClicked(object sender, EventArgs e)
	{
		_setupLayout.Remove(_resultLabel);

		var homePage = new ContentPage
		{
			Title = "Home",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						AutomationId = "GradientDescription",
						Text = "Gradient stops: OrangeRed 0; Purple 1"
					},
					_resultLabel
				}
			}
		};
		homePage.Loaded += OnHomePageLoaded;

		var tabBar = new TabBar();
		tabBar.Items.Add(new Tab
		{
			AutomationId = "HomeTab",
			Title = "Home",
			Items =
			{
				new ShellContent
				{
					Title = "Home",
					Content = homePage
				}
			}
		});
		tabBar.Items.Add(new Tab
		{
			AutomationId = "DetailsTab",
			Title = "Details",
			Items =
			{
				new ShellContent
				{
					Title = "Details",
					Content = new ContentPage
					{
						Title = "Details",
						Content = new Label
						{
							Margin = 24,
							Text = "Second tab"
						}
					}
				}
			}
		});

		var shell = new Shell
		{
			FlyoutBehavior = FlyoutBehavior.Disabled,
			Items = { tabBar },
			Background = new LinearGradientBrush
			{
				GradientStops =
				{
					new GradientStop { Color = Colors.OrangeRed, Offset = 0 },
					new GradientStop { Color = Colors.Purple, Offset = 1 }
				}
			}
		};

		Window.Page = shell;
	}

	void OnHomePageLoaded(object sender, EventArgs e)
	{
		_resultLabel.Text = "SHELL LOADED";
	}
}
#endif

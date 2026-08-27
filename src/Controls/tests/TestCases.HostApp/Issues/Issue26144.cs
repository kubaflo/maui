namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26144, "Shell TabBar content does not render after navigating away and back", PlatformAffected.Android)]
public class Issue26144 : Shell
{
	readonly Label _navigationMarker;
	int _navigationSequence;

	public Issue26144()
	{
		_navigationMarker = new Label
		{
			AutomationId = "Issue26144NavigationMarker",
			Text = "MainPage:0"
		};

		Shell.SetTitleView(this, _navigationMarker);
		Navigated += OnNavigated;

		Items.Add(new ShellContent
		{
			Title = "Main Page",
			Route = "MainPage",
			ContentTemplate = new DataTemplate(() => new Issue26144MainPage(OpenDashboard))
		});

		var dashboardTab = new Tab
		{
			Route = "DashboardPage"
		};
		dashboardTab.Items.Add(new ShellContent
		{
			Title = "Dashboard",
			ContentTemplate = new DataTemplate(() => new Issue26144DashboardPage(OpenMainPage))
		});

		var tabBar = new TabBar();
		tabBar.Items.Add(dashboardTab);
		Items.Add(tabBar);
	}

	void OpenDashboard()
	{
		_ = GoToAsync("//DashboardPage");
	}

	void OpenMainPage()
	{
		_ = GoToAsync("//MainPage");
	}

	void OnNavigated(object sender, ShellNavigatedEventArgs e)
	{
		var location = e.Current.Location.OriginalString;
		var route = location.Contains("DashboardPage", StringComparison.Ordinal)
			? "DashboardPage"
			: "MainPage";

		_navigationMarker.Text = $"{route}:{++_navigationSequence}";
	}
}

file class Issue26144MainPage : ContentPage
{
	public Issue26144MainPage(Action openDashboard)
	{
		Title = "Main Page";
		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "Issue26144MainContent",
					FontSize = 24,
					Text = "Main page content"
				},
				new Button
				{
					AutomationId = "Issue26144OpenDashboard",
					Text = "Open dashboard",
					Command = new Command(openDashboard)
				}
			}
		};
	}
}

file class Issue26144DashboardPage : Shell
{
	public Issue26144DashboardPage(Action openMainPage)
	{
		var tabBar = new TabBar();
		tabBar.Items.Add(CreateHomeTab(openMainPage));
		tabBar.Items.Add(CreateHomeTab(openMainPage));
		Items.Add(tabBar);
	}

	static Tab CreateHomeTab(Action openMainPage)
	{
		var tab = new Tab
		{
			Title = "Home",
			Icon = "home"
		};
		tab.Items.Add(new ShellContent
		{
			Route = "HomePage",
			ContentTemplate = new DataTemplate(() => new Issue26144HomePage(openMainPage))
		});
		return tab;
	}
}

file class Issue26144HomePage : ContentPage
{
	public Issue26144HomePage(Action openMainPage)
	{
		Title = "Home";
		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "Issue26144HomeContent",
					FontSize = 24,
					Text = "Dashboard home content"
				},
				new Button
				{
					AutomationId = "Issue26144BackToMain",
					Text = "Back to main page",
					Command = new Command(openMainPage)
				}
			}
		};
	}
}


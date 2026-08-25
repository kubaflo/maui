namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26144, "Shell TabBar content does not render after navigating away and back", PlatformAffected.Android)]
public class Issue26144 : Shell
{
	const string DashboardRoute = "DashboardPage";
	const string MainRoute = "MainPage";

	readonly Label _dashboardVisitLabel;
	int _dashboardVisitCount;

	public Issue26144()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		_dashboardVisitLabel = new Label
		{
			AutomationId = "Issue26144DashboardVisitCount",
			Text = "Dashboard visits: 0",
			VerticalTextAlignment = TextAlignment.Center
		};
		Shell.SetTitleView(this, _dashboardVisitLabel);

		Items.Add(new ShellContent
		{
			Title = "Main Page",
			Route = MainRoute,
			ContentTemplate = new DataTemplate(CreateMainPage)
		});

		var dashboardTabBar = new TabBar();
		var dashboardTab = new Tab
		{
			Route = DashboardRoute
		};
		dashboardTab.Items.Add(new ShellContent
		{
			Title = "Dashboard",
			ContentTemplate = new DataTemplate(() => new DashboardPage())
		});
		dashboardTabBar.Items.Add(dashboardTab);
		Items.Add(dashboardTabBar);
	}

	ContentPage CreateMainPage()
	{
		var openDashboardButton = new Button
		{
			AutomationId = "Issue26144OpenDashboard",
			Text = "Open dashboard"
		};
		openDashboardButton.Clicked += async (_, _) =>
		{
			_dashboardVisitCount++;
			_dashboardVisitLabel.Text = $"Dashboard visits: {_dashboardVisitCount}";
			await Shell.Current.GoToAsync($"//{DashboardRoute}");
		};

		return new ContentPage
		{
			Title = "Main Page",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						AutomationId = "Issue26144MainContent",
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center,
						Text = "Main page"
					},
					openDashboardButton
				}
			}
		};
	}

	sealed class DashboardPage : Shell
	{
		public DashboardPage()
		{
			var tabBar = new TabBar();
			tabBar.Items.Add(CreateHomeTab("HomePage"));
			tabBar.Items.Add(CreateHomeTab("HomePage2"));
			Items.Add(tabBar);
		}

		static Tab CreateHomeTab(string route)
		{
			var tab = new Tab
			{
				Icon = "home",
				Title = "Home"
			};
			tab.Items.Add(new ShellContent
			{
				Route = route,
				ContentTemplate = new DataTemplate(() => new HomePage())
			});
			return tab;
		}
	}

	sealed class HomePage : ContentPage
	{
		public HomePage()
		{
			Title = "Home";

			var backButton = new Button
			{
				AutomationId = "Issue26144BackToMain",
				Text = "Back to main page"
			};
			backButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"//{MainRoute}");

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						AutomationId = "Issue26144DashboardContent",
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center,
						Text = "Dashboard home content"
					},
					backButton
				}
			};
		}
	}
}


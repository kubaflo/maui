namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26144, "Nested Shell content disappears after navigating away and back", PlatformAffected.Android)]
public class Issue26144 : Shell
{
	Issue26144DashboardPage _dashboardPage = null!;
	readonly Label _dashboardRouteStatusLabel;
	readonly Label _instanceTokenLabel;
	readonly Label _mainRouteStatusLabel;
	int _dashboardNavigationCount;
	int _mainNavigationCount;

	public Issue26144()
	{
		_mainNavigationCount = -1;
		_mainRouteStatusLabel = new Label
		{
			Text = "Main:-1",
			AutomationId = "Issue26144MainRouteStatus"
		};
		_dashboardRouteStatusLabel = new Label
		{
			Text = "Dashboard:-1",
			AutomationId = "Issue26144DashboardRouteStatus"
		};
		_instanceTokenLabel = new Label
		{
			Text = "not-created",
			AutomationId = "Issue26144DashboardInstanceToken"
		};
		Shell.SetTitleView(this, new HorizontalStackLayout
		{
			Children =
			{
				_mainRouteStatusLabel,
				_dashboardRouteStatusLabel,
				_instanceTokenLabel
			}
		});

		Items.Add(new ShellContent
		{
			Title = "Main Page",
			Route = "MainPage",
			ContentTemplate = new DataTemplate(() => new Issue26144MainPage(this))
		});

		var dashboardTab = new Tab
		{
			Route = "DashboardPage"
		};
		dashboardTab.Items.Add(new ShellContent
		{
			Title = "Dashboard",
			ContentTemplate = new DataTemplate(() =>
			{
				_dashboardPage = new Issue26144DashboardPage(this);
				_instanceTokenLabel.Text = _dashboardPage.InstanceToken;
				return _dashboardPage;
			})
		});

		var tabBar = new TabBar();
		tabBar.Items.Add(dashboardTab);
		Items.Add(tabBar);
	}

	internal void NotifyMainPageAppeared()
	{
		if (_mainNavigationCount == -1)
		{
			_mainNavigationCount = 0;
			_mainRouteStatusLabel.Text = "Main:0";
		}
	}

	internal async Task NavigateToDashboardAsync()
	{
		int navigationCount = ++_dashboardNavigationCount;
		await GoToAsync("//DashboardPage");

		if (_dashboardPage is null)
			throw new InvalidOperationException("The DashboardPage route did not create its content.");

		_dashboardRouteStatusLabel.Text = $"Dashboard:{navigationCount}";
	}

	internal async Task NavigateToMainPageAsync()
	{
		int navigationCount = ++_mainNavigationCount;
		await GoToAsync("//MainPage");
		_mainRouteStatusLabel.Text = $"Main:{navigationCount}";
	}
}

public sealed class Issue26144MainPage : ContentPage
{
	readonly Issue26144 _ownerShell;

	public Issue26144MainPage(Issue26144 ownerShell)
	{
		_ownerShell = ownerShell;
		Title = "Main Page";

		var openDashboardButton = new Button
		{
			Text = "Open Dashboard",
			AutomationId = "Issue26144OpenDashboardButton",
			HorizontalOptions = LayoutOptions.Center
		};
		openDashboardButton.Clicked += OnOpenDashboardClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 24,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Main page content",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center
				},
				openDashboardButton
			}
		};

		Appearing += OnAppearing;
	}

	void OnAppearing(object sender, EventArgs e)
	{
		_ownerShell.NotifyMainPageAppeared();
	}

	async void OnOpenDashboardClicked(object sender, EventArgs e)
	{
		await _ownerShell.NavigateToDashboardAsync();
	}
}

public sealed class Issue26144DashboardPage : Shell
{
	internal string InstanceToken { get; } = Guid.NewGuid().ToString("N");

	public Issue26144DashboardPage(Issue26144 ownerShell)
	{
		Title = "Dashboard";

		var primaryHomePage = new Issue26144HomePage(ownerShell, true);
		var secondaryHomePage = new Issue26144HomePage(ownerShell, false);
		var primaryTab = CreateHomeTab(primaryHomePage);
		var secondaryTab = CreateHomeTab(secondaryHomePage);
		var tabBar = new TabBar();
		tabBar.Items.Add(primaryTab);
		tabBar.Items.Add(secondaryTab);
		Items.Add(tabBar);
	}

	static Tab CreateHomeTab(Issue26144HomePage homePage)
	{
		var tab = new Tab
		{
			Title = "Home",
			Icon = "home"
		};
		tab.Items.Add(new ShellContent
		{
			Route = "HomePage",
			ContentTemplate = new DataTemplate(() => homePage)
		});
		return tab;
	}
}

public sealed class Issue26144HomePage : ContentPage
{
	readonly Issue26144 _ownerShell;

	public Issue26144HomePage(Issue26144 ownerShell, bool isPrimary)
	{
		_ownerShell = ownerShell;
		Title = "Home";

		var contentLabel = new Label
		{
			Text = "Dashboard content visible",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center
		};

		var returnButton = new Button
		{
			Text = "Return to Main Page",
			HorizontalOptions = LayoutOptions.Center
		};
		returnButton.Clicked += OnReturnToMainPageClicked;

		if (isPrimary)
		{
			contentLabel.AutomationId = "Issue26144DashboardContent";
			returnButton.AutomationId = "Issue26144ReturnToMainButton";
		}

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 24,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				contentLabel,
				returnButton
			}
		};
	}

	async void OnReturnToMainPageClicked(object sender, EventArgs e)
	{
		await _ownerShell.NavigateToMainPageAsync();
	}
}


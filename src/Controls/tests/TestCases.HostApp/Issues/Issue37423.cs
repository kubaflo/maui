#if IOS
using System.Globalization;
using Microsoft.Maui;
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37423, "Shell TabBarBackgroundColor renders an opaque background behind the Liquid Glass tab bar", PlatformAffected.iOS)]
public class Issue37423 : ContentPage
{
	readonly Label _transitionLabel;
	readonly Label _actualAlphaLabel;
	readonly Label _nativeIdentityLabel;
	bool _shellShown;

	public Issue37423()
	{
		Application.Current.UserAppTheme = AppTheme.Light;

		_transitionLabel = CreateLabel("Issue37423Transition", "-1");
		_actualAlphaLabel = CreateLabel("Issue37423ActualAlpha", "-1");
		_nativeIdentityLabel = CreateLabel("Issue37423NativeIdentity", "pending");

		var showShellButton = new Button
		{
			AutomationId = "Issue37423ShowShell",
			Text = "Show styled Shell"
		};
		showShellButton.Clicked += OnShowShellClicked;

		var startupLayout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16
		};

		AddToGrid(startupLayout, new Label
		{
			FontSize = 22,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Issue 37423: Shell tab bar background"
		}, 1);
		AddToGrid(startupLayout, showShellButton, 2);
		AddToGrid(startupLayout, CreateLabel("Issue37423Theme", Application.Current.UserAppTheme.ToString()), 3);
		AddToGrid(startupLayout, CreateLabel("Issue37423OS", GetOperatingSystemStatus()), 4);
		AddToGrid(startupLayout, CreateLabel("Issue37423DefaultAlpha", GetDefaultTabBarAlpha()), 5);
		AddToGrid(startupLayout, _transitionLabel, 6);

		Content = startupLayout;
	}

	void OnShowShellClicked(object sender, EventArgs e)
	{
		if (_shellShown)
			return;

		_shellShown = true;
		((Grid)Content).Children.Remove(_transitionLabel);

		var homeLayout = new Grid
		{
			BackgroundColor = Color.FromArgb("#DCEBFA"),
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		AddToGrid(homeLayout, new Label
		{
			FontSize = 24,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Page content behind the floating tab bar"
		}, 1);
		AddToGrid(homeLayout, CreateLabel("Issue37423ShellReady", "Styled Shell is ready"), 2);
		AddToGrid(homeLayout, _transitionLabel, 3);
		AddToGrid(homeLayout, _actualAlphaLabel, 3);
		AddToGrid(homeLayout, _nativeIdentityLabel, 3);

		var shell = new Shell();
		Shell.SetTabBarBackgroundColor(shell, Color.FromArgb("#FEFCF9"));
		Shell.SetTabBarTitleColor(shell, Color.FromArgb("#28599A"));
		Shell.SetTabBarUnselectedColor(shell, Color.FromArgb("#6F6F6F"));

		var tabBar = new TabBar();
		tabBar.Items.Add(CreateTab("Home", new ContentPage { Content = homeLayout }));
		tabBar.Items.Add(CreateTab("Search", CreatePage("Search")));
		tabBar.Items.Add(CreateTab("Settings", CreatePage("Settings")));
		shell.Items.Add(tabBar);

		shell.Loaded += OnShellLoaded;
		Window.Page = shell;

		void OnShellLoaded(object loadedSender, EventArgs loadedArgs)
		{
			shell.Loaded -= OnShellLoaded;
			shell.Dispatcher.Dispatch(() => ProbeNativeTabBar(shell, 20));
		}
	}

	void ProbeNativeTabBar(Shell shell, int attemptsRemaining)
	{
#if IOS
		if (shell.Window?.Handler?.PlatformView is UIWindow window &&
			shell.CurrentPage?.Handler is IPlatformViewHandler pageHandler)
		{
			var nativeView = pageHandler.PlatformView;
			while (nativeView is not null && nativeView.NextResponder is not UITabBarController)
				nativeView = nativeView.Superview;

			if (nativeView?.NextResponder is UITabBarController tabBarController)
			{
				var tabBar = tabBarController.TabBar;
				var attached = tabBar.Window == window && tabBarController.View.Window == window;
				var portrait = window.Bounds.Height >= window.Bounds.Width;
				var selectedTitle = tabBar.SelectedItem?.Title ?? string.Empty;

				_actualAlphaLabel.Text = GetAlpha(tabBar.BackgroundColor).ToString("0.###", CultureInfo.InvariantCulture);
				_nativeIdentityLabel.Text = $"items={tabBar.Items?.Length ?? 0};selected={selectedTitle};attached={attached};portrait={portrait}";
				_transitionLabel.Text = "1";
				return;
			}
		}
#endif

		if (attemptsRemaining > 0)
		{
			shell.Dispatcher.Dispatch(() => ProbeNativeTabBar(shell, attemptsRemaining - 1));
			return;
		}

		_nativeIdentityLabel.Text = "native tab bar unavailable";
	}

	static Label CreateLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = text
		};

	static void AddToGrid(Grid grid, View view, int row)
	{
		Grid.SetRow(view, row);
		grid.Children.Add(view);
	}

	static Tab CreateTab(string title, Page page)
	{
		var tab = new Tab { Title = title };
		tab.Items.Add(new ShellContent
		{
			Title = title,
			Content = page
		});
		return tab;
	}

	static ContentPage CreatePage(string title) =>
		new()
		{
			Title = title,
			Content = new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				Text = $"{title} content",
				VerticalOptions = LayoutOptions.Center
			}
		};

	static string GetDefaultTabBarAlpha()
	{
#if IOS
		using var tabBar = new UITabBar();
		return GetAlpha(tabBar.BackgroundColor).ToString("0.###", CultureInfo.InvariantCulture);
#else
		return "unsupported";
#endif
	}

	static string GetOperatingSystemStatus()
	{
#if IOS
		return OperatingSystem.IsIOSVersionAtLeast(26) ? "iOS 26+" : "unsupported";
#else
		return "unsupported";
#endif
	}

#if IOS
	static double GetAlpha(UIColor color) =>
		color is null ? 0 : (double)color.CGColor.Alpha;

#endif
}

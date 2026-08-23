#if WINDOWS
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WRoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using WLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37149, "Shell Background is not applied to the Windows TabBar", PlatformAffected.WinRT)]
public class Issue37149 : Shell
{
	const string PageLoaded = "Page loaded: complete";
	const string TemplateReady = "Tab template: ready";
	const string ManagedBackground = "Managed background: LinearGradientBrush";
	const string TabIdentity = "Tab identity: First tab|Second tab (2)";

	readonly Label _pageLoadedStatusLabel;
	readonly Label _templateReadyStatusLabel;
	readonly Label _managedBackgroundStatusLabel;
	readonly Label _tabIdentityStatusLabel;
	readonly Label _tabBarBackgroundStatusLabel;
	readonly ContentPage _firstContentPage;

#if WINDOWS
	WFrameworkElement _nativeNavigationView;
#endif

	public Issue37149()
	{
		_pageLoadedStatusLabel = CreateStatusLabel("PageLoadedStatus", "Page loaded: not observed");
		_templateReadyStatusLabel = CreateStatusLabel("TemplateReadyStatus", "Template ready: not observed");
		_managedBackgroundStatusLabel = CreateStatusLabel("ManagedBackgroundStatus", "Managed background: not observed");
		_tabIdentityStatusLabel = CreateStatusLabel("TabIdentityStatus", "Tab identity: not observed");
		_tabBarBackgroundStatusLabel = CreateStatusLabel("TabBarBackgroundStatus", "Tab background: not observed");

		_firstContentPage = new ContentPage
		{
			Title = "Gradient page",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						AutomationId = "ShellGradientDescription",
						Text = "The navigation bar and the First tab / Second tab bar should share the orange-to-purple Shell.Background gradient.",
						FontSize = 18
					},
					_pageLoadedStatusLabel,
					_templateReadyStatusLabel,
					_managedBackgroundStatusLabel,
					_tabIdentityStatusLabel,
					_tabBarBackgroundStatusLabel
				}
			}
		};

		var firstTab = new Tab { Title = "First tab" };
		firstTab.Items.Add(new ShellContent
		{
			Title = "Gradient page",
			Content = _firstContentPage
		});

		var secondTab = new Tab { Title = "Second tab" };
		secondTab.Items.Add(new ShellContent
		{
			Title = "Second page",
			Content = new ContentPage
			{
				Content = new Label
				{
					Text = "Second tab content",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			}
		});

		var tabBar = new TabBar();
		tabBar.Items.Add(firstTab);
		tabBar.Items.Add(secondTab);

		FlyoutBehavior = FlyoutBehavior.Disabled;
		Background = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 0),
			GradientStops =
			{
				new GradientStop(Colors.Orange, 0),
				new GradientStop(Colors.Purple, 1)
			}
		};
		Items.Add(tabBar);

		_firstContentPage.Loaded += OnFirstPageLoaded;
	}

	static Label CreateStatusLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	void OnFirstPageLoaded(object sender, EventArgs e)
	{
		_firstContentPage.Loaded -= OnFirstPageLoaded;
		_pageLoadedStatusLabel.Text = PageLoaded;
		_managedBackgroundStatusLabel.Text = Background is LinearGradientBrush
			? ManagedBackground
			: $"Managed background: {Background?.GetType().FullName ?? "<null>"}";

		_tabIdentityStatusLabel.Text =
			CurrentItem is TabBar currentTabBar &&
			currentTabBar.Items.Count == 2 &&
			currentTabBar.Items[0].Title == "First tab" &&
			currentTabBar.Items[1].Title == "Second tab"
				? TabIdentity
				: "Tab identity: unexpected";

#if WINDOWS
		if (CurrentItem?.Handler?.PlatformView is not WFrameworkElement navigationView)
			return;

		_nativeNavigationView = navigationView;
		_nativeNavigationView.Loaded += OnNativeNavigationViewLoaded;
		_nativeNavigationView.LayoutUpdated += OnNativeNavigationViewLayoutUpdated;
		PublishNativeBackgroundWhenTemplateIsReady();
#endif
	}

#if WINDOWS
	void OnNativeNavigationViewLoaded(object sender, WRoutedEventArgs e) =>
		PublishNativeBackgroundWhenTemplateIsReady();

	void OnNativeNavigationViewLayoutUpdated(object sender, object e) =>
		PublishNativeBackgroundWhenTemplateIsReady();

	void PublishNativeBackgroundWhenTemplateIsReady()
	{
		var topNavArea = FindPanel(_nativeNavigationView, "TopNavArea");
		if (topNavArea is null)
			return;

		_nativeNavigationView.Loaded -= OnNativeNavigationViewLoaded;
		_nativeNavigationView.LayoutUpdated -= OnNativeNavigationViewLayoutUpdated;
		_templateReadyStatusLabel.Text = TemplateReady;
		_tabBarBackgroundStatusLabel.Text = topNavArea.Background is WLinearGradientBrush
			? "Tab background: gradient applied"
			: $"Tab background: gradient missing ({topNavArea.Background?.GetType().FullName ?? "<null>"})";
	}

	static WPanel FindPanel(WDependencyObject parent, string name)
	{
		if (parent is WPanel panel && panel.Name == name)
			return panel;

		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var index = 0; index < childCount; index++)
		{
			var match = FindPanel(WVisualTreeHelper.GetChild(parent, index), name);
			if (match is not null)
				return match;
		}

		return null;
	}
#endif
}


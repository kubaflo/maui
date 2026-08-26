#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26714, "Setting Shell.CurrentItem does not trigger the OnNavigating event", PlatformAffected.iOS)]
public class Issue26714 : Shell
{
	readonly ShellContent _homeContent;
	readonly Label _navigatingCountLabel;
	readonly Label _navigatedCountLabel;
	bool _triggered;
	int _navigatingCount = -1;
	int _navigatedCount = -1;

	public Issue26714()
	{
		_navigatingCountLabel = new Label
		{
			AutomationId = "Issue26714NavigatingCount",
			FontSize = 12,
			Text = "OnNavigating=-1"
		};

		_navigatedCountLabel = new Label
		{
			AutomationId = "Issue26714NavigatedCount",
			FontSize = 12,
			Text = "OnNavigated=-1"
		};

		Shell.SetTitleView(this, new VerticalStackLayout
		{
			Spacing = 0,
			Children =
			{
				_navigatingCountLabel,
				_navigatedCountLabel
			}
		});

		var setCurrentItemButton = new Button
		{
			AutomationId = "Issue26714SetCurrentItemButton",
			Text = "Set CurrentItem"
		};
		setCurrentItemButton.Clicked += OnSetCurrentItemClicked;

		var settingsPage = new ContentPage
		{
			Title = "Settings",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						AutomationId = "Issue26714SettingsPageTitle",
						FontSize = 24,
						Text = "Settings tab selected"
					},
					setCurrentItemButton
				}
			}
		};

		var settingsContent = new ShellContent
		{
			Route = "Settings",
			Title = "Settings",
			Content = settingsPage
		};

		_homeContent = new ShellContent
		{
			Route = "Home",
			Title = "Home",
			ContentTemplate = new DataTemplate(typeof(MainPage))
		};

		var tabBar = new TabBar();
		var settingsTab = new Tab { Title = "Settings" };
		settingsTab.Items.Add(settingsContent);
		tabBar.Items.Add(settingsTab);

		var homeTab = new Tab { Title = "Home" };
		homeTab.Items.Add(_homeContent);
		tabBar.Items.Add(homeTab);

		Items.Add(tabBar);
	}

	void OnSetCurrentItemClicked(object sender, EventArgs e)
	{
		_navigatingCount = 0;
		_navigatedCount = 0;
		_triggered = true;
		UpdateCountLabels();

		CurrentItem = _homeContent;
	}

	protected override void OnNavigating(ShellNavigatingEventArgs args)
	{
		if (_triggered)
		{
			_navigatingCount++;
			UpdateCountLabels();
		}

		base.OnNavigating(args);
	}

	protected override void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);

		if (_triggered)
		{
			_navigatedCount++;
			UpdateCountLabels();
		}
	}

	void UpdateCountLabels()
	{
		_navigatingCountLabel.Text = $"OnNavigating={_navigatingCount}";
		_navigatedCountLabel.Text = $"OnNavigated={_navigatedCount}";
	}

	public class MainPage : ContentPage
	{
		public MainPage()
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
						AutomationId = "Issue26714HomePageTitle",
						FontSize = 24,
						Text = "Home tab selected"
					},
					new Label
					{
						Text = "This page became visible after assigning Shell.CurrentItem."
					}
				}
			};
		}
	}
}
#endif


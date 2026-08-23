#if IOS
using UIKit;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32992, "Shell TabBarBackgroundColor does not reset to null", PlatformAffected.iOS)]
public class Issue32992 : ContentPage
{
	public Issue32992()
	{
		var openButton = new Button
		{
			Text = "Open reproduction",
			AutomationId = "OpenReproductionButton"
		};
		openButton.Clicked += OnOpenReproductionClicked;

		var launcherContent = new VerticalStackLayout
		{
			Spacing = 18,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Shell TabBar color reset",
					FontSize = 24,
					HorizontalTextAlignment = TextAlignment.Center
				},
				openButton
			}
		};
		Grid.SetRow(launcherContent, 1);

		Content = new Grid
		{
			AutomationId = "LauncherRoot",
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				launcherContent
			}
		};
	}

	void OnOpenReproductionClicked(object sender, EventArgs e)
	{
		Window.Page = new Issue32992Shell();
	}
}

class Issue32992Shell : Shell
{
	readonly Label _nativeProbeLabel;
	readonly Label _propertyStateLabel;
	UITabBar _capturedTabBar;
	string _defaultRgba;
	int _probeSequence = -1;

	public Issue32992Shell()
	{
		_nativeProbeLabel = new Label
		{
			Text = "sequence=-1;phase=waiting",
			AutomationId = "NativeTabBarProbeLabel",
			FontSize = 12
		};
		_propertyStateLabel = new Label
		{
			Text = "property=unset;sequence=0",
			AutomationId = "TabBarPropertyStateLabel",
			FontSize = 12
		};

		var applyButton = new Button
		{
			Text = "Apply TabBar Color",
			AutomationId = "ApplyTabBarColorButton"
		};
		applyButton.Clicked += OnApplyClicked;

		var removeButton = new Button
		{
			Text = "Remove TabBar Color",
			AutomationId = "RemoveTabBarColorButton"
		};
		removeButton.Clicked += OnRemoveClicked;

		var testPage = new ContentPage
		{
			Title = "Test",
			Content = new ScrollView
			{
				AutomationId = "ReproductionRoot",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "The tab bar below starts with its platform-default background.",
							FontSize = 18
						},
						_propertyStateLabel,
						_nativeProbeLabel,
						applyButton,
						removeButton
					}
				}
			}
		};
		var secondPage = new ContentPage
		{
			Title = "Second",
			Content = new Label
			{
				Text = "Second tab",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};
		var testTab = new Tab { Title = "Test" };
		var secondTab = new Tab { Title = "Second" };
		testTab.Items.Add(new ShellContent { Title = "Test", Content = testPage });
		secondTab.Items.Add(new ShellContent { Title = "Second", Content = secondPage });

		var tabBar = new TabBar();
		tabBar.Items.Add(testTab);
		tabBar.Items.Add(secondTab);
		Items.Add(tabBar);

		Loaded += OnShellLoaded;
	}

	void OnShellLoaded(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(CaptureFirstDefaultProbe);
	}

	void CaptureFirstDefaultProbe()
	{
		var tabBar = FindTabBar();
		_capturedTabBar = tabBar;
		_defaultRgba = GetResolvedBackgroundRgba(tabBar);
		_probeSequence = 1;
		Dispatcher.Dispatch(CaptureSecondDefaultProbe);
	}

	void CaptureSecondDefaultProbe()
	{
		var tabBar = FindTabBar();
		var currentRgba = GetResolvedBackgroundRgba(tabBar);
		_probeSequence = 2;
		SetProbeText("default", tabBar, currentRgba, currentRgba == _defaultRgba);
	}

	void OnApplyClicked(object sender, EventArgs e)
	{
		Shell.SetTabBarBackgroundColor(this, Colors.LightBlue);
		_propertyStateLabel.Text = Colors.LightBlue.Equals(Shell.GetTabBarBackgroundColor(this))
			? "property=LightBlue;sequence=1"
			: "property=unexpected;sequence=1";
		Dispatcher.Dispatch(() => Dispatcher.Dispatch(() => Probe("applied", 3)));
	}

	void OnRemoveClicked(object sender, EventArgs e)
	{
		SetValue(TabBarBackgroundColorProperty, null);
		_propertyStateLabel.Text = Shell.GetTabBarBackgroundColor(this) is null
			? "property=null;sequence=2"
			: "property=non-null;sequence=2";
		Dispatcher.Dispatch(() => Dispatcher.Dispatch(() => Probe("removed", 4)));
	}

	void Probe(string phase, int sequence)
	{
		var tabBar = FindTabBar();
		var currentRgba = GetResolvedBackgroundRgba(tabBar);
		_probeSequence = sequence;
		SetProbeText(phase, tabBar, currentRgba, true);
	}

	void SetProbeText(string phase, UITabBar tabBar, string currentRgba, bool stable)
	{
		var sameTabBar = ReferenceEquals(_capturedTabBar, tabBar);
		var titles = tabBar.Items is { Length: 2 }
			? $"{tabBar.Items[0].Title}|{tabBar.Items[1].Title}"
			: "unexpected";
		_nativeProbeLabel.Text =
			$"sequence={_probeSequence};phase={phase};same={sameTabBar};items={titles};" +
			$"frame={tabBar.Frame.Width:F1}x{tabBar.Frame.Height:F1};stable={stable};" +
			$"default={_defaultRgba};current={currentRgba}";
	}

	UITabBar FindTabBar()
	{
		if (Window.Handler?.PlatformView is UIWindow platformWindow &&
			FindTabBarController(platformWindow.RootViewController) is UITabBarController controller &&
			controller.TabBar.Frame.Width > 0 &&
			controller.TabBar.Frame.Height > 0)
		{
			return controller.TabBar;
		}

		throw new InvalidOperationException("The attached Shell UITabBar was not found.");
	}

	static UITabBarController FindTabBarController(UIViewController controller)
	{
		if (controller is UITabBarController tabBarController)
			return tabBarController;

		if (controller?.PresentedViewController is UIViewController presented &&
			FindTabBarController(presented) is UITabBarController presentedTabBar)
		{
			return presentedTabBar;
		}

		if (controller is not null)
		{
			foreach (var child in controller.ChildViewControllers)
			{
				if (FindTabBarController(child) is UITabBarController childTabBar)
					return childTabBar;
			}
		}

		return null;
	}

	static string GetResolvedBackgroundRgba(UITabBar tabBar)
	{
		var color = tabBar.StandardAppearance.BackgroundColor;
		if (color is null && OperatingSystem.IsIOSVersionAtLeast(15))
			color = tabBar.ScrollEdgeAppearance?.BackgroundColor;

		color ??= tabBar.BarTintColor
			?? tabBar.BackgroundColor
			?? UIColor.SystemBackground;
		color.GetRGBA(out var red, out var green, out var blue, out var alpha);

		return string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			"{0:F3},{1:F3},{2:F3},{3:F3}",
			(double)red,
			(double)green,
			(double)blue,
			(double)alpha);
	}
}
#endif


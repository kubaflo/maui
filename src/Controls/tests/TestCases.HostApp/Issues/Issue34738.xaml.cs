#if WINDOWS
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WNavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WWindow = Microsoft.UI.Xaml.Window;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34738, "TabBarDisabledColor is not applied when the TabBar is disabled", PlatformAffected.UWP)]
public partial class Issue34738 : TestShell
{
	public Issue34738()
	{
		InitializeComponent();
	}

	protected override void Init()
	{
	}

	internal void Prepare(Issue34738ContentPage page)
	{
		var configuredColor = Shell.GetTabBarDisabledColor(IssueTabBar);
		page.CalibrationLabel.TextColor = configuredColor;
		page.ConfiguredColorLabel.Text = configuredColor.ToString();
	}

	internal void DisableSecondTab(Issue34738ContentPage page)
	{
		page.ManagedStateLabel.Text = DisabledTab.IsEnabled.ToString();

#if WINDOWS
		if (!TryPrepareNativeObservation(page, out var tabItem, out var titleTextBlock, out var calibrationBrush))
		{
			page.ObservationLabel.Text = "Native observation unavailable";
			return;
		}

		page.NativeItemLabel.Text = titleTextBlock.Text;
		page.NativeStateLabel.Text = tabItem.IsEnabled.ToString();
		page.CalibrationColorLabel.Text = ToArgb(calibrationBrush.Color);

		System.ComponentModel.PropertyChangedEventHandler propertyChanged = null!;
		propertyChanged = (_, args) =>
		{
			if (args.PropertyName == nameof(BaseShellItem.IsEnabled))
			{
				page.ManagedStateLabel.Text = DisabledTab.IsEnabled.ToString();
				DisabledTab.PropertyChanged -= propertyChanged;
			}
		};

		long enabledToken = 0;
		enabledToken = tabItem.RegisterPropertyChangedCallback(
			WNavigationViewItem.IsEnabledProperty,
			(_, _) =>
			{
				page.NativeStateLabel.Text = tabItem.IsEnabled.ToString();
				if (!tabItem.IsEnabled)
					tabItem.UnregisterPropertyChangedCallback(WNavigationViewItem.IsEnabledProperty, enabledToken);
			});

		DisabledTab.PropertyChanged += propertyChanged;
		DisabledTab.IsEnabled = false;
#else
		page.ObservationLabel.Text = "This test is specific to Windows";
#endif
	}

	internal void ObserveDisabledColor(Issue34738ContentPage page)
	{
#if WINDOWS
		if (Window?.Handler?.PlatformView is not WWindow platformWindow ||
			platformWindow.Content is not WDependencyObject root ||
			!TryFindTabItem(root, "Disabled Tab", out var tabItem, out var titleTextBlock) ||
			tabItem.IsEnabled ||
			titleTextBlock.Foreground is not WSolidColorBrush titleBrush)
		{
			page.ObservationLabel.Text = "Native observation unavailable";
			return;
		}

		page.DisabledColorLabel.Text = ToArgb(titleBrush.Color);
		page.ObservationLabel.Text = "Color observation complete";
#else
		page.ObservationLabel.Text = "This test is specific to Windows";
#endif
	}

#if WINDOWS
	bool TryPrepareNativeObservation(
		Issue34738ContentPage page,
		out WNavigationViewItem tabItem,
		out WTextBlock titleTextBlock,
		out WSolidColorBrush calibrationBrush)
	{
		if (Window?.Handler?.PlatformView is not WWindow platformWindow ||
			platformWindow.Content is not WDependencyObject root ||
			!TryFindTabItem(root, "Disabled Tab", out tabItem, out titleTextBlock) ||
			!tabItem.IsEnabled ||
			page.CalibrationLabel.Handler?.PlatformView is not WTextBlock calibrationTextBlock ||
			calibrationTextBlock.Foreground is not WSolidColorBrush observedCalibrationBrush)
		{
			tabItem = default!;
			titleTextBlock = default!;
			calibrationBrush = default!;
			return false;
		}

		calibrationBrush = observedCalibrationBrush;
		return true;
	}

	static bool TryFindTabItem(
		WDependencyObject element,
		string title,
		out WNavigationViewItem tabItem,
		out WTextBlock titleTextBlock)
	{
		if (element is WNavigationViewItem candidate &&
			TryFindTextBlock(candidate, title, out titleTextBlock))
		{
			tabItem = candidate;
			return true;
		}

		var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < childCount; i++)
		{
			if (TryFindTabItem(
				Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i),
				title,
				out tabItem,
				out titleTextBlock))
			{
				return true;
			}
		}

		tabItem = default!;
		titleTextBlock = default!;
		return false;
	}

	static bool TryFindTextBlock(WDependencyObject element, string text, out WTextBlock result)
	{
		if (element is WTextBlock textBlock && textBlock.Text == text)
		{
			result = textBlock;
			return true;
		}

		var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < childCount; i++)
		{
			if (TryFindTextBlock(
				Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i),
				text,
				out result))
			{
				return true;
			}
		}

		result = default!;
		return false;
	}

	static string ToArgb(Windows.UI.Color color) =>
		$"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
#endif
}

public class Issue34738ContentPage : ContentPage
{
	internal Label CalibrationLabel { get; }
	internal Label CalibrationColorLabel { get; }
	internal Label ConfiguredColorLabel { get; }
	internal Label DisabledColorLabel { get; }
	internal Label ManagedStateLabel { get; }
	internal Label NativeItemLabel { get; }
	internal Label NativeStateLabel { get; }
	internal Label ObservationLabel { get; }

	public Issue34738ContentPage()
	{
		ConfiguredColorLabel = CreateLabel("ConfiguredColorLabel", "Configured color: pending");
		ManagedStateLabel = CreateLabel("ManagedStateLabel", "Managed transition: pending");
		NativeItemLabel = CreateLabel("NativeItemLabel", "Native item: pending");
		NativeStateLabel = CreateLabel("NativeStateLabel", "Native transition: pending");
		CalibrationColorLabel = CreateLabel("CalibrationColorLabel", "Calibration color pending");
		DisabledColorLabel = CreateLabel("DisabledColorLabel", "Disabled color pending");
		ObservationLabel = CreateLabel("ObservationLabel", "Color observation pending");
		CalibrationLabel = CreateLabel("CalibrationLabel", "Green calibration");

		var disableButton = new Button
		{
			AutomationId = "DisableButton",
			Text = "Disable second tab"
		};
		disableButton.Clicked += (_, _) =>
		{
			if (Shell.Current is Issue34738 issue)
				issue.DisableSecondTab(this);
		};

		var observeButton = new Button
		{
			AutomationId = "ObserveButton",
			Text = "Check rendered disabled color"
		};
		observeButton.Clicked += (_, _) =>
		{
			if (Shell.Current is Issue34738 issue)
				issue.ObserveDisabledColor(this);
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Shell TabBarDisabledColor on Windows"
					},
					ConfiguredColorLabel,
					CalibrationLabel,
					disableButton,
					observeButton,
					ManagedStateLabel,
					NativeItemLabel,
					NativeStateLabel,
					CalibrationColorLabel,
					DisabledColorLabel,
					ObservationLabel
				}
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (Shell.Current is Issue34738 issue)
			issue.Prepare(this);
	}

	static Label CreateLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};
}

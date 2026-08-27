#if WINDOWS
using Microsoft.Maui.Platform;
using WBitmapIcon = Microsoft.UI.Xaml.Controls.BitmapIcon;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WDependencyPropertyChangedEventArgs = Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs;
using WNavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34738, "[Windows] TabBarDisabledColor is not applied to a disabled tab", PlatformAffected.UWP)]
public partial class Issue34738 : Shell
{
#if WINDOWS
	Microsoft.Maui.Platform.MauiNavigationView _nativeNavigationView = null!;
	WNavigationViewItem _storedTab2Item = null!;
#endif

	public Issue34738()
	{
		InitializeComponent();
#if WINDOWS
		Loaded += OnShellLoaded;
#endif
	}

	void OnDisableTab2Clicked(object sender, EventArgs e)
	{
#if WINDOWS
		_storedTab2Item.IsEnabledChanged += OnNativeTab2IsEnabledChanged;
		SecondTab.IsEnabled = false;
#endif
	}

#if WINDOWS
	void OnShellLoaded(object sender, EventArgs e)
	{
		Loaded -= OnShellLoaded;

		if (CurrentItem?.Handler?.PlatformView is not Microsoft.Maui.Platform.MauiNavigationView navigationView)
		{
			OracleStatus.Text = "Setup failed: native navigation view unavailable";
			return;
		}

		_nativeNavigationView = navigationView;
		_nativeNavigationView.LayoutUpdated += OnNativeNavigationViewLayoutUpdated;
		TrySetOracleReady();
	}

	void OnNativeNavigationViewLayoutUpdated(object sender, object e)
	{
		TrySetOracleReady();
	}

	void TrySetOracleReady()
	{
		if (!TryFindTab2Item(_nativeNavigationView, out var nativeTab2) ||
			!TryFindDescendant(nativeTab2, IsTab2Title, out WTextBlock title) ||
			!TryFindDescendant(nativeTab2, IsGroceriesIcon, out WBitmapIcon icon) ||
			title.Foreground is not WSolidColorBrush titleBrush ||
			icon.Foreground is not WSolidColorBrush iconBrush ||
			nativeTab2.ActualWidth <= 0 ||
			nativeTab2.ActualHeight <= 0 ||
			!SecondTab.IsEnabled ||
			!nativeTab2.IsEnabled)
		{
			return;
		}

		_nativeNavigationView.LayoutUpdated -= OnNativeNavigationViewLayoutUpdated;
		_storedTab2Item = nativeTab2;
		OracleStatus.Text = $"Ready:Tab2;Icon=groceries.png;Frame={nativeTab2.ActualWidth:F0}x{nativeTab2.ActualHeight:F0};ManagedEnabled=True;NativeEnabled=True;Title={FormatColor(titleBrush)};IconForeground={FormatColor(iconBrush)}";
	}

	void OnNativeTab2IsEnabledChanged(object sender, WDependencyPropertyChangedEventArgs e)
	{
		if (_storedTab2Item.IsEnabled)
			return;

		_storedTab2Item.IsEnabledChanged -= OnNativeTab2IsEnabledChanged;
		if (!_storedTab2Item.DispatcherQueue.TryEnqueue(InspectDisabledTab2))
			OracleStatus.Text = "Setup failed: native observation could not be queued";
	}

	void InspectDisabledTab2()
	{
		if (!TryFindTab2Item(_nativeNavigationView, out var currentTab2) ||
			!TryFindDescendant(currentTab2, IsTab2Title, out WTextBlock title) ||
			!TryFindDescendant(currentTab2, IsGroceriesIcon, out WBitmapIcon icon) ||
			title.Foreground is not WSolidColorBrush titleBrush ||
			icon.Foreground is not WSolidColorBrush iconBrush)
		{
			OracleStatus.Text = "Setup failed: disabled native tab visuals unavailable";
			return;
		}

		var sameItem = ReferenceEquals(_storedTab2Item, currentTab2);
		var expectedColor = Shell.GetTabBarDisabledColor(this).ToWindowsColor();
		ExpectedForeground.Text = FormatColor(expectedColor.A, expectedColor.R, expectedColor.G, expectedColor.B);
		TitleForeground.Text = FormatColor(titleBrush);
		IconForeground.Text = FormatColor(iconBrush);
		OracleStatus.Text = $"Observed:SameItem={sameItem};ManagedEnabled={SecondTab.IsEnabled};NativeEnabled={currentTab2.IsEnabled}";
	}

	static bool TryFindTab2Item(WDependencyObject parent, out WNavigationViewItem result)
	{
		if (parent is WNavigationViewItem item && item.Content is string content && content == "Tab2")
		{
			result = item;
			return true;
		}

		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < childCount; i++)
		{
			if (TryFindTab2Item(WVisualTreeHelper.GetChild(parent, i), out result))
				return true;
		}

		result = null!;
		return false;
	}

	static bool TryFindDescendant<T>(WDependencyObject parent, Func<T, bool> predicate, out T result)
		where T : WDependencyObject
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < childCount; i++)
		{
			var child = WVisualTreeHelper.GetChild(parent, i);
			if (child is T candidate && predicate(candidate))
			{
				result = candidate;
				return true;
			}

			if (TryFindDescendant(child, predicate, out result))
				return true;
		}

		result = null!;
		return false;
	}

	static bool IsTab2Title(WTextBlock textBlock) => textBlock.Text == "Tab2";

	static bool IsGroceriesIcon(WBitmapIcon icon) =>
		icon.UriSource is not null &&
		icon.UriSource.AbsolutePath.EndsWith("groceries.png", StringComparison.OrdinalIgnoreCase);

	static string FormatColor(WSolidColorBrush brush) =>
		FormatColor(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);

	static string FormatColor(byte alpha, byte red, byte green, byte blue) =>
		$"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
#endif
}

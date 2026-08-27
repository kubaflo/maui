#if WINDOWS
using Microsoft.Maui.Platform;
using WMicaBackdrop = Microsoft.UI.Xaml.Media.MicaBackdrop;
using WMicaKind = Microsoft.UI.Composition.SystemBackdrops.MicaKind;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31510, "Shell Flyout and windowTitleBar Background Transparency Overlap on Windows", PlatformAffected.UWP)]
public class Issue31510 : Shell
{
	const string ArrangedColor = "#85FFFFFF";

	readonly TitleBar _issueTitleBar;
	readonly Grid _contentRoot;
	readonly Grid _micaReference;
	readonly Button _attachmentStatus;
	bool _titleBarAssigned;

	public Issue31510()
	{
		FlyoutBackgroundColor = Color.FromArgb(ArrangedColor);

		_issueTitleBar = new TitleBar
		{
			AutomationId = "Issue31510TitleBar",
			Title = "MAUI App",
			BackgroundColor = Color.FromArgb(ArrangedColor)
		};

		_attachmentStatus = new Button
		{
			AutomationId = "Issue31510AttachmentStatus",
			HorizontalOptions = LayoutOptions.Start,
			Text = "ATTACHING"
		};

		_micaReference = new Grid
		{
			AutomationId = "Issue31510MicaReference",
			MinimumHeightRequest = 120
		};

		_contentRoot = new Grid
		{
			AutomationId = "Issue31510ContentRoot",
			Padding = 40,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 20,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 32,
					Text = "Transparent title bar and flyout overlap"
				},
				new Label
				{
					FontSize = 18,
					Text = "The title bar and Shell flyout both use #85FFFFFF. Their upper-left overlap should remain semi-transparent instead of becoming an opaque white band."
				}.Row(1),
				_attachmentStatus.Row(2),
				_micaReference.Row(3)
			}
		};

		var content = new ContentPage
		{
			Title = "QuickStart",
			Content = _contentRoot
		};

		Items.Add(new FlyoutItem
		{
			Title = "QuickStart",
			Items =
			{
				new ShellContent
				{
					Title = "QuickStart",
					Route = "MainPage",
					Content = content
				}
			}
		});

		Loaded += (_, _) => OnIssueLoaded();
		SizeChanged += (_, _) => TryPublishAttachedState();
		_issueTitleBar.Loaded += (_, _) => TryPublishAttachedState();
		_issueTitleBar.SizeChanged += (_, _) => TryPublishAttachedState();
		_micaReference.Loaded += (_, _) => TryPublishAttachedState();
		_micaReference.SizeChanged += (_, _) => TryPublishAttachedState();
	}

	void OnIssueLoaded()
	{
		var window = Window;
		if (!_titleBarAssigned && window is not null)
		{
#if WINDOWS
			if (window.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
			{
				return;
			}

			nativeWindow.SystemBackdrop = new WMicaBackdrop
			{
				Kind = WMicaKind.BaseAlt
			};
#endif
			_titleBarAssigned = true;
			window.TitleBar = _issueTitleBar;
		}

		TryPublishAttachedState();
	}

	void TryPublishAttachedState()
	{
#if WINDOWS
		var status = _attachmentStatus.Text ?? string.Empty;
		if (status.StartsWith("READY|", StringComparison.Ordinal))
		{
			return;
		}

		if (Window?.Handler?.PlatformView is not MauiWinUIWindow nativeWindow ||
			Handler?.PlatformView is not MauiNavigationView navigationView ||
			_issueTitleBar.Handler?.PlatformView is not WFrameworkElement titleBarElement ||
			_micaReference.Handler?.PlatformView is not WFrameworkElement micaElement)
		{
			return;
		}

		if (nativeWindow.SystemBackdrop is not WMicaBackdrop micaBackdrop ||
			micaBackdrop.Kind != WMicaKind.BaseAlt)
		{
			_attachmentStatus.Text = "INVALID:BaseAlt Mica backdrop was not applied";
			return;
		}

		if (FindDescendantPanel(navigationView, "PaneContentGrid") is not WPanel paneContentGrid ||
			paneContentGrid.Background is not WSolidColorBrush paneBrush ||
			!IsArrangedColor(paneBrush) ||
			titleBarElement is not WPanel titleBarPanel ||
			titleBarPanel.Background is not WSolidColorBrush titleBarBrush ||
			!IsArrangedColor(titleBarBrush))
		{
			return;
		}

		var paneWidth = navigationView.IsPaneOpen ||
			navigationView.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded
				? navigationView.OpenPaneLength
				: navigationView.CompactPaneLength;
		var titleFrame = GetFrame(titleBarElement);
		var shellFrame = GetFrame(navigationView);
		var micaFrame = GetFrame(micaElement);
		if (!HasArea(titleFrame.Width, titleFrame.Height) ||
			!HasArea(paneWidth, shellFrame.Height) ||
			!HasArea(micaFrame.Width, micaFrame.Height))
			return;

		_attachmentStatus.Text = FormattableString.Invariant(
			$"READY|T:{Format(titleFrame)}|P:{Format((shellFrame.X, shellFrame.Y, paneWidth, shellFrame.Height))}|M:{Format(micaFrame)}");
#endif
	}

#if WINDOWS
	static bool IsArrangedColor(WSolidColorBrush brush) =>
		brush.Color.A == 0x85 &&
		brush.Color.R == 0xFF &&
		brush.Color.G == 0xFF &&
		brush.Color.B == 0xFF;

	static WPanel FindDescendantPanel(WDependencyObject parent, string name)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(parent, index);
			if (child is WPanel panel && panel.Name == name)
				return panel;

			if (FindDescendantPanel(child, name) is WPanel descendant)
				return descendant;
		}

		return null;
	}

	static (double X, double Y, double Width, double Height) GetFrame(WFrameworkElement element)
	{
		var origin = element.TransformToVisual(null).TransformPoint(default);
		return (origin.X, origin.Y, element.ActualWidth, element.ActualHeight);
	}

	static bool HasArea(double width, double height) => width > 0 && height > 0;

	static string Format((double X, double Y, double Width, double Height) frame) => string.Join(",",
		FormattableString.Invariant($"{frame.X:0.###}"),
		FormattableString.Invariant($"{frame.Y:0.###}"),
		FormattableString.Invariant($"{frame.Width:0.###}"),
		FormattableString.Invariant($"{frame.Height:0.###}"));
#endif
}


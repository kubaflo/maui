#if WINDOWS
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using WWindow = Microsoft.UI.Xaml.Window;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31510, "Shell flyout and TitleBar transparency overlap", PlatformAffected.UWP)]
public class Issue31510 : Shell
{
	readonly Microsoft.Maui.Controls.TitleBar _issueTitleBar;
	readonly Color _overlayColor;
	readonly Label _nativeSetupStatus;
	readonly Label _presentationStatus;
	readonly Label _flyoutStateStatus;
	int _presentationToken = -1;

	public Issue31510()
	{
		_overlayColor = Color.FromArgb("#85FFFFFF");
		FlyoutBehavior = FlyoutBehavior.Flyout;
		FlyoutBackgroundColor = _overlayColor;
		FlyoutIsPresented = false;
		AutomationId = "Issue31510WindowProbe";

		_issueTitleBar = new Microsoft.Maui.Controls.TitleBar
		{
			BackgroundColor = _overlayColor,
			AutomationId = "Issue31510TitleBarProbe"
		};

		FlyoutHeader = new VerticalStackLayout
		{
			AutomationId = "Issue31510FlyoutProbe",
			Padding = new Thickness(20, 12),
			Children =
			{
				new Label
				{
					AutomationId = "Issue31510FlyoutEvidence",
					Text = "Flyout background #85FFFFFF",
					FontAttributes = FontAttributes.Bold
				},
				new Label { Text = "The top region overlaps the title bar." }
			}
		};

		_nativeSetupStatus = new Label
		{
			AutomationId = "Issue31510NativeSetup",
			Text = "Not ready"
		};
		_presentationStatus = new Label
		{
			AutomationId = "Issue31510PresentationToken",
			Text = _presentationToken.ToString()
		};
		_flyoutStateStatus = new Label
		{
			AutomationId = "Issue31510FlyoutState",
			Text = bool.FalseString
		};

		var openButton = new Button
		{
			AutomationId = "Issue31510OpenFlyoutButton",
			Text = "Open flyout",
			HorizontalOptions = LayoutOptions.Start
		};
		openButton.Clicked += OnOpenFlyoutClicked;

		var contentPage = new ContentPage
		{
			Content = new Grid
			{
				AutomationId = "Issue31510ContentProbe",
				Padding = new Thickness(24),
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				RowSpacing = 16,
				Children =
				{
					new Label
					{
						Text = "Issue 31510: Shell flyout and title bar transparency",
						FontAttributes = FontAttributes.Bold,
						FontSize = 22
					},
					new Label
					{
						Text = "Open the flyout and compare its top overlap with the #85FFFFFF title bar.",
						FontSize = 16
					}.Row(1),
					openButton.Row(2),
					new VerticalStackLayout
					{
						HorizontalOptions = LayoutOptions.End,
						VerticalOptions = LayoutOptions.Start,
						Children =
						{
							_nativeSetupStatus,
							_presentationStatus,
							_flyoutStateStatus
						}
					}.Row(3)
				}
			}
		};
		contentPage.Appearing += OnContentPageAppearing;

		Items.Add(new FlyoutItem
		{
			Title = "Issue 31510",
			Items =
			{
				new ShellContent
				{
					Title = "Transparency overlap",
					Route = "MainPage",
					Content = contentPage
				}
			}
		});
	}

	void OnContentPageAppearing(object sender, EventArgs e)
	{
		if (Window is null)
			return;

		Window.TitleBar = _issueTitleBar;

#if WINDOWS
		if (Window.Handler?.PlatformView is WWindow platformWindow)
		{
			platformWindow.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
			string backdropKind = platformWindow.SystemBackdrop is MicaBackdrop micaBackdrop
				? micaBackdrop.Kind.ToString()
				: platformWindow.SystemBackdrop?.GetType().Name ?? "null";
			_nativeSetupStatus.Text =
				$"Mica={backdropKind};TitleBar={_issueTitleBar.BackgroundColor.ToArgbHex(true)};Flyout={FlyoutBackgroundColor.ToArgbHex(true)}";
		}
#endif
	}

	void OnOpenFlyoutClicked(object sender, EventArgs e)
	{
		PropertyChanged -= OnShellPropertyChanged;
		PropertyChanged += OnShellPropertyChanged;
		FlyoutIsPresented = true;
	}

	void OnShellPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != FlyoutIsPresentedProperty.PropertyName || !FlyoutIsPresented)
			return;

		PropertyChanged -= OnShellPropertyChanged;
		_presentationToken = 1;
		_presentationStatus.Text = _presentationToken.ToString();
		_flyoutStateStatus.Text = bool.TrueString;
	}
}


#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "[Windows] Full-screen modal page reserves title bar space", PlatformAffected.UWP)]
public class Issue36040 : ContentPage
{
	readonly bool _isShellContent;
	readonly Button _pushModalButton;
	readonly Label _readyLabel;

	public Issue36040()
		: this(false)
	{
	}

	Issue36040(bool isShellContent)
	{
		_isShellContent = isShellContent;
		AutomationId = isShellContent ? "MainPage" : "IssuePage";
		Shell.SetNavBarIsVisible(this, false);
		BackgroundColor = Colors.Blue;

		_readyLabel = new Label
		{
			Text = "Preparing full-screen window",
			AutomationId = "ReadyLabel",
			HorizontalOptions = LayoutOptions.Center,
			TextColor = Colors.White
		};

		_pushModalButton = new Button
		{
			Text = "Push Modal Page",
			AutomationId = "PushModalButton",
			HorizontalOptions = LayoutOptions.Center,
			IsEnabled = false
		};
		_pushModalButton.Clicked += OnPushModalClicked;

		Content = new VerticalStackLayout
		{
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "This is the MAIN PAGE (Blue)",
					AutomationId = "MainPageLabel",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center,
					TextColor = Colors.White
				},
				_pushModalButton,
				_readyLabel
			}
		};

		Loaded += OnLoaded;
		SizeChanged += OnSizeChanged;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		if (!_isShellContent && Window is not null)
		{
			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items =
				{
					new ShellContent
					{
						Content = new Issue36040(true)
					}
				}
			};

			Window.Page = shell;
			return;
		}

		if (Window?.Handler?.PlatformView is not object platformWindow)
		{
			_readyLabel.Text = "Setup failed: Windows window unavailable";
			return;
		}

		if (!TryEnterFullScreen(platformWindow))
		{
			_readyLabel.Text = "Setup failed: full-screen presenter unavailable";
			return;
		}

		Dispatcher.Dispatch(TryMarkReady);
	}

	void OnSizeChanged(object sender, EventArgs e)
	{
		if (_isShellContent)
			TryMarkReady();
	}

	void TryMarkReady()
	{
		if (_pushModalButton.IsEnabled ||
			Width <= 0 ||
			Height <= 0 ||
			Window?.Handler?.PlatformView is not object platformWindow ||
			!IsFullScreen(platformWindow))
		{
			return;
		}

		_readyLabel.Text = "Ready: full-screen main page";
		_pushModalButton.IsEnabled = true;
	}

	static bool TryEnterFullScreen(object platformWindow)
	{
#if WINDOWS
		if (platformWindow is not Microsoft.UI.Xaml.Window window)
			return false;

		var appWindow = window.GetAppWindow();
		if (appWindow is null)
			return false;

		if (!appWindow.TitleBar.ExtendsContentIntoTitleBar)
			return false;

		appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

		return IsFullScreenAppWindow(appWindow);
#else
		return false;
#endif
	}

	static bool IsFullScreen(object platformWindow)
	{
#if WINDOWS
		if (platformWindow is not Microsoft.UI.Xaml.Window window)
			return false;

		var appWindow = window.GetAppWindow();
		return appWindow is not null && IsFullScreenAppWindow(appWindow);
#else
		return false;
#endif
	}

#if WINDOWS
	static bool IsFullScreenAppWindow(AppWindow appWindow)
	{
		return appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
	}
#endif

	async void OnPushModalClicked(object sender, EventArgs e)
	{
		_pushModalButton.IsEnabled = false;
		await Navigation.PushModalAsync(new Issue36040ModalPage());
	}

	sealed class Issue36040ModalPage : ContentPage
	{
		public Issue36040ModalPage()
		{
			AutomationId = "ModalPage";
			BackgroundColor = Colors.Red;

			var topButton = new Button
			{
				Text = "Top edge button",
				AutomationId = "ModalTopButton",
				HorizontalOptions = LayoutOptions.Center
			};
			var modalLabel = new Label
			{
				Text = "This is the MODAL PAGE (Red)",
				AutomationId = "ModalPageLabel",
				FontSize = 24,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				TextColor = Colors.White
			};
			var modalGrid = new Grid
			{
				AutomationId = "ModalContent",
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			Grid.SetRow(topButton, 0);
			Grid.SetRow(modalLabel, 1);
			modalGrid.Add(topButton);
			modalGrid.Add(modalLabel);
			Content = modalGrid;
		}
	}
}

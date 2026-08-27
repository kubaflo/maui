#if WINDOWS
using Microsoft.Maui.Platform;
using WAppWindow = Microsoft.UI.Windowing.AppWindow;
using WAppWindowChangedEventArgs = Microsoft.UI.Windowing.AppWindowChangedEventArgs;
using WAppWindowPresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind;
using WMauiWinUIWindow = Microsoft.Maui.MauiWinUIWindow;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page leaves a title bar gap in Windows full-screen mode", PlatformAffected.WinRT)]
public class Issue36040 : ContentPage
{
	bool _fullScreenInitializationStarted;

	public Issue36040()
	{
		BackgroundColor = Colors.Blue;
		NavigationPage.SetHasNavigationBar(this, false);

		var fullScreenReadyLabel = new Label
		{
			AutomationId = "FullScreenReady",
			Text = "Full screen ready",
			IsVisible = false,
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			FontSize = 18,
			Padding = 12,
			HorizontalTextAlignment = TextAlignment.Center
		};

		var pushModalButton = new Button
		{
			AutomationId = "PushModalButton",
			Text = "Push Modal Page",
			HorizontalOptions = LayoutOptions.Center
		};
		pushModalButton.Clicked += async (_, _) => await PushModalAsync();

		var mainContent = new VerticalStackLayout
		{
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "This is the MAIN PAGE (Blue)",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center,
					TextColor = Colors.White
				},
				pushModalButton
			}
		};

		var mainLayout = new Grid
		{
			AutomationId = "MainLayout",
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		mainLayout.Add(mainContent);
		mainLayout.Add(fullScreenReadyLabel, row: 1);
		Content = mainLayout;

		Loaded += (_, _) =>
		{
			if (_fullScreenInitializationStarted)
				return;

			_fullScreenInitializationStarted = true;

			var window = Window;
			if (window is null)
				throw new InvalidOperationException("The Windows MAUI window was not available.");

			var windowHandler = window.Handler;
			if (windowHandler is null ||
				windowHandler.PlatformView is not WMauiWinUIWindow platformWindow)
				throw new InvalidOperationException("The Windows MAUI window handler was not available.");

			var appWindow = platformWindow.GetAppWindow();
			if (appWindow is null)
				throw new InvalidOperationException("The Windows AppWindow was not available.");

			void PublishReady(WAppWindow sender)
			{
				if (sender.Presenter.Kind != WAppWindowPresenterKind.FullScreen)
					return;

				sender.Changed -= OnAppWindowChanged;
				fullScreenReadyLabel.IsVisible = true;
			}

			void OnAppWindowChanged(WAppWindow sender, WAppWindowChangedEventArgs _) =>
				PublishReady(sender);

			appWindow.Changed += OnAppWindowChanged;
			appWindow.SetPresenter(WAppWindowPresenterKind.FullScreen);
			PublishReady(appWindow);
		};
	}

	async Task PushModalAsync()
	{
		var topMarker = new BoxView
		{
			AutomationId = "ModalTopMarker",
			BackgroundColor = Colors.Yellow,
			HeightRequest = 4,
			HorizontalOptions = LayoutOptions.Fill
		};

		var modalLabel = new Label
		{
			AutomationId = "ModalPageLabel",
			Text = "This is the MODAL PAGE (Red)",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			TextColor = Colors.White
		};

		var modalReadyLabel = new Label
		{
			Text = "Modal loaded",
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			FontSize = 18,
			Padding = 12,
			HorizontalTextAlignment = TextAlignment.Center
		};

		var modalGrid = new Grid
		{
			AutomationId = "ModalLayout",
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		modalGrid.Add(topMarker);
		modalGrid.Add(modalLabel, row: 1);
		modalGrid.Add(modalReadyLabel, row: 2);

		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalGrid
		};
		NavigationPage.SetHasNavigationBar(modalPage, false);

		await Navigation.PushModalAsync(modalPage);
	}
}
#endif


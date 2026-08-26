#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using WWindow = Microsoft.UI.Xaml.Window;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page reserves title bar space in full-screen mode", PlatformAffected.UWP)]
public class Issue36040 : ContentPage
{
	readonly Label _fullScreenStatus;

	public Issue36040()
	{
		BackgroundColor = Colors.Blue;
		NavigationPage.SetHasNavigationBar(this, false);

		_fullScreenStatus = new Label
		{
			AutomationId = "FullScreenStatus",
			Text = "EnteringFullScreen"
		};

		var pushModalButton = new Button
		{
			AutomationId = "PushModalButton",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Push Modal Page"
		};
		pushModalButton.Clicked += OnPushModalClicked;

		var mainContent = new VerticalStackLayout
		{
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center,
					Text = "This is the MAIN PAGE (Blue)",
					TextColor = Colors.White
				},
				pushModalButton
			}
		};

		var mainSurface = new Grid
		{
			AutomationId = "MainSurface",
			BackgroundColor = Colors.Blue,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		mainSurface.Add(mainContent);
		mainSurface.Add(_fullScreenStatus, 0, 1);
		Content = mainSurface;

		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		if (Window?.Handler?.PlatformView is not WWindow platformWindow)
		{
			_fullScreenStatus.Text = "PlatformWindowUnavailable";
			return;
		}

		var appWindow = platformWindow.GetAppWindow();
		if (appWindow is null)
		{
			_fullScreenStatus.Text = "AppWindowUnavailable";
			return;
		}

		appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
		_fullScreenStatus.Text = appWindow.Presenter.Kind.ToString();
#endif
	}

	async void OnPushModalClicked(object sender, EventArgs e)
	{
		var modalLoadedMarker = new Label
		{
			AutomationId = "ModalLoadedMarker",
			Text = "ModalLoading"
		};

		var modalSurface = new Grid
		{
			AutomationId = "ModalSurface",
			BackgroundColor = Colors.Red,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		modalSurface.Add(new Button
		{
			AutomationId = "ModalTopButton",
			HeightRequest = 48,
			HorizontalOptions = LayoutOptions.Fill,
			Text = "Top of modal page"
		});
		modalSurface.Add(new Label
		{
			AutomationId = "ModalPageMarker",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			Text = "This is the MODAL PAGE (Red)",
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center
		}, 0, 1);
		modalSurface.Add(modalLoadedMarker, 0, 2);

		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalSurface,
			Padding = 0
		};
		modalPage.Loaded += (_, _) => modalLoadedMarker.Text = "ModalLoaded";

		await Navigation.PushModalAsync(modalPage);
	}
}


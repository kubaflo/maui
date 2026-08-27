#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using WWindow = Microsoft.UI.Xaml.Window;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page leaves a title bar gap in Windows full-screen mode", PlatformAffected.WinPhone)]
public class Issue36040 : ContentPage
{
	bool _hasPushedModal;

	public Issue36040()
	{
		BackgroundColor = Colors.Blue;
		NavigationPage.SetHasNavigationBar(this, false);

		var fullScreenStatusLabel = new Label
		{
			AutomationId = "Issue36040FullScreenStatus",
			Text = "Presenter kind: pending",
			TextColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center
		};

		var pushModalButton = new Button
		{
			AutomationId = "Issue36040PushModalButton",
			Text = "Push Modal Page",
			HorizontalOptions = LayoutOptions.Center
		};
		pushModalButton.Clicked += async (_, _) => await PushModalPageAsync();

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
				fullScreenStatusLabel,
				pushModalButton
			}
		};

		var mainRoot = new Grid
		{
			AutomationId = "Issue36040MainRoot",
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		mainRoot.Add(mainContent, 0, 0);
		Content = mainRoot;

		Loaded += (_, _) =>
		{
			if (Window?.Handler?.PlatformView is not WWindow platformWindow)
			{
				fullScreenStatusLabel.Text = "Presenter kind: unavailable";
				return;
			}

			var appWindow = platformWindow.GetAppWindow();
			if (appWindow is null)
			{
				fullScreenStatusLabel.Text = "Presenter kind: unavailable";
				return;
			}

			appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
			Dispatcher.Dispatch(() =>
			{
				fullScreenStatusLabel.Text = $"Presenter kind: {appWindow.Presenter.Kind}";
			});
		};
	}

	async Task PushModalPageAsync()
	{
		if (_hasPushedModal)
			return;

		_hasPushedModal = true;
		var pressCount = -1;
		var pressCountLabel = new Label
		{
			AutomationId = "Issue36040PressCount",
			Text = $"Top-edge presses: {pressCount}",
			HorizontalOptions = LayoutOptions.Center,
			TextColor = Colors.White
		};
		var topEdgeButton = new Button
		{
			AutomationId = "Issue36040TopEdgeButton",
			Text = "Top-edge click target",
			HorizontalOptions = LayoutOptions.Fill
		};
		topEdgeButton.Clicked += (_, _) =>
		{
			pressCount++;
			pressCountLabel.Text = $"Top-edge presses: {pressCount}";
		};

		var modalBody = new VerticalStackLayout
		{
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					AutomationId = "Issue36040ModalTitle",
					Text = "This is the MODAL PAGE (Red)",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center,
					TextColor = Colors.White
				},
				pressCountLabel
			}
		};
		var modalStatusLabel = new Label
		{
			AutomationId = "Issue36040ModalStatus",
			Text = "Modal state: -1",
			HorizontalOptions = LayoutOptions.Center,
			TextColor = Colors.White
		};
		var modalRoot = new Grid
		{
			AutomationId = "Issue36040ModalRoot",
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		modalRoot.Add(topEdgeButton, 0, 0);
		modalRoot.Add(modalBody, 0, 1);
		modalRoot.Add(modalStatusLabel, 0, 2);

		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalRoot
		};
		NavigationPage.SetHasNavigationBar(modalPage, false);
		modalPage.Loaded += (_, _) =>
		{
			pressCount = 0;
			pressCountLabel.Text = $"Top-edge presses: {pressCount}";
			modalStatusLabel.Text = "Modal state: 1";
		};

		await Navigation.PushModalAsync(modalPage);
	}
}
#endif


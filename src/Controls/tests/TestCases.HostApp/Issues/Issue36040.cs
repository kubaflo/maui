namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page has a title bar gap in full-screen mode on Windows", PlatformAffected.UWP)]
public class Issue36040 : ContentPage
{
	readonly Button _pushModalButton;
	readonly Label _statusLabel;
	bool _fullScreenConfigured;

	public Issue36040()
	{
		BackgroundColor = Colors.Blue;
		NavigationPage.SetHasNavigationBar(this, false);

		_pushModalButton = new Button
		{
			AutomationId = "PushModalButton",
			HorizontalOptions = LayoutOptions.Center,
			IsEnabled = false,
			IsVisible = false,
			Text = "Push Modal Page",
		};
		_pushModalButton.Clicked += OnPushModalClicked;

		_statusLabel = new Label
		{
			AutomationId = "Issue36040Status",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Waiting for full screen",
			TextColor = Colors.White,
		};

		var mainLayout = new VerticalStackLayout
		{
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					AutomationId = "Issue36040MainCaption",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center,
					Text = "This is the MAIN PAGE (Blue)",
					TextColor = Colors.White,
				},
				_pushModalButton,
				_statusLabel,
			},
		};

		Content = new Grid
		{
			AutomationId = "Issue36040MainRoot",
			BackgroundColor = Colors.Blue,
			Children = { mainLayout },
		};

		Loaded += OnPageLoaded;
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		if (_fullScreenConfigured)
			return;

#if WINDOWS
		if (Window?.Handler?.PlatformView is not Microsoft.Maui.MauiWinUIWindow platformWindow)
			return;

		var appWindow = Microsoft.Maui.Platform.WindowExtensions.GetAppWindow(platformWindow);
		if (appWindow is null)
			return;

		appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
		if (appWindow.Presenter.Kind != Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)
			return;

		_fullScreenConfigured = true;
		Dispatcher.Dispatch(() =>
		{
			_statusLabel.Text = "Full screen ready";
			_pushModalButton.IsEnabled = true;
			_pushModalButton.IsVisible = true;
		});
#endif
	}

	async void OnPushModalClicked(object sender, EventArgs e)
	{
		_pushModalButton.IsEnabled = false;

		var modalRoot = new Grid
		{
			AutomationId = "Issue36040ModalRoot",
			BackgroundColor = Colors.Red,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
			},
		};

		modalRoot.Add(new Button
		{
			AutomationId = "ModalTopButton",
			Text = "Top edge button",
		});

		var modalCaption = new Label
		{
			AutomationId = "Issue36040ModalCaption",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			Text = "This is the MODAL PAGE (Red)",
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center,
		};
		Grid.SetRow(modalCaption, 1);
		modalRoot.Add(modalCaption);

		if (_statusLabel.Parent is Layout statusParent)
			statusParent.Remove(_statusLabel);

		_statusLabel.Text = "Modal pushed";
		Grid.SetRow(_statusLabel, 2);
		modalRoot.Add(_statusLabel);

		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalRoot,
		};
		NavigationPage.SetHasNavigationBar(modalPage, false);

		await Navigation.PushModalAsync(modalPage);
	}
}


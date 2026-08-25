#if WINDOWS
using WAppWindowPresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page leaves a title bar gap in full-screen mode", PlatformAffected.WinRT)]
public class Issue36040 : ContentPage
{
	readonly Label _fullScreenReadyLabel;
	readonly Grid _mainGrid;

	public Issue36040()
	{
		BackgroundColor = Colors.Blue;
		NavigationPage.SetHasNavigationBar(this, false);

		_fullScreenReadyLabel = new Label
		{
			AutomationId = "Issue36040FullScreenReady",
			Text = "-1",
			TextColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center
		};

		var pushModalButton = new Button
		{
			AutomationId = "Issue36040PushModalButton",
			Text = "Push Modal Page",
			Margin = 0,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start
		};
		pushModalButton.Clicked += OnPushModalClicked;

		_mainGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				pushModalButton,
				new Label
				{
					AutomationId = "Issue36040MainPageLabel",
					Text = "MAIN PAGE",
					FontSize = 36,
					TextColor = Colors.White,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}.Row(1)
			}
		};
		Content = _mainGrid;

		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		if (Window?.Handler?.PlatformView is not Microsoft.Maui.MauiWinUIWindow platformWindow)
			return;

		platformWindow.AppWindow.SetPresenter(WAppWindowPresenterKind.FullScreen);
		Dispatcher.Dispatch(() =>
		{
			_fullScreenReadyLabel.Text = "1";
			if (_fullScreenReadyLabel.Parent is null)
				_mainGrid.Add(_fullScreenReadyLabel, row: 2);
		});
#endif
	}

	async void OnPushModalClicked(object sender, EventArgs e)
	{
		var modalLoadedLabel = new Label
		{
			AutomationId = "Issue36040ModalLoaded",
			Text = "-1",
			TextColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center
		};
		var modalTopEdgeButton = new Button
		{
			AutomationId = "Issue36040ModalTopEdgeButton",
			Text = "Modal Top Edge",
			Margin = 0,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start
		};
		var modalGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				modalTopEdgeButton,
				new Label
				{
					AutomationId = "Issue36040ModalPageLabel",
					Text = "MODAL PAGE",
					FontSize = 36,
					TextColor = Colors.White,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}.Row(1)
			}
		};
		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalGrid
		};
		NavigationPage.SetHasNavigationBar(modalPage, false);
		modalPage.Loaded += (_, _) => modalPage.Dispatcher.Dispatch(() =>
		{
			modalLoadedLabel.Text = "1";
			if (modalLoadedLabel.Parent is null)
				modalGrid.Add(modalLoadedLabel, row: 2);
		});

		await Navigation.PushModalAsync(modalPage);
	}
}


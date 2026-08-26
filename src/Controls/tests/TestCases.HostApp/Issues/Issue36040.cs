#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36040, "Modal page has a title bar gap in Windows full-screen mode", PlatformAffected.UWP)]
public class Issue36040 : ContentPage
{
	public Issue36040()
	{
		NavigationPage.SetHasNavigationBar(this, false);
		BackgroundColor = Colors.Blue;

		var pushModalButton = new Button
		{
			AutomationId = "PushModalButton",
			Text = "Push Modal Page",
			HorizontalOptions = LayoutOptions.Center
		};
		pushModalButton.Clicked += OnPushModalClicked;

		Content = new Grid
		{
			Children =
			{
				new VerticalStackLayout
				{
					Spacing = 20,
					VerticalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							AutomationId = "MainPageMarker",
							Text = "This is the MAIN PAGE (Blue)",
							FontSize = 24,
							HorizontalOptions = LayoutOptions.Center,
							TextColor = Colors.White
						},
						pushModalButton
					}
				},
				new Button
				{
					AutomationId = "FullScreenTopBaseline",
					Text = "Full-Screen Top Edge",
					HeightRequest = 44,
					Margin = 0,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Start
				}
			}
		};

#if WINDOWS
		Loaded += OnLoaded;
#endif
	}

#if WINDOWS
	void OnLoaded(object sender, EventArgs e)
	{
		Loaded -= OnLoaded;

		if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window platformWindow)
			platformWindow.GetAppWindow()?.SetPresenter(AppWindowPresenterKind.FullScreen);
	}
#endif

	async void OnPushModalClicked(object sender, EventArgs e)
	{
		var modalMarker = new Label
		{
			AutomationId = "ModalPageMarker",
			Text = "This is the MODAL PAGE (Red)",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			TextColor = Colors.White
		};

		var modalRoot = new Grid
		{
			BackgroundColor = Colors.Red,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			Children =
			{
				new Button
				{
					AutomationId = "TopEdgeButton",
					Text = "Top Edge Button",
					HeightRequest = 44,
					Margin = 0,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Start
				}
			}
		};

		Grid.SetRow(modalMarker, 1);
		modalRoot.Children.Add(modalMarker);

		var modalPage = new ContentPage
		{
			BackgroundColor = Colors.Red,
			Content = modalRoot
		};
		NavigationPage.SetHasNavigationBar(modalPage, false);

		await Navigation.PushModalAsync(modalPage);
	}
}


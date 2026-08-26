namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30317, "Narrator reads App Window custom titlebar pane", PlatformAffected.UWP)]
public class Issue30317 : ContentPage
{
	readonly TitleBar _issueTitleBar = new()
	{
		Title = "TitleBar accessibility"
	};

	public Issue30317()
	{
		Content = new Grid
		{
			Padding = 24,
			Children =
			{
				new VerticalStackLayout
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Spacing = 20,
					MaximumWidthRequest = 640,
					Children =
					{
						new Label
						{
							AutomationId = "MainPageReady",
							FontSize = 24,
							HorizontalTextAlignment = TextAlignment.Center,
							Text = "Custom TitleBar sample ready"
						},
						new Label
						{
							HorizontalTextAlignment = TextAlignment.Center,
							Text = "Move Windows Narrator to the native minimize, maximize, and close buttons, then use Narrator key plus W."
						}
					}
				}
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		Window.TitleBar = _issueTitleBar;
	}
}


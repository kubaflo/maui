namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27168, "Unable to disable Android splash screen", PlatformAffected.Android)]
public class Issue27168 : ContentPage
{
	public Issue27168()
	{
		AutomationId = "Issue27168Page";

		var stateLabel = new Label
		{
			AutomationId = "Issue27168Status",
			FontAttributes = FontAttributes.Bold,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Page content ready"
		};

		Content = new VerticalStackLayout
		{
			AutomationId = "Issue27168Layout",
			Padding = 24,
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					AutomationId = "Issue27168Title",
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "Splash verification"
				},
				new Label
				{
					AutomationId = "Issue27168Description",
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "After restarting the app, confirm whether the Android splash screen appeared before this page."
				},
				stateLabel,
				new Button
				{
					AutomationId = "Issue27168ConfirmButton",
					Text = "Content action",
					Command = new Command(() => stateLabel.Text = "Content action completed")
				}
			}
		};
	}
}


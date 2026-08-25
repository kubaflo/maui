#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30317, "Narrator reads the custom title bar pane", PlatformAffected.UWP)]
public class Issue30317 : ContentPage
{
	public Issue30317()
	{
		var attachedStateLabel = new Label
		{
			Text = "Waiting for TitleBar attachment",
			AutomationId = "Issue30317AttachedState",
			FontSize = 18
		};

		var titleBar = new TitleBar
		{
			Title = "App Window"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Windows TitleBar accessibility",
					FontSize = 24
				},
				new Label
				{
					Text = "Inspect the caption-button region with Windows accessibility.",
					FontSize = 16
				},
				attachedStateLabel
			}
		};

		Loaded += (sender, args) =>
		{
			var currentWindow = Window;
			if (currentWindow is null)
			{
				attachedStateLabel.Text = "Window unavailable";
				return;
			}

			currentWindow.TitleBar = titleBar;
			attachedStateLabel.Text = "TitleBar attached";
		};
	}
}
#endif


#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 6369, "Shell bottom tabs and icons are not displayed correctly on Windows", PlatformAffected.UWP)]
public class Issue6369 : ContentPage
{
	public Issue6369()
	{
		var showShellButton = new Button
		{
			AutomationId = "ShowShellButton",
			Text = "Show Shell tabs"
		};
		showShellButton.Clicked += OnShowShellButtonClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					Text = "Issue 6369: Windows Shell tabs"
				},
				new Label { Text = "The Cats and Dogs tabs should appear at the bottom with their configured icons." },
				showShellButton
			}
		};
	}

	void OnShowShellButtonClicked(object sender, EventArgs e)
	{
		var reportedShell = new Shell();
		var catsTab = new Tab
		{
			Title = "Cats",
			Icon = "dotnet_bot.svg"
		};
		var dogsTab = new Tab
		{
			Title = "Dogs",
			Icon = "dotnet_bot.svg"
		};
		catsTab.Items.Add(new ShellContent
		{
			ContentTemplate = new DataTemplate(() => CreateTabContent("Cats"))
		});
		dogsTab.Items.Add(new ShellContent
		{
			ContentTemplate = new DataTemplate(() => CreateTabContent("Dogs"))
		});

		var tabBar = new TabBar();
		tabBar.Items.Add(catsTab);
		tabBar.Items.Add(dogsTab);
		reportedShell.Items.Add(tabBar);

		Window.Page = reportedShell;
	}

	static ContentPage CreateTabContent(string title)
	{
		return new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Children =
				{
					new Label
					{
						AutomationId = $"{title}Content",
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = $"{title} content"
					},
					new Label { Text = "The configured Cats and Dogs tabs should be at the bottom and display their icons." }
				}
			}
		};
	}
}
#endif

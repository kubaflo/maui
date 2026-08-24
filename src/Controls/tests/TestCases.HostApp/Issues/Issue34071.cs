namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "Shell foreground color is not applied to ToolbarItems", PlatformAffected.UWP)]
public class Issue34071 : Shell
{
	public Issue34071()
	{
		var purple = Colors.Purple;
		var loadedStatus = new Label
		{
			AutomationId = "Issue34071LoadedStatus",
			Text = "-1"
		};

		var referenceSwatch = new BoxView
		{
			AutomationId = "Issue34071PurpleReference",
			Color = purple,
			HeightRequest = 24,
			WidthRequest = 24
		};

		var page = new ContentPage
		{
			Title = "Home",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						FontSize = 22,
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Shell toolbar foreground color"
					},
					new HorizontalStackLayout
					{
						HorizontalOptions = LayoutOptions.Center,
						Spacing = 10,
						Children =
						{
							referenceSwatch,
							new Label
							{
								AutomationId = "Issue34071PurpleReferenceLabel",
								Text = "Expected toolbar icon color: Purple",
								VerticalTextAlignment = TextAlignment.Center
							}
						}
					},
					loadedStatus
				}
			}
		};

		page.ToolbarItems.Add(new ToolbarItem
		{
			AutomationId = "AffectedToolbarItem",
			IconImageSource = "shopping_cart.png",
			Text = "Calculator"
		});
		page.Loaded += (_, _) => loadedStatus.Text = "1";

		FlyoutBehavior = FlyoutBehavior.Disabled;
		Shell.SetForegroundColor(this, purple);
		Items.Add(new ShellContent
		{
			Content = page,
			Route = "Issue34071MainPage",
			Title = "Home"
		});
	}
}


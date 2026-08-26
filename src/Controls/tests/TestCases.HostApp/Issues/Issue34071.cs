namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "Shell foreground color is not applied to toolbar item icons", PlatformAffected.UWP)]
public class Issue34071 : Shell
{
	public Issue34071()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Shell.SetForegroundColor(this, Colors.Fuchsia);

		Items.Add(new ShellContent
		{
			ContentTemplate = new DataTemplate(() =>
			{
				var page = new ContentPage
				{
					Title = "Home",
					Content = new Label
					{
						Text = "The shopping-cart toolbar icon should be Fuchsia.",
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					}
				};

				page.ToolbarItems.Add(new ToolbarItem
				{
					AutomationId = "AffectedToolbarItem",
					IconImageSource = "shopping_cart.png",
					Order = ToolbarItemOrder.Primary
				});

				return page;
			})
		});
	}
}


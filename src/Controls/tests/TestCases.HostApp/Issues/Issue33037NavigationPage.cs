namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33037, "iOS large navigation title disappears after scrolling", PlatformAffected.iOS, issueTestNumber: 1)]
public class Issue33037NavigationPage : NavigationPage
{
	public Issue33037NavigationPage() : base(CreateContentPage())
	{
		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.NavigationPage.SetPrefersLargeTitles(this, true);
	}

	static ContentPage CreateContentPage()
	{
		var statusLabel = new Label
		{
			AutomationId = "ScrollStatus",
			Text = "Scroll offset below 100",
			FontAttributes = FontAttributes.Bold,
			BackgroundColor = Colors.White,
			Padding = 8
		};

		var itemsLayout = new VerticalStackLayout
		{
			Padding = new Thickness(20, 10),
			Spacing = 10,
			Children =
			{
				statusLabel,
				new Label
				{
					AutomationId = "PageTitle",
					Text = "Large Title Test Page",
					FontSize = 18,
					Margin = new Thickness(0, 10)
				}
			}
		};

		for (int i = 0; i < 30; i++)
		{
			itemsLayout.Children.Add(new Label
			{
				Text = $"Item {i}",
				Margin = new Thickness(0, 5)
			});
		}

		var scrollView = new ScrollView
		{
			AutomationId = "TestScrollView",
			Content = itemsLayout
		};

		scrollView.Scrolled += (sender, args) =>
		{
			statusLabel.TranslationY = args.ScrollY;

			if (args.ScrollY >= 100)
				statusLabel.Text = "Scroll offset reached 100";
		};

		var page = new ContentPage
		{
			Title = "Large Title Test",
			Content = scrollView
		};

		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetLargeTitleDisplay(
			page,
			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.LargeTitleDisplayMode.Always);

		return page;
	}
}

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30812, "Unnecessary More options button appears after applying resize", PlatformAffected.UWP)]
public class Issue30812 : ContentPage
{
	public Issue30812()
	{
		Title = "Developer Balance";

		var moreOptionsButton = new Button
		{
			AutomationId = "MoreOptionsButton",
			IsVisible = false,
			Text = "More options"
		};

		var insightsHeader = new Grid
		{
			ColumnSpacing = 12,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Auto }
			},
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 18,
					Text = "Developer balance insights",
					VerticalOptions = LayoutOptions.Center
				},
				moreOptionsButton
			}
		};
		Grid.SetColumn(moreOptionsButton, 1);

		var dashboardContainer = new VerticalStackLayout
		{
			AutomationId = "DashboardContainer",
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 28,
					Text = "Dashboard"
				},
				insightsHeader,
				CreateInsightCard("Work items", "12 active items"),
				CreateInsightCard("Pull requests", "4 awaiting review")
			}
		};

		var applyResizeButton = new Button
		{
			AutomationId = "ApplyResizeButton",
			Text = "Apply Resize"
		};
		applyResizeButton.Clicked += (_, _) =>
		{
			dashboardContainer.WidthRequest = 320;
			moreOptionsButton.IsVisible = true;
		};
		dashboardContainer.Children.Add(applyResizeButton);

		Content = new ScrollView
		{
			Content = dashboardContainer
		};
	}

	static Border CreateInsightCard(string heading, string detail)
	{
		return new Border
		{
			Padding = 16,
			Content = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						Text = heading
					},
					new Label { Text = detail }
				}
			}
		};
	}
}


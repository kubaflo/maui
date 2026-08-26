namespace Controls.TestCases.HostApp.Issues;

[Issue(IssueTracker.Github, 30812, "Resize exposes an unnecessary More options button", PlatformAffected.UWP)]
public class Issue30812 : NavigationPage
{
	public Issue30812() : base(new DashboardPage())
	{
	}

	sealed class DashboardPage : ContentPage
	{
		public DashboardPage()
		{
			Title = "Dashboard";

			var dashboardTitleLabel = new Label
			{
				Text = "Developer Balance Dashboard",
				AutomationId = "DashboardTitle",
				FontSize = 24,
				FontAttributes = FontAttributes.Bold
			};

			var instructionsLabel = new Label
			{
				Text = "Apply the accessibility Resize setting, then inspect the Dashboard toolbar.",
				AutomationId = "InstructionsLabel",
				FontSize = 16
			};

			var resizeStatusLabel = new Label
			{
				Text = "Resize not applied",
				AutomationId = "ResizeStatus",
				FontSize = 16
			};

			var actionStatusLabel = new Label
			{
				Text = "Insights action not activated",
				AutomationId = "ActionStatus",
				FontSize = 16
			};

			var insightsToolbarItem = new ToolbarItem
			{
				Text = "Insights",
				AutomationId = "InsightsToolbarItem",
				Order = ToolbarItemOrder.Primary
			};
			insightsToolbarItem.Clicked += (_, _) => actionStatusLabel.Text = "Insights action activated";
			ToolbarItems.Add(insightsToolbarItem);

			var applyResizeButton = new Button
			{
				Text = "Apply Resize",
				AutomationId = "ApplyResizeButton"
			};

			applyResizeButton.Clicked += (_, _) =>
			{
				dashboardTitleLabel.FontSize = 48;
				instructionsLabel.FontSize = 32;
				resizeStatusLabel.FontSize = 32;
				actionStatusLabel.FontSize = 32;
				resizeStatusLabel.Text = "Resize applied at 200 percent";

				ToolbarItems.Remove(insightsToolbarItem);
				insightsToolbarItem.Order = ToolbarItemOrder.Secondary;
				ToolbarItems.Add(insightsToolbarItem);
			};

			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						dashboardTitleLabel,
						instructionsLabel,
						resizeStatusLabel,
						actionStatusLabel,
						applyResizeButton
					}
				}
			};
		}
	}
}


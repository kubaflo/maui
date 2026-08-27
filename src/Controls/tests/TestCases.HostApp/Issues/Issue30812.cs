using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30812, "Unnecessary More options button appears after resizing", PlatformAffected.UWP)]
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

			ToolbarItems.Add(new ToolbarItem
			{
				Text = "Refresh",
				AutomationId = "MoreButton",
				Order = ToolbarItemOrder.Primary,
				Priority = 0
			});

			var resizeObservationLabel = new Label
			{
				Text = "Resize callback width: -1",
				AutomationId = "Issue30812ResizeObservation"
			};

			bool resizeRequested = false;
			SizeChanged += (_, _) =>
			{
				if (!resizeRequested)
					return;

				var dashboardWindow = Window;
				if (dashboardWindow is null)
				{
					resizeObservationLabel.Text = "Resize callback unavailable";
					return;
				}

				resizeObservationLabel.Text =
					$"Resize callback width: {dashboardWindow.Width.ToString(CultureInfo.InvariantCulture)}";
			};

			var resizeButton = new Button
			{
				Text = "Apply Resize",
				AutomationId = "Issue30812ResizeButton"
			};

			resizeButton.Clicked += (_, _) =>
			{
				resizeObservationLabel.Text = "Resize callback width: -1";
				resizeRequested = true;

				var dashboardWindow = Window;
				if (dashboardWindow is null)
				{
					resizeObservationLabel.Text = "Resize callback unavailable";
					return;
				}

				dashboardWindow.Width = 500;
			};

			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label
						{
							Text = "Developer Balance",
							FontSize = 28,
							FontAttributes = FontAttributes.Bold
						},
						new Border
						{
							Padding = 18,
							Content = new VerticalStackLayout
							{
								Spacing = 6,
								Children =
								{
									new Label { Text = "Available balance" },
									new Label
									{
										Text = "$12,480.00",
										FontSize = 24,
										FontAttributes = FontAttributes.Bold
									}
								}
							}
						},
						new Border
						{
							Padding = 18,
							Content = new VerticalStackLayout
							{
								Spacing = 6,
								Children =
								{
									new Label { Text = "Current month" },
									new Label
									{
										Text = "$3,240.00",
										FontSize = 24,
										FontAttributes = FontAttributes.Bold
									}
								}
							}
						},
						resizeButton,
						resizeObservationLabel
					}
				}
			};
		}
	}
}


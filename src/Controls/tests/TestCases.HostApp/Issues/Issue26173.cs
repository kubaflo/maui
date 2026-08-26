namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26173, "Fancy Sample Code Uses Copyrighted Fonts", PlatformAffected.iOS)]
public class Issue26173 : ContentPage
{
	public Issue26173()
	{
		Title = "Included sample content";

		var inspectionStatus = new Label
		{
			Text = "Inspection pending",
			AutomationId = "InspectionStatus",
			FontAttributes = FontAttributes.Bold
		};

		var inspectButton = new Button
		{
			Text = "Inspect included sample fonts",
			AutomationId = "InspectFontsButton"
		};

		inspectButton.Clicked += (_, _) => inspectionStatus.Text = "Inspection complete";

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Generated sample font resources",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Resources / Fonts",
						FontAttributes = FontAttributes.Bold
					},
					new VerticalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label
							{
								Text = "FluentSystemIcons-Regular.ttf",
								AutomationId = "FluentFontEntry"
							},
							new Label
							{
								Text = "SegoeUI-Semibold.ttf",
								AutomationId = "SegoeFontEntry"
							}
						}
					},
					inspectButton,
					inspectionStatus
				}
			}
		};
	}
}

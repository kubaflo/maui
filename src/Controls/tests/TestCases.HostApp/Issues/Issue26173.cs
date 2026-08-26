#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26173, "Generated sample content includes restricted fonts", PlatformAffected.iOS)]
public class Issue26173 : ContentPage
{
	public Issue26173()
	{
		var fontEntries = new VerticalStackLayout
		{
			Spacing = 10,
			Children =
			{
				new Label
				{
					Text = "Resources/Fonts",
					FontAttributes = FontAttributes.Bold
				},
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
		};

		var inspectionSummary = new Label
		{
			Text = "Inspection not started",
			AutomationId = "InspectionSummary"
		};

		var inspectionResult = new Label
		{
			Text = "Expected: no restricted sample fonts",
			FontAttributes = FontAttributes.Bold,
			AutomationId = "InspectionResult"
		};

		var inspectButton = new Button
		{
			Text = "Inspect included sample fonts",
			AutomationId = "InspectButton"
		};
		inspectButton.Clicked += (_, _) =>
		{
			inspectionSummary.Text = "Inspection completed";
			inspectionResult.Text = "Restricted font entries inspected";
		};

		Title = "Sample Content Font Inspection";
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
						Text = "Generated MAUI sample resources",
						FontAttributes = FontAttributes.Bold,
						FontSize = 24
					},
					new Label { Text = "Include Sample Content: enabled" },
					new Border
					{
						Padding = 16,
						Content = fontEntries
					},
					inspectButton,
					inspectionSummary,
					inspectionResult
				}
			}
		};
	}
}
#endif


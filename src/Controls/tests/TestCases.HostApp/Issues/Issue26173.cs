#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26173, "Sample content distributes restricted fonts", PlatformAffected.Android)]
public class Issue26173 : ContentPage
{
	public Issue26173()
	{
		var checkButton = new Button
		{
			AutomationId = "Issue26173CheckButton",
			Text = "Check generated font inventory"
		};
		checkButton.Clicked += (_, _) => checkButton.Text = "Inventory check requested";

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
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "Generated sample project"
					},
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 18,
						Text = "Resources/Fonts"
					},
					new Label
					{
						AutomationId = "Issue26173FluentFontEntry",
						Text = "FluentSystemIcons-Regular.ttf"
					},
					new Label
					{
						AutomationId = "Issue26173SegoeFontEntry",
						Text = "SegoeUI-Semibold.ttf"
					},
					checkButton
				}
			}
		};
	}
}
#endif


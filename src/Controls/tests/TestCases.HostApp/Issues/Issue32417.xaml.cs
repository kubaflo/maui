namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32417, "Shell templates are not applied dynamically at runtime", PlatformAffected.iOS)]
public partial class Issue32417 : Shell
{
	public Issue32417()
	{
		InitializeComponent();
		HomeContent.ContentTemplate = new DataTemplate(CreateMainPage);
	}

	ContentPage CreateMainPage()
	{
		var templatesAssignedMarker = new Label
		{
			AutomationId = "TemplatesAssignedMarker",
			Text = "Templates pending"
		};

		var openFlyoutButton = new Button
		{
			AutomationId = "OpenFlyoutButton",
			Text = "Open Flyout"
		};
		openFlyoutButton.Clicked += (_, _) => FlyoutIsPresented = true;

		var applyTemplatesButton = new Button
		{
			AutomationId = "ApplyTemplatesButton",
			Text = "Apply Replacement Templates"
		};
		applyTemplatesButton.Clicked += (_, _) =>
		{
			ItemTemplate = CreateTemplate("ReplacementItemTemplateVisual", Colors.DarkBlue, "Title");
			MenuItemTemplate = CreateTemplate("ReplacementMenuItemTemplateVisual", Colors.DarkRed, "Text");
			templatesAssignedMarker.Text = "TemplatesAssigned";
		};

		return new ContentPage
		{
			Title = "Template Test",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontSize = 18,
						Text = "Open the flyout before and after replacing its templates."
					},
					openFlyoutButton,
					applyTemplatesButton,
					templatesAssignedMarker
				}
			}
		};
	}

	static DataTemplate CreateTemplate(string automationId, Color textColor, string bindingPath)
	{
		return new DataTemplate(() =>
		{
			var label = new Label
			{
				AutomationId = automationId,
				FontAttributes = FontAttributes.Bold,
				Padding = new Thickness(20, 12),
				TextColor = textColor
			};
			label.SetBinding(Label.TextProperty, bindingPath);
			return label;
		});
	}
}

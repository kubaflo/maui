namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32417, "Shell ItemTemplate and MenuItemTemplate are not applied dynamically at runtime", PlatformAffected.iOS)]
public class Issue32417 : Shell
{
	public Issue32417()
	{
		ItemTemplate = CreateItemTemplate("OldItemTemplate", "OLD ITEM TEMPLATE");
		MenuItemTemplate = CreateMenuItemTemplate("OldMenuTemplate", "OLD MENU TEMPLATE");

		var flyoutItem = new FlyoutItem
		{
			Title = "Sandbox",
			Items =
			{
				new ShellContent
				{
					Title = "Sandbox",
					Route = "Issue32417MainPage",
					ContentTemplate = new DataTemplate(CreateContentPage)
				}
			}
		};

		Items.Add(flyoutItem);
		Items.Add(new MenuItem { Text = "Shell Menu Item" });
	}

	ContentPage CreateContentPage()
	{
		var updateStatus = new Label
		{
			AutomationId = "TemplateUpdateStatus",
			Text = "Templates not applied"
		};

		var applyTemplatesButton = new Button
		{
			AutomationId = "ApplyTemplatesButton",
			Text = "Apply New Templates"
		};

		applyTemplatesButton.Clicked += (_, _) =>
		{
			ItemTemplate = CreateItemTemplate("NewItemTemplate", "NEW ITEM TEMPLATE");
			MenuItemTemplate = CreateMenuItemTemplate("NewMenuTemplate", "NEW MENU TEMPLATE");
			updateStatus.Text = "Templates applied";
		};

		return new ContentPage
		{
			Title = "Sandbox",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					updateStatus,
					applyTemplatesButton
				}
			}
		};
	}

	static DataTemplate CreateItemTemplate(string automationId, string text)
	{
		return new DataTemplate(() => new Label
		{
			AutomationId = automationId,
			Text = text,
			FontAttributes = FontAttributes.Bold,
			Padding = new Thickness(20, 12)
		});
	}

	static DataTemplate CreateMenuItemTemplate(string automationId, string text)
	{
		return new DataTemplate(() => new Label
		{
			AutomationId = automationId,
			Text = text,
			FontAttributes = FontAttributes.Italic,
			Padding = new Thickness(20, 12)
		});
	}
}


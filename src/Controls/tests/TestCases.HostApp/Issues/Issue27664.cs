namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27664, "Editor does not resize when the iOS keyboard appears", PlatformAffected.iOS)]
public class Issue27664 : ContentPage
{
	public Issue27664()
	{
		Title = "Editor keyboard resize";

		var instructions = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new Label
				{
					Text = "Focus the Editor and enter enough text to wrap across multiple lines."
				},
				new Label
				{
					Text = "The Editor should resize while the keyboard is visible."
				},
				new Button
				{
					Text = "Check editor resize"
				}
			}
		};

		var description = new Label
		{
			Text = "The Editor should become shorter while the keyboard is visible."
		};

		var editor = new Editor
		{
			AutomationId = "IssueEditor",
			VerticalOptions = LayoutOptions.Fill
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		grid.Add(instructions, 0, 0);
		grid.Add(description, 0, 1);
		grid.Add(editor, 0, 2);
		Content = grid;
	}
}


#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27664, "Editor does not resize above the iOS keyboard", PlatformAffected.iOS)]
public class Issue27664 : ContentPage
{
	public Issue27664()
	{
		var instructionLabel = new Label
		{
			Text = "Tap the Editor and enter enough text to wrap onto multiple lines."
		};

		var resultButton = new Button
		{
			AutomationId = "Issue27664Result",
			Text = "Waiting for closing token"
		};

		var editor = new Editor
		{
			AutomationId = "Issue27664Editor",
			Placeholder = "Enter multiple lines of text"
		};

		editor.TextChanged += (_, e) =>
		{
			if (e.NewTextValue?.EndsWith("focused", StringComparison.Ordinal) == true)
				resultButton.Text = "Input completed: focused";
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 12,
			RowDefinitions = new RowDefinitionCollection
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		grid.Add(instructionLabel);
		grid.Add(resultButton);
		grid.Add(editor);
		Grid.SetRow(resultButton, 1);
		Grid.SetRow(editor, 2);

		Content = grid;
	}
}
#endif


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "[iOS] Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	public Issue36412()
	{
		var instructionsLabel = new Label
		{
			AutomationId = "InstructionsLabel",
			Text = "Tap Field 1, then Field 7."
		};

		var fields = new VerticalStackLayout
		{
			Spacing = 24
		};

		for (int i = 1; i <= 15; i++)
		{
			fields.Add(new Entry
			{
				AutomationId = $"Field{i}",
				Keyboard = Keyboard.Numeric,
				Placeholder = $"Field {i}"
			});
		}

		var scrollView = new ScrollView
		{
			Content = fields
		};
		Grid.SetRow(scrollView, 1);

		Content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				instructionsLabel,
				scrollView
			}
		};
	}
}


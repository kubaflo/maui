namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	public Issue36412()
	{
		var titleLabel = new Label
		{
			Text = "Numeric fields",
			FontAttributes = FontAttributes.Bold
		};
		var instructionsLabel = new Label
		{
			Text = "Tap Field 1, then Field 8"
		};
		var headerButton = new Button
		{
			Text = "Form"
		};

		var header = new Grid
		{
			Padding = 12,
			ColumnDefinitions =
			[
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			],
			RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			]
		};
		header.Add(titleLabel, 0, 0);
		header.Add(instructionsLabel, 0, 1);
		header.Add(headerButton, 1, 0);
		Grid.SetRowSpan(headerButton, 2);

		var fields = new VerticalStackLayout
		{
			Padding = new Thickness(12, 0),
			Spacing = 8
		};

		for (var fieldNumber = 1; fieldNumber <= 15; fieldNumber++)
		{
			var fieldName = $"Field {fieldNumber}";
			var entry = new Entry
			{
				AutomationId = $"Field{fieldNumber}",
				Placeholder = fieldName,
				Keyboard = Keyboard.Numeric
			};
			fields.Add(entry);
		}

		var layout = new Grid
		{
			RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			]
		};
		layout.Add(header, 0, 0);
		layout.Add(new ScrollView { Content = fields }, 0, 1);
		Content = layout;
	}
}


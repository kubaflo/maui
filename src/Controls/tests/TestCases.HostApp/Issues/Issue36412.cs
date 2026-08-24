namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "[iOS] Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	public Issue36412()
	{
		Title = "Numeric entry focus";

		var focusMarker = new Label
		{
			AutomationId = "FocusMarker",
			FontSize = 12,
			LineBreakMode = LineBreakMode.TailTruncation,
			Text = "Focus: none",
			VerticalTextAlignment = TextAlignment.Center
		};

		var checkFocusButton = new Button
		{
			AutomationId = "CheckFocusButton",
			Text = "Check focus"
		};

		var header = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			ColumnSpacing = 8
		};
		header.Add(focusMarker);
		header.Add(checkFocusButton, 1);

		var fields = new VerticalStackLayout
		{
			Spacing = 16
		};

		for (int i = 1; i <= 15; i++)
		{
			string fieldName = $"Field {i}";
			var entry = new Entry
			{
				AutomationId = $"Field{i}",
				Keyboard = Keyboard.Numeric,
				Placeholder = fieldName
			};

			if (i is 1 or 8)
				entry.Focused += (sender, args) => focusMarker.Text = $"Focus: {fieldName}";

			fields.Children.Add(entry);
		}

		var rootGrid = new Grid
		{
			AutomationId = "RootGrid",
			Padding = new Thickness(16, 12),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 8
		};
		rootGrid.Add(header);
		rootGrid.Add(new ScrollView { Content = fields }, 0, 1);

		Content = rootGrid;
	}
}


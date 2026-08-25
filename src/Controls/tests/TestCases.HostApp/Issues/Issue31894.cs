namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31894, "Entry selects all text when clicking left of end-aligned text", PlatformAffected.WinRT)]
public class Issue31894 : ContentPage
{
	public Issue31894()
	{
		var instructionLabel = new Label
		{
			Text = "Tap the wide Entry to the left of its end-aligned text, then check its selection.",
			FontSize = 18
		};
		var entry = new Entry
		{
			AutomationId = "Issue31894Entry",
			HorizontalOptions = LayoutOptions.Fill,
			HorizontalTextAlignment = TextAlignment.End,
			Text = "End aligned text"
		};
		var selectionLengthLabel = new Label
		{
			AutomationId = "Issue31894SelectionLength",
			FontAttributes = FontAttributes.Bold,
			Text = "Selection length: -1"
		};

#if WINDOWS
		entry.HandlerChanged += (_, _) =>
		{
			if (entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
			{
				textBox.SelectionChanged += (_, _) =>
					selectionLengthLabel.Text = $"Selection length: {entry.SelectionLength}";
			}
		};
#endif

		Grid.SetRow(instructionLabel, 0);
		Grid.SetRow(entry, 1);
		Grid.SetRow(selectionLengthLabel, 2);

		Content = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto }
			},
			RowSpacing = 16,
			Children =
			{
				instructionLabel,
				entry,
				selectionLengthLabel
			}
		};
	}
}


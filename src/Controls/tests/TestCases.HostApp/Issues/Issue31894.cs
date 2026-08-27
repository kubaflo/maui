namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31894, "Entry selects all text when clicking left of end-aligned text", PlatformAffected.UWP)]
public class Issue31894 : ContentPage
{
	public Issue31894()
	{
		var diagnosticLabel = new Label
		{
			AutomationId = "DiagnosticLabel",
			FontSize = 18,
			Text = "Pending: IsFocused=False; Focused=-1; SelectionLength=-1"
		};

		var alignedEntry = new Entry
		{
			AutomationId = "AlignedEntry",
			HorizontalTextAlignment = TextAlignment.End,
			Text = "Selection test text"
		};

		var focusedEventCount = 0;

		alignedEntry.Loaded += (_, _) =>
		{
			diagnosticLabel.Text = $"Ready: IsFocused={alignedEntry.IsFocused}; Focused=-1; SelectionLength=-1";
		};

		alignedEntry.Focused += (_, _) =>
		{
			focusedEventCount++;
			diagnosticLabel.Text = $"Focused: IsFocused={alignedEntry.IsFocused}; Focused={focusedEventCount}; SelectionLength=-1";
		};

		alignedEntry.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == Entry.SelectionLengthProperty.PropertyName && alignedEntry.IsFocused)
				diagnosticLabel.Text = $"Sampled: IsFocused={alignedEntry.IsFocused}; Focused={focusedEventCount}; SelectionLength={alignedEntry.SelectionLength}";
		};

		Content = new VerticalStackLayout
		{
			Padding = 30,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					FontSize = 18,
					Text = "Tap the empty area to the left of the end-aligned text."
				},
				alignedEntry,
				diagnosticLabel
			}
		};
	}
}


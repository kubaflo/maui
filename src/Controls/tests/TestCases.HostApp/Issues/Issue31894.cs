namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31894, "Entry selects all text when tapping left of end-aligned text", PlatformAffected.UWP)]
public class Issue31894 : ContentPage
{
	public Issue31894()
	{
		var focusStatus = new Label
		{
			AutomationId = "FocusStatus",
			Text = "FocusCount=0"
		};

		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "Text=Sample entry text; Alignment=End; IsFocused=False; SelectionLength=0"
		};

		var affectedEntry = new Entry
		{
			AutomationId = "AffectedEntry",
			HorizontalTextAlignment = TextAlignment.End,
			Text = "Sample entry text"
		};

		var focusCount = 0;
		affectedEntry.Focused += (sender, args) =>
		{
			focusCount++;
			focusStatus.Text = $"FocusCount={focusCount}";
			UpdateResult();
		};
		affectedEntry.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == Entry.SelectionLengthProperty.PropertyName)
				UpdateResult();
		};

		void UpdateResult()
		{
			resultLabel.Text = $"Text={affectedEntry.Text}; Alignment={affectedEntry.HorizontalTextAlignment}; IsFocused={affectedEntry.IsFocused}; SelectionLength={affectedEntry.SelectionLength}";
		}

		var grid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 16
		};

		grid.Add(new Label
		{
			Text = "Tap inside the Entry, to the left of its end-aligned text."
		}, 0, 0);
		grid.Add(affectedEntry, 0, 1);
		grid.Add(focusStatus, 0, 2);
		grid.Add(resultLabel, 0, 3);

		Content = grid;
	}
}


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36505, "[iOS] Span tap hitbox is displaced for wrapped formatted text", PlatformAffected.iOS)]
public class Issue36505 : ContentPage
{
	public Issue36505()
	{
		var tapCount = 0;
		var resultLabel = new Label
		{
			AutomationId = "Issue36505Result",
			Text = "0",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18
		};

		var linkSpan = new Span
		{
			Text = "TAP LINK NOW",
			BackgroundColor = Colors.Yellow,
			TextColor = Colors.Blue,
			TextDecorations = TextDecorations.Underline
		};
		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (sender, args) =>
		{
			tapCount++;
			resultLabel.Text = tapCount.ToString();
			linkSpan.Text = "TAPPED ONCE";
			linkSpan.BackgroundColor = Colors.Lime;
		};
		linkSpan.GestureRecognizers.Add(tapGesture);

		var formattedText = new FormattedString();
		formattedText.Spans.Add(new Span
		{
			Text = "Read these opening words carefully because they wrap over several full lines before the interactive link. "
		});
		formattedText.Spans.Add(linkSpan);
		formattedText.Spans.Add(new Span
		{
			Text = " Then continue reading this ordinary trailing text after the link for several more wrapped lines."
		});

		var affectedLabel = new Label
		{
			AutomationId = "Issue36505AffectedLabel",
			WidthRequest = 280,
			Padding = new Thickness(0, 0, 0, 18),
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			FontFamily = "Segoe UI Bold",
			FontSize = 32,
			LineBreakMode = LineBreakMode.WordWrap,
			FormattedText = formattedText
		};

		var grid = new Grid
		{
			Padding = 24,
			RowSpacing = 12
		};
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

		var instructionLabel = new Label
		{
			Text = "Tap the single yellow link line centered in the text, then check the result.",
			FontSize = 16
		};
		var checkButton = new Button
		{
			AutomationId = "Issue36505Check",
			Text = "Check visible link tap"
		};

		grid.Add(instructionLabel);
		grid.Add(resultLabel, 0, 1);
		grid.Add(affectedLabel, 0, 2);
		grid.Add(checkButton, 0, 3);
		Content = grid;
	}
}

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37013, "FormattedString span tap target is vertically offset on iOS", PlatformAffected.iOS)]
public class Issue37013 : ContentPage
{
	const string LeadingText = "Paragraph one contains ordinary text that wraps naturally across several lines in this constrained label.\n\nParagraph two adds more words before the interactive span so layout differences can accumulate vertically.\n\nParagraph three continues the platform-default formatted text with no explicit font or line-height styling.\n\nParagraph four provides another naturally wrapped block before the link that must be tapped on its visible glyphs.\n\nParagraph five keeps the same Label and FormattedString hierarchy while increasing the rendered line count.\n\nParagraph six supplies additional wrapping content needed to expose a progressively displaced late-span hit region.\n\nParagraph seven appears near the bottom of the ordinary text that precedes the interactive span.\n\nParagraph eight completes more than twenty rendered lines before the visible link.\n";
	const string LinkText = "Click here for details";
	const string TrailingText = "\nParagraph one contains ordinary text that wraps naturally across several lines in this constrained label.\n\nParagraph two adds more words before the interactive span so layout differences can accumulate vertically.\n\nParagraph three continues the platform-default formatted text with no explicit font or line-height styling.\n\nParagraph four provides another naturally wrapped block before the link that must be tapped on its visible glyphs.\n\nParagraph five keeps the same Label and FormattedString hierarchy while increasing the rendered line count.\n\nParagraph six supplies additional wrapping content needed to expose a progressively displaced late-span hit region.\n\nParagraph seven appears near the bottom of the ordinary text that precedes the interactive span.\n\nParagraph eight completes more than twenty rendered lines before the visible link.";

	readonly Label _tapStateLabel;
	readonly Label _checkStateLabel;

	public Issue37013()
	{
		_tapStateLabel = new Label
		{
			AutomationId = "TapStateLabel",
			Text = "NOT_TAPPED"
		};

		_checkStateLabel = new Label
		{
			AutomationId = "CheckStateLabel",
			Text = "CHECK:-1"
		};

		var layoutStateLabel = new Label
		{
			AutomationId = "LayoutStateLabel",
			Text = "LAYOUT:-1"
		};

		var checkButton = new Button
		{
			AutomationId = "CheckResultButton",
			Text = "Check visible span tap"
		};
		checkButton.Clicked += OnCheckResultClicked;

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += OnLinkTapped;

		var linkSpan = new Span
		{
			Text = LinkText,
			TextColor = Colors.Blue,
			TextDecorations = TextDecorations.Underline
		};
		linkSpan.GestureRecognizers.Add(tapGestureRecognizer);

		var formattedString = new FormattedString();
		formattedString.Spans.Add(new Span { Text = LeadingText });
		formattedString.Spans.Add(linkSpan);
		formattedString.Spans.Add(new Span { Text = TrailingText });

		var affectedLabel = new Label
		{
			AutomationId = "AffectedFormattedLabel",
			FormattedText = formattedString,
			HorizontalOptions = LayoutOptions.Center,
			WidthRequest = 300
		};
		affectedLabel.SizeChanged += (_, _) =>
		{
			if (affectedLabel.Width > 0 && affectedLabel.Height > 0)
				layoutStateLabel.Text = $"LAYOUT:{affectedLabel.Width:F0}x{affectedLabel.Height:F0}";
		};

		Content = new VerticalStackLayout
		{
			Padding = 12,
			Spacing = 6,
			Children =
			{
				_tapStateLabel,
				checkButton,
				affectedLabel,
				_checkStateLabel,
				layoutStateLabel
			}
		};
	}

	void OnLinkTapped(object sender, TappedEventArgs e)
	{
		_tapStateLabel.Text = "TAPPED";
	}

	void OnCheckResultClicked(object sender, EventArgs e)
	{
		_checkStateLabel.Text = "CHECK:COMPLETED";
	}
}

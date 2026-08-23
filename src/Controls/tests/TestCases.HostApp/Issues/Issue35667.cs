namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35667, "TextTransform.Uppercase does not work on Shell SearchHandler", PlatformAffected.iOS)]
public class Issue35667 : Shell
{
	public Issue35667()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		var contentPage = new ContentPage
		{
			Title = "Search"
		};

		var configurationLabel = new Label
		{
			AutomationId = "Issue35667Configuration",
			HorizontalTextAlignment = TextAlignment.Center
		};

		var queryChangedLabel = new Label
		{
			AutomationId = "Issue35667QueryChanged",
			Text = "QUERY_NOT_CHANGED",
			HorizontalTextAlignment = TextAlignment.Center
		};

		SearchHandler searchHandler = new SearchHandler
		{
			AutomationId = "Issue35667SearchHandler",
			Placeholder = "Type lowercase text",
			SearchBoxVisibility = SearchBoxVisibility.Expanded,
			TextTransform = TextTransform.Uppercase
		};

		configurationLabel.Text = $"{searchHandler.TextTransform}|{searchHandler.SearchBoxVisibility}";
		searchHandler.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(SearchHandler.Query) && !string.IsNullOrEmpty(searchHandler.Query))
				queryChangedLabel.Text = "QUERY_CHANGED";
		};

		Shell.SetSearchHandler(contentPage, searchHandler);

		contentPage.Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Expected visible search text: MAUI",
					HorizontalTextAlignment = TextAlignment.Center
				},
				configurationLabel,
				queryChangedLabel
			}
		};

		Items.Add(new ShellContent
		{
			Title = "Search",
			Content = contentPage
		});
	}
}


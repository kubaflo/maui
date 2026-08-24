namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35667, "TextTransform.Uppercase does not work on Shell SearchHandler", PlatformAffected.iOS)]
public class Issue35667 : Shell
{
	public Issue35667()
	{
		var observedQueryLabel = new Label
		{
			AutomationId = "Issue35667ObservedQuery",
			Text = "Query event not received"
		};

		var searchHandler = new SearchHandler
		{
			AutomationId = "Issue35667SearchHandler",
			Placeholder = "Type lowercase text",
			SearchBoxVisibility = SearchBoxVisibility.Expanded,
			TextTransform = TextTransform.Uppercase
		};

		searchHandler.PropertyChanged += (_, args) =>
		{
			var query = searchHandler.Query;
			if (args.PropertyName == SearchHandler.QueryProperty.PropertyName &&
				!string.IsNullOrEmpty(query))
			{
				observedQueryLabel.Text = query;
			}
		};

		var contentPage = new ContentPage
		{
			Title = "Search transform",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Shell SearchHandler TextTransform.Uppercase",
						FontAttributes = FontAttributes.Bold,
						FontSize = 20
					},
					new Label
					{
						Text = "Type maui in the search box. The displayed text should become MAUI."
					},
					observedQueryLabel,
					new Label
					{
						AutomationId = "Issue35667Configuration",
						Text = "Configured: TextTransform.Uppercase",
						FontAttributes = FontAttributes.Bold
					}
				}
			}
		};

		SetSearchHandler(contentPage, searchHandler);
		Items.Add(new ShellContent
		{
			Content = contentPage
		});
	}
}


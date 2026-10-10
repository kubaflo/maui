namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35667, "TextTransform.Uppercase does not work on Shell SearchHandler", PlatformAffected.iOS)]
public class Issue35667 : TestShell
{
	protected override void Init()
	{
		Items.Add(new ShellContent
		{
			Title = "SearchHandler TextTransform",
			ContentTemplate = new DataTemplate(() =>
			{
				var queryStatusLabel = new Label
				{
					AutomationId = "Issue35667QueryStatus",
					FontAttributes = FontAttributes.Bold,
					FontSize = 18,
					Text = "QUERY_NOT_OBSERVED"
				};

				var searchHandler = new SearchHandler
				{
					AutomationId = "Issue35667SearchHandler",
					Placeholder = "Type lowercase text",
					TextTransform = TextTransform.Uppercase
				};

				searchHandler.PropertyChanged += (_, args) =>
				{
					var query = searchHandler.Query;
					if (args.PropertyName == SearchHandler.QueryProperty.PropertyName &&
						!string.IsNullOrEmpty(query))
					{
						queryStatusLabel.Text = $"QUERY_OBSERVED:{query}";
					}
				};

				var page = new ContentPage
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label
							{
								AutomationId = "Issue35667Ready",
								FontSize = 18,
								Text = $"TextTransform={searchHandler.TextTransform}. Type lowercase text into the Shell SearchHandler."
							},
							queryStatusLabel
						}
					}
				};

				Shell.SetSearchHandler(page, searchHandler);
				return page;
			})
		});
	}
}


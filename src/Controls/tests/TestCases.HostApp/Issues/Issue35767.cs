#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35767, "SearchHandler.ShowsResults does not work correctly", PlatformAffected.UWP)]
public class Issue35767 : TestShell
{
	protected override void Init()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		var page = new ContentPage
		{
			Title = "Issue 35767"
		};

		var searchHandler = new Issue35767SearchHandler
		{
			AutomationId = "Issue35767SearchHandler",
			Placeholder = "Search issue 35767",
			ShowsResults = true,
			ItemTemplate = new DataTemplate(() =>
			{
				var resultLabel = new Label
				{
					Padding = 12,
					FontSize = 18
				};
				resultLabel.SetBinding(Label.TextProperty, ".");
				return resultLabel;
			})
		};

		var showsResultsState = new Label
		{
			AutomationId = "ShowsResultsState",
			Text = "ShowsResults: True"
		};

		var disableResultsButton = new Button
		{
			AutomationId = "DisableResultsButton",
			Text = "ShowsResults = False"
		};
		disableResultsButton.Clicked += (_, _) =>
		{
			searchHandler.ShowsResults = false;
			showsResultsState.Text = "ShowsResults: False";
		};

		Shell.SetSearchHandler(page, searchHandler);
		page.Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 22,
					Text = "Issue 35767 SearchHandler reproduction"
				},
				new Label
				{
					Text = "Search for Alpha, disable ShowsResults, then search for Beta."
				},
				showsResultsState,
				disableResultsButton
			}
		};

		Items.Add(new ShellContent
		{
			Title = "Search",
			Content = page
		});
	}
}

public sealed class Issue35767SearchHandler : SearchHandler
{
	protected override void OnQueryChanged(string oldValue, string newValue)
	{
		base.OnQueryChanged(oldValue, newValue);

		if (string.IsNullOrEmpty(newValue))
			ItemsSource = Array.Empty<string>();
		else if (newValue.Contains("Alpha", StringComparison.OrdinalIgnoreCase))
			ItemsSource = new[] { "Alpha result" };
		else if (newValue.Contains("Beta", StringComparison.OrdinalIgnoreCase))
			ItemsSource = new[] { "Beta result" };
		else
			ItemsSource = Array.Empty<string>();
	}
}
#endif


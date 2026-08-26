namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35767, "SearchHandler.ShowsResults does not work correctly", PlatformAffected.UWP)]
public partial class Issue35767 : Shell
{
	const string SearchResult = "Issue 35767 result";

	public Issue35767()
	{
		InitializeComponent();
		IssueSearchHandler.ItemsSource = new[] { SearchResult };
		IssueSearchHandler.QueryObserved = query => QueryObservedLabel.Text = query;
	}

	void OnDisableResultsClicked(object sender, EventArgs e)
	{
		IssueSearchHandler.ShowsResults = false;
		ShowsResultsStateLabel.Text = "ShowsResults: False";
	}
}

public sealed class Issue35767SearchHandler : SearchHandler
{
	public Action<string> QueryObserved { get; set; } = _ => { };

	protected override void OnQueryChanged(string oldValue, string newValue)
	{
		base.OnQueryChanged(oldValue, newValue);
		QueryObserved(newValue);
	}
}

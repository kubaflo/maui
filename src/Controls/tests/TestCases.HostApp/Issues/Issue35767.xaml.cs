namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35767, "SearchHandler.ShowsResults does not work correctly", PlatformAffected.UWP)]
public partial class Issue35767 : Shell
{
	public Issue35767()
	{
		InitializeComponent();
		IssueSearchHandler.QueryObserved += OnQueryObserved;
	}

	void OnDisableResultsClicked(object sender, EventArgs e)
	{
		IssueSearchHandler.ShowsResults = false;
		PropertyStateLabel.Text = $"ShowsResults: {IssueSearchHandler.ShowsResults}";
	}

	void OnQueryObserved(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(() =>
		{
			QueryStateLabel.Text = $"Query: {IssueSearchHandler.Query}";
			SourceStateLabel.Text = $"Source: {IssueSearchHandler.LastResult}";
		});
	}
}

public sealed class Issue35767SearchHandler : SearchHandler
{
	public event EventHandler QueryObserved;

	public string LastResult { get; private set; } = "<none>";

	protected override void OnQueryChanged(string oldValue, string newValue)
	{
		base.OnQueryChanged(oldValue, newValue);

		LastResult = newValue switch
		{
			"alpha" => "alpha result",
			"beta" => "beta result",
			_ => "<none>",
		};

		ItemsSource = LastResult == "<none>" ? Array.Empty<string>() : new[] { LastResult };
		QueryObserved?.Invoke(this, EventArgs.Empty);
	}
}

using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35767, "SearchHandler.ShowsResults does not work correctly", PlatformAffected.UWP)]
public partial class Issue35767 : Shell
{
	bool _showsResultsWasTrue;

	public Issue35767()
	{
		InitializeComponent();

		IssueSearchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;
		IssueSearchHandler.QueryProcessed += OnQueryProcessed;
		IssueSearchHandler.ItemSelected += OnItemSelected;
	}

	void OnDisableResultsClicked(object sender, EventArgs e)
	{
		_showsResultsWasTrue = IssueSearchHandler.ShowsResults;
		IssueSearchHandler.ShowsResults = false;
	}

	void OnSearchHandlerPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (ReferenceEquals(sender, IssueSearchHandler) &&
			e.PropertyName == nameof(SearchHandler.ShowsResults) &&
			_showsResultsWasTrue &&
			!IssueSearchHandler.ShowsResults)
		{
			TransitionStatusLabel.Text = "ShowsResults transition: True to False";
			DisableResultsButton.Text = "ShowsResults transition: True to False";
		}
	}

	void OnQueryProcessed(object sender, string query)
	{
		QueryStatusLabel.Text = string.Equals(query, "beta", StringComparison.OrdinalIgnoreCase) &&
			!IssueSearchHandler.ShowsResults
				? "Beta query processed; no result selected"
				: $"Query processed: {query}";
	}

	void OnItemSelected(object sender, object item)
	{
		if (!IssueSearchHandler.ShowsResults &&
			string.Equals(item as string, "Beta Result", StringComparison.Ordinal))
		{
			QueryStatusLabel.Text = "Beta result selected while ShowsResults was false";
		}
	}
}

public sealed class Issue35767Page : ContentPage
{
}

public sealed class Issue35767SearchHandler : SearchHandler
{
	public event EventHandler<string> QueryProcessed;
	public event EventHandler<object> ItemSelected;

	protected override void OnQueryChanged(string oldValue, string newValue)
	{
		base.OnQueryChanged(oldValue, newValue);

		if (string.Equals(newValue, "alpha", StringComparison.OrdinalIgnoreCase))
			ItemsSource = new[] { "Alpha Result" };
		else if (string.Equals(newValue, "beta", StringComparison.OrdinalIgnoreCase))
			ItemsSource = new[] { "Beta Result" };
		else
			ItemsSource = Array.Empty<string>();

		QueryProcessed?.Invoke(this, newValue);
	}

	protected override void OnItemSelected(object item)
	{
		base.OnItemSelected(item);
		ItemSelected?.Invoke(this, item);
	}
}

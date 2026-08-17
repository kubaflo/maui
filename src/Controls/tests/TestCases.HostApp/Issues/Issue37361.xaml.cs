using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37361, "RefreshView pull-to-refresh does nothing when CollectionView is empty", PlatformAffected.iOS)]
public partial class Issue37361 : ContentPage
{
	readonly ObservableCollection<string> _items = [];
	int _refreshCount;

	public Issue37361()
	{
		InitializeComponent();

		EmptyCollection.ItemsSource = _items;
		RefreshControl.Command = new Command(OnRefresh);
	}

	void OnRefresh()
	{
		_refreshCount++;
		RefreshCountLabel.Text = $"Refreshes: {_refreshCount}";
		RefreshControl.IsRefreshing = false;
	}

	void OnCheckEmptyRefreshClicked(object sender, EventArgs e)
	{
		StatusLabel.Text = "Check completed";
	}
}

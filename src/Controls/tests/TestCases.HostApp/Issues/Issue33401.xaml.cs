namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33401, "CollectionView SelectionChanged is not raised inside a Grid with a TapGestureRecognizer", PlatformAffected.iOS)]
public partial class Issue33401 : ContentPage
{
	public Issue33401()
	{
		InitializeComponent();
		IssueCollectionView.ItemsSource = new string[] { "Issue 33401 item" };
	}

	void OnContainerTapped(object sender, TappedEventArgs e)
	{
		TapStatus.Text = "Grid tap received.";
	}

	void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SelectionStatus.Text = "SelectionChanged received.";
	}
}

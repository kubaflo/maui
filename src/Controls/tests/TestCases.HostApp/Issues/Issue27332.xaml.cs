using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27332, "CollectionView footer is displayed at the bottom of the page", PlatformAffected.UWP)]
public partial class Issue27332 : ContentPage
{
	readonly ObservableCollection<string> issueItems = [];
	int nextItemNumber = 1;
	int resetCount;

	public Issue27332()
	{
		InitializeComponent();
		IssueCollectionView.ItemsSource = issueItems;
		issueItems.CollectionChanged += OnItemsCollectionChanged;
	}

	void OnAddItemsClicked(object sender, EventArgs e)
	{
		for (int i = 0; i < 2; i++)
			issueItems.Add($"Item {nextItemNumber++}");
	}

	void OnClearItemsClicked(object sender, EventArgs e)
	{
		issueItems.Clear();
		nextItemNumber = 1;
	}

	void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			resetCount++;
			Dispatcher.Dispatch(() => ResetStatusLabel.Text = $"Reset:{resetCount}");
		}
	}
}

using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27332, "CollectionView footer is displayed at the bottom after items are added and cleared", PlatformAffected.WinPhone)]
public partial class Issue27332 : ContentPage
{
	readonly ObservableCollection<string> _items = [];
	int _itemNumber = 1;

	public Issue27332()
	{
		InitializeComponent();
		TestCollectionView.ItemsSource = _items;
	}

	void OnAddClicked(object sender, EventArgs e)
	{
		for (int i = 0; i < 2; i++)
			_items.Add($"Item {_itemNumber++}");

		ResultLabel.Text = $"Count: {_items.Count}";
	}

	void OnClearClicked(object sender, EventArgs e)
	{
		_items.Clear();
		_itemNumber = 1;
		ResultLabel.Text = $"Count: {_items.Count}";
	}
}

using System.Collections.ObjectModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public ObservableCollection<string> Items { get; set; }
	private int _tapCount = 0;

	public MainPage()
	{
		InitializeComponent();

		Items = new ObservableCollection<string>
		{
			"Item 1",
			"Item 2",
			"Item 3",
			"Item 4",
			"Item 5"
		};

		BothGesturesCollectionView.ItemsSource = Items;
		OnlyDropCollectionView.ItemsSource = Items;
		OnlyDragCollectionView.ItemsSource = Items;
		MixedGesturesCollectionView.ItemsSource = Items;
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.Count > 0)
		{
			var selectedItem = e.CurrentSelection[0].ToString();
			var collectionView = sender as CollectionView;
			string scenario = "Unknown";
			
			if (collectionView == BothGesturesCollectionView)
				scenario = "Scenario 1 (Both)";
			else if (collectionView == OnlyDropCollectionView)
				scenario = "Scenario 2 (Drop only)";
			else if (collectionView == OnlyDragCollectionView)
				scenario = "Scenario 3 (Drag only)";
			else if (collectionView == MixedGesturesCollectionView)
				scenario = "Scenario 4 (Mixed)";
			
			StatusLabel.Text = $"{scenario} - Selected: {selectedItem}";
			Console.WriteLine($"=== SELECTION EVENT === {scenario}: {selectedItem}");
		}
		else
		{
			StatusLabel.Text = "No selection";
			Console.WriteLine("=== SELECTION EVENT === No selection");
		}
	}

	private void OnLabelTapped(object sender, EventArgs e)
	{
		_tapCount++;
		TapCountLabel.Text = $"Tap count (Scenario 4): {_tapCount}";
		Console.WriteLine($"=== TAP GESTURE === Count: {_tapCount}");
	}
}
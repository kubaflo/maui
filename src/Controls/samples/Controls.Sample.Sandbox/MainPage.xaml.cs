using System.Collections.ObjectModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public ObservableCollection<string> Items { get; set; }

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
		
		TestCollectionView.ItemsSource = Items;
		
		Console.WriteLine("[REPRO-32702] MainPage initialized with items");
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		Console.WriteLine($"[REPRO-32702] SelectionChanged event fired!");
		
		if (e.CurrentSelection.Count > 0)
		{
			var selectedItem = e.CurrentSelection[0].ToString();
			StatusLabel.Text = $"Selected: {selectedItem}";
			Console.WriteLine($"[REPRO-32702] Selected item: {selectedItem}");
		}
		else
		{
			StatusLabel.Text = "No selection";
			Console.WriteLine($"[REPRO-32702] Selection cleared");
		}
	}
}
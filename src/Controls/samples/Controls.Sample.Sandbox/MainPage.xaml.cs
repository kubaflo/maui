using System.Collections.ObjectModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public ObservableCollection<string> Items { get; set; }

	public MainPage()
	{
		InitializeComponent();
		
		// Create a list of 50 items to ensure scrolling and cell reuse
		Items = new ObservableCollection<string>();
		for (int i = 1; i <= 50; i++)
		{
			Items.Add($"Question {i}");
		}
		
		BindingContext = this;
		
		Console.WriteLine("=== Issue #32407 Reproduction ===");
		Console.WriteLine("Scroll through the items and observe:");
		Console.WriteLine("- Blue separators should always be visible");
		Console.WriteLine("- Bug: Separators disappear/reappear during scrolling");
		Console.WriteLine("==================================");
	}
}
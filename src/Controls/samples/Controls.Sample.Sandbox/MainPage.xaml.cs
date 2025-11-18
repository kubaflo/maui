using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public ObservableCollection<AnimalGroup> Animals { get; set; } = new();

	public MainPage()
	{
		InitializeComponent();

		// Populate data similar to the reproduction project
		Animals.Add(new AnimalGroup("Bears", new List<Animal>
		{
			new Animal
			{
				Name = "American Black Bear",
				Location = "North America"
			},
			new Animal
			{
				Name = "Asian Black Bear",
				Location = "Asia"
			}
		}));

		Animals.Add(new AnimalGroup("Monkeys", new List<Animal>
		{
			new Animal
			{
				Name = "Baboon",
				Location = "Africa & Asia"
			},
			new Animal
			{
				Name = "Capuchin Monkey",
				Location = "Central & South America"
			},
			new Animal
			{
				Name = "Blue Monkey",
				Location = "Central and East Africa"
			}
		}));

		BindingContext = this;
	}

	private void OnToggleSizingStrategy(object sender, EventArgs e)
	{
		// Toggle between MeasureFirstItem and MeasureAllItems
		if (TestCollection.ItemSizingStrategy == ItemSizingStrategy.MeasureFirstItem)
		{
			TestCollection.ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems;
			StatusLabel.Text = "Current: MeasureAllItems";
			System.Diagnostics.Debug.WriteLine("=== Switched to MeasureAllItems ===");
		}
		else
		{
			TestCollection.ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem;
			StatusLabel.Text = "Current: MeasureFirstItem";
			System.Diagnostics.Debug.WriteLine("=== Switched to MeasureFirstItem ===");
		}
	}
}

public class Animal
{
	public string Name { get; set; }
	public string Location { get; set; }
}

public class AnimalGroup : List<Animal>
{
	public string Name { get; private set; }

	public AnimalGroup(string name, List<Animal> animals) : base(animals)
	{
		Name = name;
	}
}
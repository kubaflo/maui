using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 32578, "Grouped CollectionView doesn't size correctly when ItemSizingStrategy=MeasureFirstItem", PlatformAffected.Android)]
	public partial class Issue32578 : ContentPage
	{
		public ObservableCollection<AnimalGroup> Animals { get; set; } = new();

		public Issue32578()
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
}

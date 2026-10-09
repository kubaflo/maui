namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27137, "CollectionView EmptyView is hidden behind the iOS keyboard", PlatformAffected.iOS)]
public class Issue27137 : ContentPage
{
	readonly string[] _filterItems =
	[
		"Apple",
		"Banana",
		"Cherry",
		"Orange"
	];

	public Issue27137()
	{
		Title = "EmptyView (string)";

		var filterStatusLabel = new Label
		{
			AutomationId = "FilterStatusLabel",
			FontSize = 12,
			Text = "waiting"
		};

		var instructionLabel = new Label
		{
			AutomationId = "InstructionLabel",
			Text = "Filter the items below; the empty-view message should remain visible while the keyboard is open."
		};

		var filterSearchBar = new SearchBar
		{
			AutomationId = "FilterSearchBar",
			Placeholder = "Filter items"
		};

		var itemsCollection = new CollectionView
		{
			AutomationId = "ItemsCollection",
			EmptyView = "No items match your filter.",
			ItemsSource = _filterItems,
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					Padding = 12
				};
				itemLabel.SetBinding(Label.TextProperty, ".");
				return itemLabel;
			})
		};

		filterSearchBar.TextChanged += (_, e) =>
		{
			var filter = e.NewTextValue ?? string.Empty;
			var filteredItems = _filterItems
				.Where(item => item.Contains(filter, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			itemsCollection.ItemsSource = filteredItems;
			filterStatusLabel.Text = $"filtered-count: {filteredItems.Length}";
		};

		var rootGrid = new Grid
		{
			Padding = 20,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		rootGrid.Add(filterStatusLabel, 0, 0);
		rootGrid.Add(instructionLabel, 0, 1);
		rootGrid.Add(filterSearchBar, 0, 2);
		rootGrid.Add(itemsCollection, 0, 3);
		Content = rootGrid;
	}
}


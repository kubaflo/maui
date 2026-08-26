namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	public Issue35301()
	{
		Title = "CollectionView selection";

		var selectionStateLabel = new Label
		{
			AutomationId = "SelectionState",
			Text = "Selection received: none"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "IssueCollectionView",
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					Padding = 8,
					FontSize = 20
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		collectionView.SelectionChanged += (_, args) =>
		{
			var selectedItem = args.CurrentSelection.Count > 0
				? args.CurrentSelection[0]?.ToString()
				: null;
			selectionStateLabel.Text = $"Selection received: {selectedItem ?? "none"}";
		};

		var grid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		var instructionLabel = new Label
		{
			Text = "Select Apple in the default-styled single-selection CollectionView."
		};
		var scenarioLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			Text = "Default CollectionView item appearance"
		};

		grid.Add(instructionLabel, 0, 0);
		grid.Add(scenarioLabel, 0, 1);
		grid.Add(selectionStateLabel, 0, 2);
		grid.Add(collectionView, 0, 3);
		Content = grid;
	}
}


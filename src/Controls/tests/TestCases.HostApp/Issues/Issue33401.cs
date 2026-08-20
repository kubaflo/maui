namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33401, "CollectionView SelectionChanged is not fired inside a Grid with a TapGestureRecognizer", PlatformAffected.iOS)]
public class Issue33401 : ContentPage
{
	public Issue33401()
	{
		var parentTapCount = 0;
		var selectionChangedCount = 0;

		var parentTapCountLabel = new Label
		{
			AutomationId = "ParentTapCount",
			Text = "Parent tap count: 0"
		};

		var selectionChangedCountLabel = new Label
		{
			AutomationId = "SelectionChangedCount",
			Text = "Selection changed count: 0"
		};

		var collectionView = new CollectionView
		{
			ItemsSource = new[]
			{
				"First item",
				"Second item",
				"Third item"
			},
			SelectionMode = SelectionMode.Single
		};

		collectionView.SelectionChanged += (sender, args) =>
		{
			if (args.CurrentSelection.Count == 0)
				return;

			selectionChangedCount++;
			selectionChangedCountLabel.Text = $"Selection changed count: {selectionChangedCount}";
		};

		var gestureGrid = new Grid();
		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
		{
			parentTapCount++;
			parentTapCountLabel.Text = $"Parent tap count: {parentTapCount}";
		};
		gestureGrid.GestureRecognizers.Add(tapGestureRecognizer);
		gestureGrid.Add(new Border { Content = collectionView });

		var outerGrid = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};

		outerGrid.Add(new Label { Text = "Tap the first CollectionView item." }, 0, 0);
		outerGrid.Add(gestureGrid, 0, 1);
		outerGrid.Add(parentTapCountLabel, 0, 2);
		outerGrid.Add(selectionChangedCountLabel, 0, 3);
		outerGrid.Add(new Label { Text = "Both counts should become 1 after one tap." }, 0, 4);

		Content = outerGrid;
	}
}


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33401, "CollectionView SelectionChanged is not fired inside a Grid with a TapGestureRecognizer", PlatformAffected.iOS)]
public class Issue33401 : ContentPage
{
	public Issue33401()
	{
		var gridTapCountLabel = new Label
		{
			AutomationId = "Issue33401GridTapCount",
			Text = "0"
		};

		var selectionChangedCountLabel = new Label
		{
			AutomationId = "Issue33401SelectionChangedCount",
			Text = "0"
		};

		var attachedStateLabel = new Label
		{
			AutomationId = "Issue33401AttachedState",
			Text = "-1"
		};

		int gridTapCount = 0;
		int selectionChangedCount = 0;

		var collectionView = new CollectionView
		{
			ItemsSource = new[] { "Collection item" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					AutomationId = "Issue33401Item"
				};
				itemLabel.SetBinding(Label.TextProperty, ".");
				return itemLabel;
			})
		};
		collectionView.SelectionChanged += (_, _) =>
		{
			selectionChangedCount++;
			selectionChangedCountLabel.Text = selectionChangedCount.ToString();
		};

		var border = new Border
		{
			Content = collectionView
		};

		var rootGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};

		var gridTapRecognizer = new TapGestureRecognizer();
		gridTapRecognizer.Tapped += (_, _) =>
		{
			gridTapCount++;
			gridTapCountLabel.Text = gridTapCount.ToString();
		};
		rootGrid.GestureRecognizers.Add(gridTapRecognizer);

		rootGrid.Children.Add(border);
		Grid.SetRow(gridTapCountLabel, 1);
		rootGrid.Children.Add(gridTapCountLabel);
		Grid.SetRow(selectionChangedCountLabel, 2);
		rootGrid.Children.Add(selectionChangedCountLabel);
		Grid.SetRow(attachedStateLabel, 3);
		rootGrid.Children.Add(attachedStateLabel);

		Loaded += (_, _) => attachedStateLabel.Text = "0";
		Content = rootGrid;
	}
}

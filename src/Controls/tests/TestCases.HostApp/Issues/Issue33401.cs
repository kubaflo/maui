namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33401, "CollectionView SelectionChanged is not fired on iOS when inside a Grid with a TapGestureRecognizer", PlatformAffected.iOS)]
public class Issue33401 : ContentPage
{
	public Issue33401()
	{
		int gridTapCount = 0;
		int selectionChangedCount = 0;

		var gridTapCountLabel = new Label
		{
			AutomationId = "GridTapCountLabel",
			Text = "Grid taps: 0"
		};

		var selectionChangedCountLabel = new Label
		{
			AutomationId = "SelectionChangedCountLabel",
			Text = "SelectionChanged: 0"
		};

		var collectionView = new CollectionView
		{
			ItemsSource = new[] { "Alpha" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					AutomationId = "AlphaItem",
					Padding = new Thickness(12)
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};
		collectionView.SelectionChanged += (_, _) =>
		{
			selectionChangedCount++;
			selectionChangedCountLabel.Text = $"SelectionChanged: {selectionChangedCount}";
		};

		var grid = new Grid();
		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) =>
		{
			gridTapCount++;
			gridTapCountLabel.Text = $"Grid taps: {gridTapCount}";
		};
		grid.GestureRecognizers.Add(tapGestureRecognizer);
		grid.Add(new Border
		{
			Content = collectionView
		});

		Content = new VerticalStackLayout
		{
			Padding = new Thickness(24),
			Spacing = 16,
			Children =
			{
				gridTapCountLabel,
				selectionChangedCountLabel,
				grid
			}
		};
	}
}


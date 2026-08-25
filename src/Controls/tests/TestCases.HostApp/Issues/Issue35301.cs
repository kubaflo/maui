namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	readonly Label _statusLabel;
	int _generation = -1;

	public Issue35301()
	{
		_statusLabel = new Label
		{
			AutomationId = "Issue35301Status",
			FontSize = 18,
			Text = "Ready|Generation=-1|Item=<none>"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "Issue35301CollectionView",
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 24,
					Padding = 12
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		collectionView.SelectionChanged += (_, args) =>
		{
			if (args.CurrentSelection.Count != 1)
				return;

			_generation++;
			_statusLabel.Text = $"Selected|Generation={_generation}|Item={args.CurrentSelection[0]}";
		};

		var grid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16
		};
		grid.Add(new Label
		{
			FontSize = 20,
			Text = "Select an item in the default CollectionView."
		});
		grid.Add(_statusLabel, row: 1);
		grid.Add(collectionView, row: 2);
		Content = grid;
	}
}


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.WinRT)]
public class Issue35301 : TestContentPage
{
	protected override void Init()
	{
		var selectionState = new Label
		{
			AutomationId = "SelectionState",
			FontAttributes = FontAttributes.Bold,
			Text = "NONE"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "TestCollectionView",
			ItemsSource = new[] { "Apple", "Banana", "Cherry" },
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					Padding = 8,
					FontSize = 18
				};
				label.SetBinding(Label.TextProperty, ".");
				label.SetBinding(Label.AutomationIdProperty, ".");
				return label;
			})
		};

		collectionView.SelectionChanged += (_, e) =>
		{
			if (e.CurrentSelection.Count == 1 && e.CurrentSelection[0] is string selectedItem)
				selectionState.Text = selectedItem;
		};

		Content = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "NeutralElement",
					FontSize = 18,
					Text = "Select Apple. The row should not gain a rounded pill or blue accent."
				},
				selectionState,
				collectionView
			}
		};

		Grid.SetRow(selectionState, 1);
		Grid.SetRow(collectionView, 2);
	}
}


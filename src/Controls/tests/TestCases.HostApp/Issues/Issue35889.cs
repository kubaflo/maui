namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35889, "Empty CollectionView has incorrect height on iOS", PlatformAffected.iOS)]
public class Issue35889 : ContentPage
{
	public Issue35889()
	{
		var lifecycleStatus = new Label
		{
			AutomationId = "LifecycleStatus",
			Text = "UNTRIGGERED"
		};

		var showScenarioButton = new Button
		{
			AutomationId = "ShowScenario",
			Text = "Show empty CollectionView"
		};

		var landingGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 16
		};

		landingGrid.Add(new Label
		{
			Text = "Issue 35889: Empty CollectionView height",
			FontSize = 20
		});
		landingGrid.Add(lifecycleStatus, 0, 1);
		landingGrid.Add(showScenarioButton, 0, 2);

		showScenarioButton.Clicked += (_, _) =>
		{
			landingGrid.Children.Remove(lifecycleStatus);

			var layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			var beforeLabel = new Label
			{
				AutomationId = "BeforeCollectionLabel",
				Text = "before collectionview"
			};

			var collectionView = new CollectionView
			{
				AutomationId = "EmptyCollectionView",
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};

			var afterLabel = new Label
			{
				AutomationId = "AfterCollectionLabel",
				Text = "after collectionview"
			};

			lifecycleStatus.HorizontalOptions = LayoutOptions.End;

			layout.Add(beforeLabel);
			layout.Add(collectionView, 0, 1);
			layout.Add(afterLabel, 0, 2);
			layout.Add(lifecycleStatus);

			collectionView.Loaded += (_, _) => lifecycleStatus.Text = "LOADED";
			Content = layout;
		};

		Content = landingGrid;
	}
}


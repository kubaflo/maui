using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27332, "CollectionView footer is displayed at the bottom of the page", PlatformAffected.UWP)]
public class Issue27332 : ContentPage
{
	public Issue27332()
	{
		var items = new ObservableCollection<string>();
		var actionStatus = new Label
		{
			Text = "Clear handled: 0",
			AutomationId = "ActionStatus",
			VerticalTextAlignment = TextAlignment.Center
		};
		var addItemsButton = new Button
		{
			Text = "Add 2 Items",
			AutomationId = "AddItemsButton",
			HorizontalOptions = LayoutOptions.Center
		};
		addItemsButton.Clicked += (_, _) =>
		{
			items.Add("Item 1");
			items.Add("Item 2");
		};

		var clearItemsButton = new Button
		{
			Text = "Clear All Items",
			AutomationId = "ClearItemsButton",
			HorizontalOptions = LayoutOptions.Center
		};
		var clearCount = 0;
		clearItemsButton.Clicked += (_, _) =>
		{
			items.Clear();
			clearCount++;
			actionStatus.Text = $"Clear handled: {clearCount}";
		};

		var buttonGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
			}
		};
		buttonGrid.Add(addItemsButton, 0);
		buttonGrid.Add(clearItemsButton, 1);

		var statusGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star)
			}
		};
		statusGrid.Add(actionStatus, 0);

		var collectionView = new CollectionView
		{
			AutomationId = "ReproCollection",
			ItemsSource = items,
			ItemTemplate = new DataTemplate(() =>
			{
				var itemLabel = new Label
				{
					Padding = 12
				};
				itemLabel.SetBinding(Label.TextProperty, ".");
				return itemLabel;
			}),
			Header = new VerticalStackLayout
			{
				BackgroundColor = Colors.LightGray,
				Children =
				{
					new Label
					{
						Text = "Header",
						AutomationId = "HeaderView",
						FontAttributes = FontAttributes.Bold,
						FontSize = 12
					}
				}
			},
			Footer = new VerticalStackLayout
			{
				BackgroundColor = Colors.LightGray,
				Children =
				{
					new Label
					{
						Text = "Footer",
						AutomationId = "FooterView",
						FontAttributes = FontAttributes.Bold,
						FontSize = 12
					}
				}
			}
		};
		var rootGrid = new Grid
		{
			Margin = 20,
			RowSpacing = 14,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		rootGrid.Add(new Label
		{
			Text = "1. The test passes if the button is able to trigger the onClicked event handler and the page displays normally.",
			FontSize = 18
		}, 0, 0);
		rootGrid.Add(buttonGrid, 0, 1);
		rootGrid.Add(statusGrid, 0, 2);
		rootGrid.Add(collectionView, 0, 3);

		Title = "Header and Footer (Add Clear)";
		Content = rootGrid;
	}
}


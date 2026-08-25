namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28023, "ItemSpacing is retained after re-entering a CollectionView page", PlatformAffected.iOS)]
public class Issue28023 : NavigationPage
{
	public Issue28023() : base(new ItemSpacingMenuPage())
	{
	}

	sealed class ItemSpacingMenuPage : ContentPage
	{
		int pageOpenCount;

		public ItemSpacingMenuPage()
		{
			Title = "I2 - ItemSpacing";

			var verticalListCell = new TextCell
			{
				AutomationId = "VerticalListCell",
				Text = "Vertical list for ItemSpacing",
				Detail = "Open the vertical monkey list"
			};
			verticalListCell.Tapped += OnVerticalListCellTapped;

			var tableSection = new TableSection("CollectionView")
			{
				verticalListCell
			};
			var tableRoot = new TableRoot
			{
				tableSection
			};

			var menuGrid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			menuGrid.Add(new Label
			{
				Text = "I2 - ItemSpacing",
				FontSize = 24,
				FontAttributes = FontAttributes.Bold
			});
			menuGrid.Add(new TableView
			{
				Root = tableRoot,
				Intent = TableIntent.Menu
			}, 0, 1);
			Content = menuGrid;

			async void OnVerticalListCellTapped(object sender, EventArgs e)
			{
				await Navigation.PushAsync(new VerticalItemSpacingPage(++pageOpenCount));
			}
		}
	}

	sealed class VerticalItemSpacingPage : ContentPage
	{
		readonly Entry spacingEntry;
		readonly Label currentSpacingLabel;
		readonly Label pageInstanceLabel;
		readonly LinearItemsLayout itemsLayout;

		public VerticalItemSpacingPage(int pageInstance)
		{
			Title = "Vertical list for ItemSpacing";
			itemsLayout = (LinearItemsLayout)LinearItemsLayout.Vertical;

			spacingEntry = new Entry
			{
				AutomationId = "SpacingEntry",
				Keyboard = Keyboard.Numeric,
				Text = "0"
			};

			var updateButton = new Button
			{
				AutomationId = "UpdateSpacingButton",
				Text = "Update"
			};
			updateButton.Clicked += OnUpdateClicked;

			currentSpacingLabel = new Label
			{
				AutomationId = "CurrentSpacing",
				Text = "Current spacing: 0",
				FontAttributes = FontAttributes.Bold
			};

			pageInstanceLabel = new Label
			{
				AutomationId = "PageInstance",
				Text = "Page instance: pending"
			};

			var controls = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				},
				ColumnSpacing = 8
			};
			controls.Add(spacingEntry);
			controls.Add(updateButton, 1);

			var rowIndex = 0;
			var collectionView = new CollectionView
			{
				AutomationId = "MonkeyCollection",
				ItemsLayout = itemsLayout,
				ItemsSource = new[]
				{
					"Baboon - Africa",
					"Capuchin Monkey - South America",
					"Blue Monkey - Central Africa",
					"Squirrel Monkey - South America"
				},
				ItemTemplate = new DataTemplate(() => CreateMonkeyRow(rowIndex++))
			};

			var content = new Grid
			{
				Padding = 16,
				RowSpacing = 8,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			content.Add(controls);
			content.Add(currentSpacingLabel, 0, 1);
			content.Add(pageInstanceLabel, 0, 2);
			content.Add(collectionView, 0, 3);
			Content = content;

			Loaded += (sender, e) => pageInstanceLabel.Text = $"Page instance: {pageInstance}";
		}

		static View CreateMonkeyRow(int rowIndex)
		{
			var nameLabel = new Label
			{
				AutomationId = $"MonkeyName{rowIndex}",
				VerticalOptions = LayoutOptions.Center,
				FontSize = 16
			};
			nameLabel.SetBinding(Label.TextProperty, ".");

			return new Border
			{
				AutomationId = $"MonkeyRow{rowIndex}",
				BackgroundColor = Colors.LightGray,
				Padding = new Thickness(12),
				HeightRequest = 54,
				Content = new HorizontalStackLayout
				{
					Spacing = 12,
					Children =
					{
						new Label
						{
							Text = "Monkey",
							FontAttributes = FontAttributes.Bold,
							VerticalOptions = LayoutOptions.Center
						},
						nameLabel
					}
				}
			};
		}

		void OnUpdateClicked(object sender, EventArgs e)
		{
			if (!double.TryParse(spacingEntry.Text, out var spacing) || spacing < 0)
			{
				currentSpacingLabel.Text = "Enter a nonnegative spacing value.";
				return;
			}

			itemsLayout.ItemSpacing = spacing;
			currentSpacingLabel.Text = $"Current spacing: {spacing:0}";
			spacingEntry.Unfocus();
		}
	}
}


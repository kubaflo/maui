namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28023, "CollectionView retains ItemSpacing after navigating back and re-entering", PlatformAffected.iOS)]
public class Issue28023 : NavigationPage
{
	public Issue28023() : base(new MenuPage())
	{
	}

	sealed class MenuPage : ContentPage
	{
		public MenuPage()
		{
			Title = "I2_Spacing";

			var verticalListCell = new TextCell
			{
				Text = "Vertical list",
				Detail = "ItemSpacing",
				AutomationId = "Issue28023VerticalListCell",
				Command = new Command(async () => await Navigation.PushAsync(new VerticalListSpacingPage()))
			};

			Content = new TableView
			{
				Intent = TableIntent.Menu,
				Root = new TableRoot
				{
					new TableSection
					{
						verticalListCell
					}
				}
			};
		}
	}

	sealed class VerticalListSpacingPage : ContentPage
	{
		readonly Entry _spacingEntry;
		readonly CollectionView _collectionView;

		public VerticalListSpacingPage()
		{
			Title = "Vertical list (spacing)";

			var instructions = new StackLayout
			{
				Children =
				{
					new Label { Text = "1. The Monkeys are displayed in a single column list." },
					new Label { Text = "2. The spacing between the Monkeys should reset after re-entering." }
				}
			};

			_spacingEntry = new Entry
			{
				Text = "0",
				WidthRequest = 100,
				AutomationId = "Issue28023SpacingEntry"
			};

			var updateButton = new Button
			{
				Text = "Update",
				AutomationId = "Issue28023UpdateButton"
			};
			updateButton.Clicked += OnUpdateButtonClicked;

			var spacingModifier = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.Center,
				Children =
				{
					new Label { Text = "Spacing:", VerticalTextAlignment = TextAlignment.Center },
					_spacingEntry,
					updateButton
				}
			};

			_collectionView = new CollectionView
			{
				AutomationId = "Issue28023MonkeyCollection",
				ItemsSource = new[] { "Baboon", "Capuchin Monkey", "Blue Monkey", "Squirrel Monkey" },
				ItemTemplate = new DataTemplate(CreateMonkeyItem),
				ItemsLayout = LinearItemsLayout.Vertical
			};

			var pageLayout = new Grid
			{
				Margin = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			pageLayout.Add(instructions);
			pageLayout.Add(spacingModifier, 0, 1);
			pageLayout.Add(_collectionView, 0, 2);
			Content = pageLayout;
		}

		static View CreateMonkeyItem()
		{
			var monkeyName = new Label
			{
				FontAttributes = FontAttributes.Bold,
				VerticalTextAlignment = TextAlignment.Center
			};
			monkeyName.SetBinding(Label.TextProperty, ".");

			var itemRoot = new Grid
			{
				Padding = 10,
				HeightRequest = 80,
				BackgroundColor = Colors.LightBlue,
				Children = { monkeyName }
			};
			itemRoot.SetBinding(AutomationIdProperty, new Binding(".", stringFormat: "Issue28023Item_{0}"));
			return itemRoot;
		}

		void OnUpdateButtonClicked(object sender, EventArgs e)
		{
			if (int.TryParse(_spacingEntry.Text, out int spacing) &&
				_collectionView.ItemsLayout is LinearItemsLayout linearItemsLayout)
			{
				linearItemsLayout.ItemSpacing = spacing;
			}
		}
	}
}


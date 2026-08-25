namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28023, "ItemSpacing is retained when opening a fresh CollectionView", PlatformAffected.iOS)]
public class Issue28023 : NavigationPage
{
	public Issue28023() : this(new ItemSpacingScenarioState())
	{
	}

	Issue28023(ItemSpacingScenarioState scenarioState) : base(new ItemSpacingGalleryPage(scenarioState))
	{
	}

	static Grid CreateGalleryContent(EventHandler tapped)
	{
		var openListCell = new TextCell
		{
			Text = "Vertical list",
			Detail = "ItemSpacing",
			AutomationId = "OpenVerticalList"
		};
		openListCell.Tapped += tapped;

		var gallery = new TableView
		{
			Intent = TableIntent.Menu,
			AutomationId = "ItemSpacingGallery",
			Root = new TableRoot
			{
				new TableSection("I2")
				{
					openListCell
				}
			}
		};

		return new Grid
		{
			Padding = 20,
			Children =
			{
				gallery
			}
		};
	}

	sealed class ItemSpacingScenarioState
	{
		public int ListVisits { get; set; }
	}

	sealed class ItemSpacingGalleryPage : ContentPage
	{
		readonly ItemSpacingScenarioState _scenarioState;

		public ItemSpacingGalleryPage(ItemSpacingScenarioState scenarioState)
		{
			_scenarioState = scenarioState;
			Title = "I2 ItemSpacing";
			Content = CreateGalleryContent(OnVerticalListTapped);
		}

		async void OnVerticalListTapped(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ItemSpacingPage(_scenarioState));
		}
	}

	sealed class ItemSpacingPage : ContentPage
	{
		readonly ItemSpacingScenarioState _scenarioState;
		readonly LinearItemsLayout _itemsLayout;
		readonly Entry _spacingEntry;

		public ItemSpacingPage(ItemSpacingScenarioState scenarioState)
		{
			_scenarioState = scenarioState;
			_scenarioState.ListVisits++;
			_itemsLayout = (LinearItemsLayout)LinearItemsLayout.Vertical;
			Title = "Vertical list ItemSpacing";

			_spacingEntry = new Entry
			{
				Text = "0",
				WidthRequest = 100,
				Keyboard = Keyboard.Numeric,
				AutomationId = "SpacingEntry"
			};

			var updateButton = new Button
			{
				Text = "Update",
				AutomationId = "UpdateSpacing"
			};
			updateButton.Clicked += OnUpdateClicked;

			var spacingModifier = new ContentView
			{
				Content = new HorizontalStackLayout
				{
					Spacing = 8,
					Children =
					{
						_spacingEntry,
						updateButton
					}
				}
			};

			var returnButton = new Button
			{
				Text = "Return to ItemSpacing gallery",
				AutomationId = "ReturnToGallery"
			};
			returnButton.Clicked += OnReturnClicked;

			var visitMarker = new Label
			{
				Text = $"Visit: {_scenarioState.ListVisits}",
				AutomationId = "VisitMarker"
			};

			var collectionView = new CollectionView
			{
				ItemsLayout = _itemsLayout,
				ItemsSource = new[]
				{
					"Monkey 1",
					"Monkey 2",
					"Monkey 3",
					"Monkey 4",
					"Monkey 5",
					"Monkey 6"
				},
				ItemTemplate = CreateMonkeyTemplate(),
				AutomationId = "MonkeyCollection"
			};

			var layout = new Grid
			{
				Margin = 20,
				RowSpacing = 8,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				Children =
				{
					spacingModifier,
					returnButton,
					visitMarker,
					collectionView
				}
			};
			Grid.SetRow(returnButton, 1);
			Grid.SetRow(visitMarker, 2);
			Grid.SetRow(collectionView, 3);
			Content = layout;
		}

		static DataTemplate CreateMonkeyTemplate()
		{
			return new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 20,
					VerticalTextAlignment = TextAlignment.Center
				};
				label.SetBinding(Label.TextProperty, ".");

				var itemSurface = new Grid
				{
					WidthRequest = 120,
					HeightRequest = 100,
					BackgroundColor = Colors.BlanchedAlmond,
					Children =
					{
						label
					}
				};
				itemSurface.SetBinding(AutomationIdProperty, ".");
				return itemSurface;
			});
		}

		void OnUpdateClicked(object sender, EventArgs e)
		{
			if (int.TryParse(_spacingEntry.Text, out int spacing))
			{
				_itemsLayout.ItemSpacing = spacing;
			}
		}

		async void OnReturnClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ItemSpacingGalleryPage(_scenarioState));
		}
	}
}


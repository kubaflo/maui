using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28023, "Item spacing is retained when reopening a CollectionView page", PlatformAffected.Android)]
public class Issue28023 : NavigationPage
{
	public Issue28023() : base(new GalleryPage())
	{
	}

	sealed class GalleryPage : ContentPage
	{
		readonly IItemsLayout sharedItemsLayout = LinearItemsLayout.Vertical;
		int pageInstance = 0;

		public GalleryPage()
		{
			var openButton = new Button
			{
				AutomationId = "OpenVerticalSpacing",
				Text = "Vertical list for ItemSpacing",
				VerticalOptions = LayoutOptions.Start
			};

			openButton.Clicked += async (sender, args) =>
				await Navigation.PushAsync(CreateSpacingPage(sharedItemsLayout, ++pageInstance));

			Content = new Grid
			{
				Padding = 24,
				Children = { openButton }
			};
		}
	}

	static ContentPage CreateSpacingPage(IItemsLayout itemsLayout, int instance)
	{
		var spacingEntry = new Entry
		{
			AutomationId = "SpacingEntry",
			Text = "0",
			Keyboard = Keyboard.Numeric,
			WidthRequest = 100
		};

		var updateButton = new Button
		{
			AutomationId = "UpdateSpacingButton",
			Text = "Update"
		};

		updateButton.Clicked += (sender, args) =>
		{
			if (itemsLayout is LinearItemsLayout linearItemsLayout &&
				spacingEntry.Text is string text &&
				int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int spacing))
			{
				linearItemsLayout.ItemSpacing = spacing;
				spacingEntry.Unfocus();
			}
		};

		var editor = new HorizontalStackLayout
		{
			Spacing = 10,
			Children =
			{
				new Label
				{
					Text = "Spacing:",
					VerticalTextAlignment = TextAlignment.Center
				},
				spacingEntry,
				updateButton
			}
		};

		var instanceLabel = new Label
		{
			AutomationId = "DetailPageInstance",
			Text = instance.ToString(CultureInfo.InvariantCulture)
		};

		var loadStateLabel = new Label
		{
			AutomationId = "DetailPageLoadState",
			Text = "NotLoaded"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "MonkeyCollection",
			ItemsLayout = itemsLayout,
			ItemsSource = new[]
			{
				new Monkey("Baboon", "Africa"),
				new Monkey("Capuchin", "Central America"),
				new Monkey("Howler", "South America"),
				new Monkey("Macaque", "Asia")
			},
			ItemTemplate = new DataTemplate(CreateMonkeyRow),
			Margin = 10
		};

		var observations = new HorizontalStackLayout
		{
			Children = { instanceLabel, loadStateLabel }
		};

		var layout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Padding = 12
		};
		layout.Add(editor);
		layout.Add(observations, 0, 1);
		layout.Add(collectionView, 0, 2);

		var page = new ContentPage
		{
			Title = "Vertical list for ItemSpacing",
			Content = layout
		};
		page.Loaded += (sender, args) => loadStateLabel.Text = "Loaded";
		return page;
	}

	static View CreateMonkeyRow()
	{
		var row = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(64),
				new ColumnDefinition(GridLength.Star)
			},
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			HeightRequest = 72,
			Padding = 6
		};
		var image = new Image
		{
			Source = "dotnet_bot.png",
			WidthRequest = 50,
			HeightRequest = 50
		};
		var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
		nameLabel.SetBinding(Label.TextProperty, nameof(Monkey.Name));
		nameLabel.SetBinding(AutomationIdProperty, new Binding(nameof(Monkey.Name), stringFormat: "MonkeyName{0}"));
		var locationLabel = new Label();
		locationLabel.SetBinding(Label.TextProperty, nameof(Monkey.Location));

		row.Add(image, 0, 0);
		Grid.SetRowSpan(image, 2);
		row.Add(nameLabel, 1, 0);
		row.Add(locationLabel, 1, 1);
		return row;
	}

	sealed class Monkey
	{
		public Monkey(string name, string location)
		{
			Name = name;
			Location = location;
		}

		public string Name { get; }
		public string Location { get; }
	}
}


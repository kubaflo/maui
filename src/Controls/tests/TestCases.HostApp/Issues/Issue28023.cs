#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28023, "CollectionView ItemSpacing persists after re-entering the page", PlatformAffected.iOS)]
public class Issue28023 : TestNavigationPage
{
	int _pageOpenCount;

	protected override void Init()
	{
		((LinearItemsLayout)LinearItemsLayout.Vertical).ItemSpacing = 0;

		var openVerticalListButton = new Button
		{
			Text = "Vertical List Spacing",
			AutomationId = "VerticalListSpacingButton"
		};
		openVerticalListButton.Clicked += async (_, _) =>
		{
			_pageOpenCount++;
			await Navigation.PushAsync(CreateSpacingPage());
		};

		var menuPage = new ContentPage
		{
			Title = "Item Spacing Galleries",
			Content = new ScrollView
			{
				Content = new StackLayout
				{
					Padding = 12,
					Spacing = 12,
					Children =
					{
						new Label
						{
							Text = "Item Spacing Galleries",
							FontSize = 22
						},
						openVerticalListButton
					}
				}
			}
		};

		_ = PushAsync(menuPage);
	}

	ContentPage CreateSpacingPage()
	{
		var itemsLayout = (LinearItemsLayout)LinearItemsLayout.Vertical;
		var spacingEntry = new Entry
		{
			Text = "0",
			WidthRequest = 100,
			AutomationId = "SpacingEntry"
		};
		var updateButton = new Button
		{
			Text = "Update",
			AutomationId = "UpdateSpacingButton"
		};
		var pageVisitLabel = new Label
		{
			Text = "-1",
			AutomationId = "PageVisitStatus"
		};
		var modifier = new StackLayout
		{
			Orientation = StackOrientation.Horizontal,
			HorizontalOptions = LayoutOptions.Fill,
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
		var itemTemplate = new DataTemplate(() =>
		{
			var label = new Label
			{
				FontSize = 20,
				HeightRequest = 48,
				BackgroundColor = Colors.LightBlue,
				VerticalTextAlignment = TextAlignment.Center
			};
			label.SetBinding(Label.TextProperty, ".");
			label.SetBinding(Label.AutomationIdProperty, ".");
			return label;
		});
		var collectionView = new CollectionView
		{
			ItemsLayout = itemsLayout,
			ItemsSource = new[]
			{
				"Monkey 1",
				"Monkey 2",
				"Monkey 3",
				"Monkey 4",
				"Monkey 5",
				"Monkey 6"
			},
			ItemTemplate = itemTemplate,
			AutomationId = "MonkeyCollectionView",
			Margin = 10
		};

		updateButton.Clicked += (_, _) =>
		{
			if (double.TryParse(spacingEntry.Text, out double spacing))
				itemsLayout.ItemSpacing = spacing;
		};

		var layout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		var instructions = new Label
		{
			Text = "Use the control below to update the spacing between items."
		};
		layout.Add(instructions);
		layout.Add(modifier, 0, 1);
		layout.Add(pageVisitLabel, 0, 2);
		layout.Add(collectionView, 0, 3);

		var page = new ContentPage
		{
			Title = "Vertical List Spacing",
			Content = layout
		};
		page.Appearing += (_, _) =>
			pageVisitLabel.Text = _pageOpenCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

		return page;
	}
}
#endif


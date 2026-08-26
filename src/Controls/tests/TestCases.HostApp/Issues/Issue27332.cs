using System.Collections.ObjectModel;

#if WINDOWS
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27332, "CollectionView footer is displayed at the bottom after clearing items", PlatformAffected.UWP)]
public class Issue27332 : ContentPage
{
	readonly ObservableCollection<string> _items = new();
	readonly Label _itemCountLabel;
	readonly Label _layoutGenerationLabel;
#if WINDOWS
	bool _awaitingPostClearLayout;
#endif

	public Issue27332()
	{
		_layoutGenerationLabel = new Label
		{
			Text = "Layout generation: -1",
			AutomationId = "LayoutGenerationLabel",
			BackgroundColor = Colors.White,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.End
		};

		_itemCountLabel = new Label
		{
			Text = "Items: 0",
			AutomationId = "ItemCountLabel",
			BackgroundColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var addButton = new Button
		{
			Text = "Add 2 Items",
			AutomationId = "AddButton",
			FontAttributes = FontAttributes.Bold,
			Margin = 20,
			HorizontalOptions = LayoutOptions.Start
		};
		addButton.Clicked += AddItems;

		var clearButton = new Button
		{
			Text = "Clear All Items",
			AutomationId = "ClearButton",
			FontAttributes = FontAttributes.Bold,
			Margin = 20,
			HorizontalOptions = LayoutOptions.End
		};
		clearButton.Clicked += ClearItems;

		var collectionView = new CollectionView
		{
			AutomationId = "CollectionView",
			ItemsSource = _items,
			VerticalOptions = LayoutOptions.FillAndExpand,
			Header = CreateHeaderOrFooter("Header", "HeaderRoot"),
			Footer = CreateHeaderOrFooter("Footer", "FooterRoot"),
			ItemTemplate = new DataTemplate(CreateItemTemplate)
		};

#if WINDOWS
		collectionView.HandlerChanged += OnCollectionViewHandlerChanged;
#endif

		var layout = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		var instructionLayout = new StackLayout
		{
			new Label
			{
				Text = "1.The test passes if the button is able to trigger the onClicked event handler and the page displays normally."
			}
		};

		layout.Add(instructionLayout, 0, 0);
		layout.Add(_layoutGenerationLabel, 0, 0);
		layout.Add(addButton, 0, 1);
		layout.Add(_itemCountLabel, 0, 1);
		layout.Add(clearButton, 0, 1);
		layout.Add(collectionView, 0, 2);

		Content = layout;
	}

	static StackLayout CreateHeaderOrFooter(string text, string automationId)
	{
		return new StackLayout
		{
			AutomationId = automationId,
			BackgroundColor = Colors.LightGray,
			Children =
			{
				new Label
				{
					Text = text,
					Margin = new Thickness(10, 0, 0, 0),
					FontSize = 12,
					FontAttributes = FontAttributes.Bold
				}
			}
		};
	}

	static Grid CreateItemTemplate()
	{
		var itemLabel = new Label
		{
			FontSize = 24
		};
		itemLabel.SetBinding(Label.TextProperty, ".");

		var itemRoot = new Grid
		{
			Padding = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Auto)
			},
			Children =
			{
				new ContentView
				{
					Content = new Frame
					{
						Margin = new Thickness(2, 6),
						CornerRadius = 20,
						Content = itemLabel
					}
				}
			}
		};

		itemRoot.BindingContextChanged += (_, _) =>
		{
			if (itemRoot.AutomationId is null && itemRoot.BindingContext is string item)
				itemRoot.AutomationId = $"{item.Replace(" ", string.Empty, StringComparison.Ordinal)}Root";
		};

		return itemRoot;
	}

	void AddItems(object sender, EventArgs e)
	{
		_items.Add("Item 1");
		_items.Add("Item 2");
		_itemCountLabel.Text = "Items: 2";
	}

	void ClearItems(object sender, EventArgs e)
	{
		_layoutGenerationLabel.Text = "Layout generation: -1";
#if WINDOWS
		_awaitingPostClearLayout = true;
#endif
		_items.Clear();
		_itemCountLabel.Text = "Items: 0";
	}

#if WINDOWS
	void OnCollectionViewHandlerChanged(object sender, EventArgs e)
	{
		if (sender is CollectionView collectionView &&
			collectionView.Handler?.PlatformView is WFrameworkElement nativeView)
		{
			nativeView.LayoutUpdated += OnNativeLayoutUpdated;
		}
	}

	void OnNativeLayoutUpdated(object sender, object e)
	{
		if (!_awaitingPostClearLayout)
			return;

		_awaitingPostClearLayout = false;
		_layoutGenerationLabel.Text = "Layout generation: 1";
	}
#endif
}


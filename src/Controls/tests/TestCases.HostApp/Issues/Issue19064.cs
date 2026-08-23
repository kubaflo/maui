#if IOS
using System.Collections.Generic;
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 19064, "ItemSizingStrategy gallery displays items inconsistently", PlatformAffected.iOS)]
public class Issue19064 : ContentPage
{
	readonly CollectionView _itemsCollection;
	readonly Entry _itemCountEntry;
	readonly Picker _sizingPicker;
	readonly Label _explanationLabel;
	readonly Label _scrollStateLabel;
	readonly Label _checkResultLabel;
	readonly string[] _images = ["cover1.jpg", "vegetables.jpg", "fruits.jpg", "flowerbuds.jpg", "legumes.jpg"];
	bool _scrolledAway;
	int _returnCount;

	public Issue19064()
	{
		Title = "ItemSizing Strategy";

		_itemCountEntry = new Entry
		{
			AutomationId = "Issue19064ItemCountEntry",
			Keyboard = Keyboard.Numeric,
			Text = "100",
			WidthRequest = 100
		};

		var updateButton = new Button
		{
			AutomationId = "Issue19064UpdateButton",
			Text = "Update"
		};
		updateButton.Clicked += (_, _) => GenerateItems();

		var itemControls = new ContentView
		{
			Content = new HorizontalStackLayout
			{
				HorizontalOptions = LayoutOptions.Fill,
				Children =
				{
					new Label { Text = "Items:", VerticalTextAlignment = TextAlignment.Center },
					_itemCountEntry,
					updateButton
				}
			}
		};

		_sizingPicker = new Picker
		{
			AutomationId = "Issue19064SizingPicker",
			ItemsSource = Enum.GetNames<ItemSizingStrategy>(),
			SelectedItem = ItemSizingStrategy.MeasureFirstItem.ToString(),
			WidthRequest = 200
		};
		_sizingPicker.SelectedIndexChanged += OnSizingStrategyChanged;

		var sizingControls = new ContentView
		{
			Content = new HorizontalStackLayout
			{
				HorizontalOptions = LayoutOptions.Fill,
				Children =
				{
					new Label { Text = "ItemSizingStrategy:", VerticalTextAlignment = TextAlignment.Center },
					_sizingPicker
				}
			}
		};

		_explanationLabel = new Label
		{
			Text = "The first item is measured, and that size is given to all subsequent cells."
		};

		_scrollStateLabel = new Label
		{
			AutomationId = "Issue19064ScrollState",
			HorizontalOptions = LayoutOptions.End,
			InputTransparent = true,
			Text = "START",
			VerticalOptions = LayoutOptions.End
		};

		_checkResultLabel = new Label
		{
			AutomationId = "Issue19064CheckResult",
			HorizontalOptions = LayoutOptions.End,
			InputTransparent = true,
			Text = "NOT CHECKED",
			VerticalOptions = LayoutOptions.End
		};

		var checkButton = new Button
		{
			AutomationId = "Issue19064CheckButton",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Check returned item"
		};
		checkButton.Clicked += (_, _) =>
			_checkResultLabel.Text = $"RETURNS:{_returnCount}";

		var readyLabel = new Label
		{
			AutomationId = "Issue19064ReadyLabel",
			HorizontalOptions = LayoutOptions.End,
			InputTransparent = true,
			Text = "READY"
		};

		_itemsCollection = new CollectionView
		{
			AutomationId = "Issue19064CollectionView",
			ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
			ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Horizontal) { Span = 2 },
			ItemTemplate = CreateVariableSizeTemplate()
		};
		_itemsCollection.Scrolled += OnCollectionScrolled;

		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		root.Add(itemControls, 0, 0);
		root.Add(checkButton, 0, 0);
		root.Add(readyLabel, 0, 0);
		root.Add(_checkResultLabel, 0, 0);
		root.Add(sizingControls, 0, 1);
		root.Add(_scrollStateLabel, 0, 1);
		root.Add(_explanationLabel, 0, 2);
		root.Add(_itemsCollection, 0, 3);
		Content = root;

		GenerateItems();
	}

	DataTemplate CreateVariableSizeTemplate()
	{
		var heightConverter = new IndexRequestConverter(3, 50, 150);
		var widthConverter = new IndexRequestConverter(3, 100, 300);
		var colorConverter = new IndexColorConverter();

		return new DataTemplate(() =>
		{
			var image = new Image
			{
				Aspect = Aspect.AspectFit
			};

			image.SetBinding(AutomationIdProperty, new Binding(nameof(GalleryItem.Index), stringFormat: "Issue19064Item{0}Image"));
			image.SetBinding(HeightRequestProperty, new Binding(nameof(GalleryItem.Index), converter: heightConverter));
			image.SetBinding(WidthRequestProperty, new Binding(nameof(GalleryItem.Index), converter: widthConverter));
			image.SetBinding(Image.SourceProperty, nameof(GalleryItem.Image));

			var border = new Border { Content = image };
			border.SetBinding(HeightRequestProperty, new Binding(nameof(GalleryItem.Index), converter: heightConverter));
			border.SetBinding(WidthRequestProperty, new Binding(nameof(GalleryItem.Index), converter: widthConverter));
			border.SetBinding(BackgroundColorProperty, new Binding(nameof(GalleryItem.Index), converter: colorConverter));
			return border;
		});
	}

	void GenerateItems()
	{
		if (!int.TryParse(_itemCountEntry.Text, out var count))
			return;

		var items = new List<GalleryItem>();
		for (var index = 0; index < count; index++)
			items.Add(new GalleryItem(_images[index % _images.Length], index));

		_itemsCollection.ItemsSource = items;
	}

	void OnSizingStrategyChanged(object sender, EventArgs e)
	{
		if (Enum.TryParse<ItemSizingStrategy>(_sizingPicker.SelectedItem?.ToString(), out var strategy))
		{
			_itemsCollection.ItemSizingStrategy = strategy;
			_explanationLabel.Text = strategy == ItemSizingStrategy.MeasureAllItems
				? "Each item is individually measured."
				: "The first item is measured, and that size is given to all subsequent cells.";
		}
	}

	void OnCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.FirstVisibleItemIndex >= 2)
		{
			_scrolledAway = true;
			_scrollStateLabel.Text = $"AWAY:{e.FirstVisibleItemIndex}";
		}
		else if (_scrolledAway && e.FirstVisibleItemIndex == 0)
		{
			_scrolledAway = false;
			_returnCount++;
			_scrollStateLabel.Text = $"RETURNED:0:{_returnCount}";
		}
	}

	sealed record GalleryItem(string Image, int Index);

	sealed class IndexRequestConverter(int cutoff, int lowValue, int highValue) : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			(int)value < cutoff ? lowValue : highValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotImplementedException();
	}

	sealed class IndexColorConverter : IValueConverter
	{
		readonly Color[] _colors = [Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.BlanchedAlmond];

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			_colors[(int)value % _colors.Length];

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotImplementedException();
	}
}
#endif


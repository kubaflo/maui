using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 19064, "[iOS] ItemSizingStrategy gallery displays items inconsistently", PlatformAffected.iOS)]
public class Issue19064 : ContentPage
{
	readonly SizeConverter _heightConverter = new(3, 50, 150);
	readonly SizeConverter _widthConverter = new(3, 100, 300);
	readonly Label _progressLabel;
	readonly Label _resultLabel;
	Border _firstItemBorder;
	Image _firstItemImage;
	string _observedMismatch;
	bool _baselineCaptured;
	bool _isAwayFromStart;
	bool _returnedToStart;
	int _cycle;

	public Issue19064()
	{
		var collectionView = new CollectionView
		{
			AutomationId = "Issue19064CollectionView",
			ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
			ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Horizontal),
			ItemTemplate = new DataTemplate(CreateItemView),
			ItemsSource = CreateItems()
		};
		collectionView.Scrolled += OnCollectionViewScrolled;

		_progressLabel = new Label
		{
			AutomationId = "Issue19064Progress",
			Text = "Loading gallery"
		};
		_resultLabel = new Label
		{
			AutomationId = "Issue19064Result",
			FontAttributes = FontAttributes.Bold,
			Text = "WAITING"
		};

		var layout = new Grid
		{
			Padding = 12,
			RowSpacing = 8,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};

		layout.Add(new Label
		{
			Text = "Issue 19064 - ItemSizingStrategy",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18
		}, 0, 0);
		layout.Add(_progressLabel, 0, 1);
		layout.Add(_resultLabel, 0, 2);
		layout.Add(collectionView, 0, 3);

		Content = layout;
	}

	View CreateItemView()
	{
		var border = new Border();
		var image = new Image
		{
			Aspect = Aspect.AspectFit
		};

		border.SetBinding(HeightRequestProperty, new Binding(nameof(GalleryItem.Index), converter: _heightConverter));
		border.SetBinding(WidthRequestProperty, new Binding(nameof(GalleryItem.Index), converter: _widthConverter));
		border.SetBinding(BackgroundColorProperty, nameof(GalleryItem.Color));
		border.SetBinding(SemanticProperties.DescriptionProperty, nameof(GalleryItem.BorderDescription));

		image.SetBinding(HeightRequestProperty, new Binding(nameof(GalleryItem.Index), converter: _heightConverter));
		image.SetBinding(WidthRequestProperty, new Binding(nameof(GalleryItem.Index), converter: _widthConverter));
		image.SetBinding(Image.SourceProperty, nameof(GalleryItem.ImageName));
		image.SetBinding(SemanticProperties.DescriptionProperty, nameof(GalleryItem.ImageDescription));

		border.Content = image;
		border.BindingContextChanged += (_, _) => TrackFirstItem(border, image);
		border.Loaded += (_, _) => TrackFirstItem(border, image);
		border.SizeChanged += (_, _) => ObserveFirstItem(border, image);
		image.SizeChanged += (_, _) => ObserveFirstItem(border, image);
		return border;
	}

	void TrackFirstItem(Border border, Image image)
	{
		if (border.BindingContext is not GalleryItem { Index: 0 })
			return;

		_firstItemBorder = border;
		_firstItemImage = image;
		border.Dispatcher.Dispatch(() => ObserveFirstItem(border, image));
	}

	void ObserveFirstItem(Border border, Image image)
	{
		if (border.BindingContext is not GalleryItem { Index: 0 } ||
			border.Width <= 0 || border.Height <= 0 ||
			image.Width <= 0 || image.Height <= 0)
		{
			return;
		}

#if IOS
		if (border.Handler?.PlatformView is not UIKit.UIView platformBorder)
			return;

		var nativeWidth = platformBorder.Frame.Width;
		var nativeHeight = platformBorder.Frame.Height;
#else
		var nativeWidth = border.Width;
		var nativeHeight = border.Height;
#endif

		if (!_baselineCaptured)
		{
			if (!AreClose(border.Width, 100) || !AreClose(border.Height, 50) ||
				!AreClose(nativeWidth, 100) || !AreClose(nativeHeight, 50) ||
				!AreClose(image.Width, 100) || !AreClose(image.Height, 50))
			{
				return;
			}

			_baselineCaptured = true;
			_progressLabel.Text = "READY";
			return;
		}

		if (!_returnedToStart)
			return;

		var measurement = string.Format(
			CultureInfo.InvariantCulture,
			"measured: managed={0:0.#}x{1:0.#}; native={2:0.#}x{3:0.#}; image={4:0.#}x{5:0.#}",
			border.Width, border.Height, nativeWidth, nativeHeight, image.Width, image.Height);

		if (!AreClose(border.Width, 100) || !AreClose(border.Height, 50) ||
			!AreClose(nativeWidth, 100) || !AreClose(nativeHeight, 50) ||
			!AreClose(image.Width, 100) || !AreClose(image.Height, 50))
		{
			_observedMismatch ??= measurement;
		}

		if (_cycle >= 3)
			_resultLabel.Text = _observedMismatch ?? measurement;
	}

	void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (!_baselineCaptured)
			return;

		if (e.FirstVisibleItemIndex >= 2 && !_isAwayFromStart)
		{
			_isAwayFromStart = true;
			_returnedToStart = false;
			_cycle++;
			_progressLabel.Text = $"RIGHT REACHED: cycle {_cycle}";
		}
		else if (e.FirstVisibleItemIndex <= 1 && _isAwayFromStart)
		{
			_isAwayFromStart = false;
			_returnedToStart = true;
			_progressLabel.Text = $"LEFT RETURNED: cycle {_cycle}";

			var border = _firstItemBorder;
			var image = _firstItemImage;
			if (border is not null && image is not null)
				border.Dispatcher.Dispatch(() => ObserveFirstItem(border, image));
		}
	}

	static bool AreClose(double first, double second) =>
		Math.Abs(first - second) <= 1;

	static List<GalleryItem> CreateItems()
	{
		string[] images = ["groceries.png", "shopping_cart.png", "dotnet_bot.svg"];
		Color[] colors = [Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.BlanchedAlmond];
		var items = new List<GalleryItem>();

		for (int index = 0; index < 100; index++)
			items.Add(new GalleryItem(index, images[index % images.Length], colors[index % colors.Length]));

		return items;
	}

	sealed class GalleryItem
	{
		public GalleryItem(int index, string imageName, Color color)
		{
			Index = index;
			ImageName = imageName;
			Color = color;
			BorderDescription = $"Issue19064Border{index}";
			ImageDescription = $"Issue19064Image{index}";
		}

		public int Index { get; }
		public string ImageName { get; }
		public Color Color { get; }
		public string BorderDescription { get; }
		public string ImageDescription { get; }
	}

	sealed class SizeConverter : IValueConverter
	{
		readonly int _cutoff;
		readonly double _lowValue;
		readonly double _highValue;

		public SizeConverter(int cutoff, double lowValue, double highValue)
		{
			_cutoff = cutoff;
			_lowValue = lowValue;
			_highValue = highValue;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			(int)value < _cutoff ? _lowValue : _highValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotImplementedException();
	}
}


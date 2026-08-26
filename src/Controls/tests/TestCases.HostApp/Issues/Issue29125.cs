#if WINDOWS
using Microsoft.Maui.Handlers;
using WBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WSlider = Microsoft.UI.Xaml.Controls.Slider;
using WThumb = Microsoft.UI.Xaml.Controls.Primitives.Thumb;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29125, "[Windows] Slider thumb image is rendered too large", PlatformAffected.WinPhone)]
public class Issue29125 : ContentPage
{
	readonly Slider _issueSlider;
	readonly Button _setThumbButton;
	readonly Button _resultButton;
	WSlider _nativeSlider = null!;
	WThumb _nativeThumb = null!;
	double _defaultThumbWidth;
	double _defaultThumbHeight;
	bool _initialMetricsPublished;
	bool _thumbImageSet;
	bool _nativeImageOpened;
	bool _resultPublished;

	public Issue29125()
	{
		var heading = new Label
		{
			FontSize = 24,
			Text = "Slider thumb image size"
		};

		_issueSlider = new Slider
		{
			AutomationId = "Issue29125Slider",
			Minimum = 0,
			Maximum = 100,
			Value = 25
		};
		_issueSlider.Loaded += OnSliderLoaded;

		_setThumbButton = new Button
		{
			AutomationId = "Issue29125SetThumbImage",
			HorizontalOptions = LayoutOptions.Start,
			IsEnabled = false,
			Text = "Set thumb image"
		};
		_setThumbButton.Clicked += OnSetThumbImageClicked;

		_resultButton = new Button
		{
			AutomationId = "Issue29125Result",
			HorizontalOptions = LayoutOptions.Start,
			Text = "token=-1;source=none"
		};

		var grid = new Grid
		{
			Padding = 32,
			RowDefinitions = new RowDefinitionCollection(
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)),
			RowSpacing = 24
		};

		grid.Add(heading, 0, 0);
		grid.Add(_issueSlider, 0, 1);
		grid.Add(_setThumbButton, 0, 2);
		grid.Add(_resultButton, 0, 3);
		Content = grid;
	}

	void OnSliderLoaded(object sender, EventArgs e)
	{
		if (_issueSlider.Handler is not SliderHandler handler)
		{
			_resultButton.Text = "token=-1;source=handler-missing";
			return;
		}

		_nativeSlider = handler.PlatformView;
		if (!TryFindThumb(_nativeSlider, out var thumb))
		{
			_resultButton.Text = "token=-1;source=thumb-missing";
			return;
		}

		_nativeThumb = thumb;
		_defaultThumbWidth = thumb.Width;
		_defaultThumbHeight = thumb.Height;

		_nativeThumb.SizeChanged += OnNativeThumbSizeChanged;
		_nativeSlider.LayoutUpdated += OnNativeSliderLayoutUpdated;
		TryPublishInitialMetrics();
	}

	void OnSetThumbImageClicked(object sender, EventArgs e)
	{
		_thumbImageSet = true;
		_issueSlider.ThumbImageSource = "dotnet_bot.png";
	}

	void OnNativeThumbSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
	{
		if (!_thumbImageSet || _nativeThumb.Tag is not WBitmapImage bitmapImage)
			return;

		var sourceUri = bitmapImage.UriSource;
		if (sourceUri is null ||
			!sourceUri.OriginalString.EndsWith("/dotnet_bot.png", StringComparison.OrdinalIgnoreCase) ||
			bitmapImage.PixelWidth <= 0 ||
			bitmapImage.PixelHeight <= 0)
		{
			return;
		}

		_nativeImageOpened = true;
	}

	void OnNativeSliderLayoutUpdated(object sender, object e)
	{
		if (!_initialMetricsPublished)
		{
			TryPublishInitialMetrics();
			return;
		}

		if (!_nativeImageOpened || _resultPublished ||
			!TryFindThumb(_nativeSlider, out var currentThumb) ||
			!ReferenceEquals(currentThumb, _nativeThumb) ||
			_nativeThumb.ActualWidth <= 0 ||
			_nativeThumb.ActualHeight <= 0)
		{
			return;
		}

		_resultPublished = true;
		_resultButton.Text = FormattableString.Invariant(
			$"token=1;source=dotnet_bot.png;sameThumb=1;defaultWidth={_defaultThumbWidth:R};defaultHeight={_defaultThumbHeight:R};thumbWidth={_nativeThumb.ActualWidth:R};thumbHeight={_nativeThumb.ActualHeight:R}");
	}

	void TryPublishInitialMetrics()
	{
		if (_initialMetricsPublished ||
			_defaultThumbWidth <= 0 ||
			_defaultThumbHeight <= 0 ||
			_nativeThumb.ActualWidth <= 0 ||
			_nativeThumb.ActualHeight <= 0)
		{
			return;
		}

		_initialMetricsPublished = true;
		_resultButton.Text = FormattableString.Invariant(
			$"token=-1;source=none;defaultWidth={_defaultThumbWidth:R};defaultHeight={_defaultThumbHeight:R};thumbWidth={_nativeThumb.ActualWidth:R};thumbHeight={_nativeThumb.ActualHeight:R}");
		_setThumbButton.IsEnabled = true;
	}

	static bool TryFindThumb(WDependencyObject root, out WThumb thumb)
	{
		for (var index = 0; index < WVisualTreeHelper.GetChildrenCount(root); index++)
		{
			var child = WVisualTreeHelper.GetChild(root, index);
			if (child is WThumb foundThumb)
			{
				thumb = foundThumb;
				return true;
			}

			if (TryFindThumb(child, out thumb))
				return true;
		}

		thumb = null!;
		return false;
	}
}
#endif


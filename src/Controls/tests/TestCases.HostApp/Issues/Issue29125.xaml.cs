#if WINDOWS
using WBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WDependencyProperty = Microsoft.UI.Xaml.DependencyProperty;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WRoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using WSizeChangedEventArgs = Microsoft.UI.Xaml.SizeChangedEventArgs;
using WSlider = Microsoft.UI.Xaml.Controls.Slider;
using WThumb = Microsoft.UI.Xaml.Controls.Primitives.Thumb;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29125, "[Windows] Slider thumb image is rendered too large", PlatformAffected.UWP)]
public partial class Issue29125 : ContentPage
{
#if WINDOWS
	double _defaultThumbWidth;
	double _defaultThumbHeight;
	double _defaultTemplateWidth;
	double _defaultTemplateHeight;
	int _measurementSequence = -1;
	int _defaultSequence = -1;
#endif

	public Issue29125()
	{
		InitializeComponent();
	}

	void OnDefaultSliderLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		if (DefaultSlider.Handler?.PlatformView is WSlider platformSlider &&
			TryFindDescendant(platformSlider, out WThumb thumb))
		{
			thumb.SizeChanged += OnDefaultThumbSizeChanged;
			CaptureDefaultThumb(thumb);
		}
#endif
	}

#if WINDOWS
	void OnDefaultThumbSizeChanged(object sender, WSizeChangedEventArgs e)
	{
		if (sender is WThumb thumb)
			CaptureDefaultThumb(thumb);
	}

	void CaptureDefaultThumb(WThumb thumb)
	{
		if (_defaultSequence >= 0 ||
			thumb.ActualWidth <= 0 ||
			thumb.ActualHeight <= 0 ||
			thumb.Width <= 0 ||
			thumb.Height <= 0)
		{
			return;
		}

		thumb.SizeChanged -= OnDefaultThumbSizeChanged;
		_defaultThumbWidth = thumb.ActualWidth;
		_defaultThumbHeight = thumb.ActualHeight;
		_defaultTemplateWidth = thumb.Width;
		_defaultTemplateHeight = thumb.Height;
		_defaultSequence = ++_measurementSequence;

		var hosted = SliderHost.Children.Count == 1 && ReferenceEquals(SliderHost.Children[0], DefaultSlider);
		MeasurementDetails.Text = FormattableString.Invariant(
			$"phase=default;sequence={_defaultSequence};native=True;thumb=True;hosted={hosted};source={DefaultSlider.ThumbImageSource is not null};value={DefaultSlider.Value:R};defaultWidth={_defaultThumbWidth:R};defaultHeight={_defaultThumbHeight:R};templateWidth={_defaultTemplateWidth:R};templateHeight={_defaultTemplateHeight:R}");
		ResultStatus.Text = "Default thumb measured.";
		ShowSliderButton.IsVisible = true;
	}
#endif

	void OnShowSliderClicked(object sender, EventArgs e)
	{
		var imageSlider = new Slider
		{
			AutomationId = "ImageSlider",
			Value = 0.5,
			ThumbImageSource = "shopping_cart.png"
		};
		imageSlider.HandlerChanged += OnImageSliderHandlerChanged;

		SliderHost.Children.Clear();
		SliderHost.Children.Add(imageSlider);
		ShowSliderButton.IsEnabled = false;
		ResultStatus.Text = "Waiting for the image thumb measurement.";
	}

	void OnImageSliderHandlerChanged(object sender, EventArgs e)
	{
#if WINDOWS
		if (sender is not Slider imageSlider ||
			imageSlider.Handler?.PlatformView is not WSlider platformSlider)
		{
			return;
		}

		if (platformSlider.IsLoaded)
			ObserveImageThumb(platformSlider);
		else
			platformSlider.Loaded += OnImagePlatformSliderLoaded;
#endif
	}

#if WINDOWS
	void OnImagePlatformSliderLoaded(object sender, WRoutedEventArgs e)
	{
		if (sender is WSlider platformSlider)
		{
			platformSlider.Loaded -= OnImagePlatformSliderLoaded;
			ObserveImageThumb(platformSlider);
		}
	}

	void ObserveImageThumb(WSlider platformSlider)
	{
		if (!TryFindDescendant(platformSlider, out WThumb thumb))
			return;

		var imageLoaded = false;
		var published = false;
		long tagCallbackToken = 0;

		thumb.SizeChanged += OnThumbSizeChanged;
		tagCallbackToken = thumb.RegisterPropertyChangedCallback(WFrameworkElement.TagProperty, OnThumbTagChanged);
		ObserveThumbImage();

		void OnThumbTagChanged(WDependencyObject sender, WDependencyProperty property)
		{
			ObserveThumbImage();
		}

		void ObserveThumbImage()
		{
			if (thumb.Tag is not WBitmapImage bitmapImage)
				return;

			if (tagCallbackToken != 0)
			{
				thumb.UnregisterPropertyChangedCallback(WFrameworkElement.TagProperty, tagCallbackToken);
				tagCallbackToken = 0;
			}

			if (bitmapImage.PixelWidth > 0 && bitmapImage.PixelHeight > 0)
			{
				imageLoaded = true;
				TryPublish();
			}
			else
			{
				bitmapImage.ImageOpened += OnImageOpened;
			}
		}

		void OnImageOpened(object sender, WRoutedEventArgs e)
		{
			if (sender is WBitmapImage bitmapImage)
				bitmapImage.ImageOpened -= OnImageOpened;

			imageLoaded = true;
			TryPublish();
		}

		void OnThumbSizeChanged(object sender, WSizeChangedEventArgs e)
		{
			TryPublish();
		}

		void TryPublish()
		{
			if (published ||
				!imageLoaded ||
				thumb.ActualWidth <= 0 ||
				thumb.ActualHeight <= 0 ||
				Math.Abs(thumb.ActualWidth - thumb.Width) > 0.5 ||
				Math.Abs(thumb.ActualHeight - thumb.Height) > 0.5 ||
				SliderHost.Children.Count != 1 ||
				SliderHost.Children[0] is not Slider imageSlider)
			{
				return;
			}

			published = true;
			thumb.SizeChanged -= OnThumbSizeChanged;
			var sourceAttached = imageSlider.ThumbImageSource is not null;
			var sequence = ++_measurementSequence;
			MeasurementDetails.Text = FormattableString.Invariant(
				$"phase=image;sequence={sequence};native=True;thumb=True;hosted=True;source={sourceAttached};value={imageSlider.Value:R};defaultWidth={_defaultThumbWidth:R};defaultHeight={_defaultThumbHeight:R};templateWidth={_defaultTemplateWidth:R};templateHeight={_defaultTemplateHeight:R};imageWidth={thumb.ActualWidth:R};imageHeight={thumb.ActualHeight:R}");
			ResultStatus.Text = "Image thumb measured.";
		}
	}

	static bool TryFindDescendant<T>(WDependencyObject element, out T result)
		where T : WFrameworkElement
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(element);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(element, index);
			if (child is T match)
			{
				result = match;
				return true;
			}

			if (TryFindDescendant(child, out result))
				return true;
		}

		result = default!;
		return false;
	}
#endif
}

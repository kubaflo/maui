#if WINDOWS
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WBitmapImage = Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using WMauiSlider = Microsoft.Maui.Platform.MauiSlider;
using WThumb = Microsoft.UI.Xaml.Controls.Primitives.Thumb;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29125, "[Windows] Slider thumb image renders too large", PlatformAffected.UWP)]
public class Issue29125 : ContentPage
{
	public Issue29125()
	{
		var baselineReport = new Label
		{
			AutomationId = "Issue29125BaselineReport",
			Text = "Waiting for the default thumb"
		};
		var resultReport = new Label
		{
			AutomationId = "Issue29125ResultReport",
			Text = "Sequence=-1"
		};
		var applyButton = new Button
		{
			AutomationId = "Issue29125ApplyButton",
			IsEnabled = false,
			Text = "Apply 512 x 512 thumb image"
		};
		var affectedSlider = new Slider
		{
			AutomationId = "Issue29125Slider",
			Minimum = 0,
			Maximum = 100,
			Value = 50,
			VerticalOptions = LayoutOptions.Center
		};

#if WINDOWS
		var imageApplied = false;
		affectedSlider.Loaded += (_, _) =>
		{
			Dispatcher.Dispatch(() =>
			{
				if (affectedSlider.Handler?.PlatformView is not WMauiSlider platformSlider)
					return;

				platformSlider.ApplyTemplate();
				var thumb = FindFirstThumb(platformSlider);
				if (thumb is null)
					return;

				var defaultWidth = thumb.ActualWidth;
				var defaultHeight = thumb.ActualHeight;
				if (!TryGetStyledSize(thumb, out var styledWidth, out var styledHeight) ||
					defaultWidth <= 0 ||
					defaultHeight <= 0)
					return;

				baselineReport.Text = FormattableString.Invariant(
					$"DefaultWidth={defaultWidth:0.###};DefaultHeight={defaultHeight:0.###};StyledWidth={styledWidth:0.###};StyledHeight={styledHeight:0.###};Pending=-1");
				applyButton.IsEnabled = true;

				var sequence = 0;
				thumb.SizeChanged += (_, _) =>
				{
					var image = FindFirstImage(thumb);
					if (!imageApplied ||
						affectedSlider.ThumbImageSource is null ||
						platformSlider.ThumbImageSource is null ||
						image is null ||
						image.Source is not WBitmapImage bitmap ||
						bitmap.PixelWidth != 512 ||
						bitmap.PixelHeight != 512 ||
						thumb.ActualWidth <= 0 ||
						thumb.ActualHeight <= 0)
					{
						return;
					}

					sequence++;
					resultReport.Text = FormattableString.Invariant(
						$"Sequence={sequence};Source=True;ImageTemplate=True;DefaultWidth={defaultWidth:0.###};DefaultHeight={defaultHeight:0.###};ImageWidth={thumb.ActualWidth:0.###};ImageHeight={thumb.ActualHeight:0.###}");
				};
			});
		};

		applyButton.Clicked += (_, _) =>
		{
			imageApplied = true;
			affectedSlider.ThumbImageSource = "groceries.png";
		};
#endif

		var grid = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Add(new Label { Text = "Windows Slider thumb image sizing", FontSize = 24 });
		grid.Add(baselineReport, row: 1);
		grid.Add(resultReport, row: 2);
		grid.Add(applyButton, row: 3);
		grid.Add(affectedSlider, row: 4);
		Content = grid;
	}

#if WINDOWS
	static bool TryGetStyledSize(WFrameworkElement element, out double width, out double height)
	{
		if (!double.IsNaN(element.Width) &&
			!double.IsNaN(element.Height) &&
			element.Width > 0 &&
			element.Height > 0)
		{
			width = element.Width;
			height = element.Height;
			return true;
		}

		width = 0;
		height = 0;
		return false;
	}

	static WThumb FindFirstThumb(WDependencyObject parent)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(parent, index);
			if (child is WThumb thumb)
				return thumb;

			var descendant = FindFirstThumb(child);
			if (descendant is not null)
				return descendant;
		}

		return null!;
	}

	static WImage FindFirstImage(WDependencyObject parent)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(parent, index);
			if (child is WImage image)
				return image;

			var descendant = FindFirstImage(child);
			if (descendant is not null)
				return descendant;
		}

		return null!;
	}
#endif
}


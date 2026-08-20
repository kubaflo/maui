#if WINDOWS
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WBitmapSource = Microsoft.UI.Xaml.Media.Imaging.BitmapSource;
using WRenderTargetBitmap = Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
using WWindow = Microsoft.UI.Xaml.Window;
#endif

#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "The Shell foreground color is not applied to ToolbarItems", PlatformAffected.UWP)]
public class Issue34071 : Shell
{
	const string ToolbarAutomationId = "ToolbarColorItem";

	readonly BoxView _purpleReference;
	readonly Label _measurementLabel;

	public Issue34071()
	{
		Shell.SetForegroundColor(this, Colors.Purple);

		var toolbarItem = new ToolbarItem
		{
			AutomationId = ToolbarAutomationId,
			IconImageSource = "shopping_cart.png",
			Order = ToolbarItemOrder.Primary
		};

		_purpleReference = new BoxView
		{
			AutomationId = "PurpleReference",
			Color = Colors.Purple,
			HeightRequest = 36,
			WidthRequest = 120,
			HorizontalOptions = LayoutOptions.Center
		};

		_measurementLabel = new Label
		{
			AutomationId = "MeasurementResult",
			Text = "complete=-1",
			HorizontalTextAlignment = TextAlignment.Center
		};

		var measureButton = new Button
		{
			AutomationId = "CheckColorButton",
			Text = "Check toolbar color"
		};
		measureButton.Clicked += async (_, _) => await MeasureRenderedColorsAsync();

		var page = new ContentPage
		{
			Title = "Home",
			ToolbarItems = { toolbarItem },
			Content = new VerticalStackLayout
			{
				Padding = 32,
				Spacing = 18,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Expected toolbar icon color: Purple",
						FontSize = 22,
						HorizontalTextAlignment = TextAlignment.Center
					},
					_purpleReference,
					new Label
					{
						Text = "The toolbar icon above must match the purple reference.",
						HorizontalTextAlignment = TextAlignment.Center
					},
					measureButton,
					_measurementLabel
				}
			}
		};

		Items.Add(page);
		page.Loaded += OnPageLoaded;
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		if (sender is VisualElement element)
			element.Loaded -= OnPageLoaded;

		_ = Dispatcher.DispatchAsync(MeasureRenderedColorsAsync);
	}

	async Task MeasureRenderedColorsAsync()
	{
		_measurementLabel.Text = "complete=-1";
		string sourceDescription = CurrentPage?.ToolbarItems[0].IconImageSource?.ToString() ?? "NoImageSource";
		string sourceName = sourceDescription.EndsWith("shopping_cart.png", StringComparison.Ordinal)
			? "shopping_cart.png"
			: sourceDescription;
		string foreground = Shell.GetForegroundColor(this) == Colors.Purple ? "Purple" : "NotPurple";

#if WINDOWS
		if (Window?.Handler?.PlatformView is not WWindow platformWindow ||
			platformWindow.Content is not WDependencyObject root)
		{
			_measurementLabel.Text = $"complete=1|source={sourceName}|foreground={foreground}|toolbar=WindowUnavailable";
			return;
		}

		var toolbarButton = FindElementByAutomationId(root, ToolbarAutomationId) as WAppBarButton;
		if (toolbarButton is null)
		{
			_measurementLabel.Text = $"complete=1|source={sourceName}|foreground={foreground}|toolbar=ToolbarItemUnavailable";
			return;
		}

		string toolbarIdentity = WAutomationProperties.GetAutomationId(toolbarButton);

		if (toolbarButton.Content is not WImage iconImage ||
			_purpleReference.Handler?.PlatformView is not WFrameworkElement reference)
		{
			_measurementLabel.Text = $"complete=1|source={sourceName}|foreground={foreground}|toolbar=RenderedSurfaceUnavailable";
			return;
		}

		await WaitForImageOpenedAsync(iconImage);
		var iconResult = await RenderAndCountPurplePixelsAsync(iconImage);
		var referenceResult = await RenderAndCountPurplePixelsAsync(reference);

		_measurementLabel.Text =
			$"complete=1|source={sourceName}|foreground={foreground}|toolbar={toolbarIdentity}" +
			$"|iconWidth={iconResult.Width}|iconHeight={iconResult.Height}|iconOpaque={iconResult.OpaquePixels}|iconPurple={iconResult.PurplePixels}" +
			$"|referenceWidth={referenceResult.Width}|referenceHeight={referenceResult.Height}|referenceOpaque={referenceResult.OpaquePixels}|referencePurple={referenceResult.PurplePixels}";
#endif
	}

#if WINDOWS
	static WFrameworkElement FindElementByAutomationId(WDependencyObject root, string automationId)
	{
		if (root is WFrameworkElement element &&
			WAutomationProperties.GetAutomationId(element) == automationId)
		{
			return element;
		}

		int childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount; i++)
		{
			var match = FindElementByAutomationId(WVisualTreeHelper.GetChild(root, i), automationId);
			if (match is not null)
				return match;
		}

		return null;
	}

	static async Task WaitForImageOpenedAsync(WImage image)
	{
		if (IsDecoded())
			return;

		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		image.ImageOpened += OnOpened;
		image.ImageFailed += OnFailed;

		try
		{
			if (!IsDecoded())
				await completion.Task;
		}
		finally
		{
			image.ImageOpened -= OnOpened;
			image.ImageFailed -= OnFailed;
		}

		bool IsDecoded() =>
			image.Source is WBitmapSource { PixelWidth: > 0, PixelHeight: > 0 };

		void OnOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
			completion.TrySetResult(true);

		void OnFailed(object sender, Microsoft.UI.Xaml.ExceptionRoutedEventArgs e) =>
			completion.TrySetException(new InvalidOperationException($"The packaged toolbar image failed to decode: {e.ErrorMessage}"));
	}

	static async Task<(int Width, int Height, int OpaquePixels, int PurplePixels)> RenderAndCountPurplePixelsAsync(WFrameworkElement element)
	{
		var bitmap = new WRenderTargetBitmap();
		await bitmap.RenderAsync(element);
		var pixelBuffer = await bitmap.GetPixelsAsync();
		var pixels = new byte[checked((int)pixelBuffer.Length)];
		using (var reader = global::Windows.Storage.Streams.DataReader.FromBuffer(pixelBuffer))
			reader.ReadBytes(pixels);

		int opaquePixels = 0;
		int purplePixels = 0;
		for (int i = 0; i <= pixels.Length - 4; i += 4)
		{
			byte blue = pixels[i];
			byte green = pixels[i + 1];
			byte red = pixels[i + 2];
			byte alpha = pixels[i + 3];

			if (alpha > 100)
				opaquePixels++;

			if (alpha > 100 &&
				red > green + 30 &&
				blue > green + 30 &&
				Math.Abs(red - blue) < 80)
			{
				purplePixels++;
			}
		}

		return (bitmap.PixelWidth, bitmap.PixelHeight, opaquePixels, purplePixels);
	}
#endif
}
#endif

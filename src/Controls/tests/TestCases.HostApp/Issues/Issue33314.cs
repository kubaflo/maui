#if WINDOWS
using Microsoft.Maui.Handlers;
using WCompositionTarget = Microsoft.UI.Xaml.Media.CompositionTarget;
using WFocusState = Microsoft.UI.Xaml.FocusState;
using WKeyRoutedEventArgs = Microsoft.UI.Xaml.Input.KeyRoutedEventArgs;
using WRenderTargetBitmap = Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
using WDataReader = Windows.Storage.Streams.DataReader;
using WUISettings = Windows.UI.ViewManagement.UISettings;
using WVirtualKey = Windows.System.VirtualKey;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33314, "Editor caret becomes a dot after clearing text and hiding adjacent content", PlatformAffected.UWP)]
public class Issue33314 : ContentPage
{
#if WINDOWS
	WTextBox _nativeEditor = null!;
#endif

	public Issue33314()
	{
		var affectedEditor = new Issue33314Editor
		{
			AutomationId = "AffectedEditor"
		};

		var cancelView = new ContentView
		{
			AutomationId = "CancelView",
			IsVisible = false,
			Content = new Label
			{
				Text = "Cancel",
				VerticalOptions = LayoutOptions.Center
			}
		};

		var baselineReady = new Label
		{
			AutomationId = "BaselineReady",
			IsVisible = false
		};

		var baselineMetrics = new Label
		{
			AutomationId = "BaselineMetrics",
			Text = "generation=-1"
		};

		var postCaptureReady = new Label
		{
			AutomationId = "PostCaptureReady",
			IsVisible = false
		};

		var postMetrics = new Label
		{
			AutomationId = "PostMetrics",
			Text = "generation=-1"
		};

		var triggerState = new Label
		{
			AutomationId = "TriggerState",
			Text = "Waiting for Shift"
		};

		affectedEditor.TextChanged += (_, e) =>
			cancelView.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);

#if WINDOWS
		affectedEditor.HandlerChanged += (_, _) =>
		{
			if (_nativeEditor is not null)
				_nativeEditor.KeyDown -= OnNativeEditorKeyDown;

			if (affectedEditor.Handler is not EditorHandler handler)
				return;

			_nativeEditor = handler.PlatformView;
			_nativeEditor.KeyDown += OnNativeEditorKeyDown;
		};

		affectedEditor.Focused += (_, _) =>
		{
			if (!baselineReady.IsVisible && _nativeEditor is not null)
				BeginCaretCapture(_nativeEditor, 0, baselineMetrics, baselineReady);
		};

		void OnNativeEditorKeyDown(object sender, WKeyRoutedEventArgs e)
		{
			if (e.Key != WVirtualKey.Shift)
				return;

			affectedEditor.Text = string.Empty;
			Dispatcher.Dispatch(() =>
			{
				bool triggerCompleted =
					_nativeEditor.FocusState != WFocusState.Unfocused &&
					_nativeEditor.SelectionStart == 0 &&
					_nativeEditor.SelectionLength == 0 &&
					string.IsNullOrEmpty(_nativeEditor.Text) &&
					!cancelView.IsVisible;

				triggerState.Text = triggerCompleted
					? "Shift key received; text cleared; cancel hidden"
					: "Shift key received; trigger state incomplete";

				if (triggerCompleted)
					BeginCaretCapture(_nativeEditor, 1, postMetrics, postCaptureReady);
			});
		}
#endif

		var reportedGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		reportedGrid.Add(affectedEditor, 0);
		reportedGrid.Add(cancelView, 1);

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Issue 33314: enter text, then press Shift",
					FontSize = 20
				},
				reportedGrid,
				new Label
				{
					Text = "The focused Editor remains visible so its caret can be observed."
				},
				triggerState,
				baselineMetrics,
				baselineReady,
				postMetrics,
				postCaptureReady
			}
		};
	}

#if WINDOWS
	static void BeginCaretCapture(WTextBox nativeEditor, int generation, Label metricsLabel, Label readyLabel)
	{
		byte[] firstPixels = Array.Empty<byte>();
		int firstWidth = 0;
		int firstHeight = 0;
		int renderCount = 0;
		bool captureInProgress = false;
		EventHandler<object> rendering = null!;

		rendering = async (_, _) =>
		{
			renderCount++;
			if (captureInProgress || renderCount < 18 || renderCount % 6 != 0)
				return;

			captureInProgress = true;
			var bitmap = new WRenderTargetBitmap();
			await bitmap.RenderAsync(nativeEditor);
			var pixelBuffer = await bitmap.GetPixelsAsync();
			byte[] pixels = new byte[pixelBuffer.Length];
			using (var reader = WDataReader.FromBuffer(pixelBuffer))
				reader.ReadBytes(pixels);
			int width = bitmap.PixelWidth;
			int height = bitmap.PixelHeight;

			if (firstPixels.Length == 0 || width != firstWidth || height != firstHeight)
			{
				firstPixels = pixels;
				firstWidth = width;
				firstHeight = height;
				captureInProgress = false;
				return;
			}

			if (TryMeasureDifference(firstPixels, pixels, width, height, out int caretWidth, out int caretHeight, out int changedPixels))
			{
				WCompositionTarget.Rendering -= rendering;
				var xamlRoot = nativeEditor.XamlRoot;
				if (xamlRoot is null)
				{
					metricsLabel.Text = $"generation={generation};capture=failed;reason=no-xaml-root;frameWidth={width};frameHeight={height}";
					readyLabel.Text = $"Capture generation {generation} failed";
					readyLabel.IsVisible = true;
					return;
				}

				double rasterScale = xamlRoot.RasterizationScale;
				double textScale = nativeEditor.IsTextScaleFactorEnabled ? new WUISettings().TextScaleFactor : 1d;
				int minimumHeight = Math.Max(3, (int)Math.Ceiling(nativeEditor.FontSize * rasterScale * textScale * 0.5));
				metricsLabel.Text =
					$"generation={generation};height={caretHeight};width={caretWidth};pixels={changedPixels};minimum={minimumHeight};scale={rasterScale:F2};textScale={textScale:F2};frameWidth={width};frameHeight={height};theme={nativeEditor.ActualTheme}";
				readyLabel.Text = $"Capture generation {generation} ready";
				readyLabel.IsVisible = true;
				return;
			}

			if (renderCount >= 240)
			{
				WCompositionTarget.Rendering -= rendering;
				metricsLabel.Text = $"generation={generation};capture=failed;frameWidth={width};frameHeight={height}";
				readyLabel.Text = $"Capture generation {generation} failed";
				readyLabel.IsVisible = true;
				return;
			}

			captureInProgress = false;
		};

		WCompositionTarget.Rendering += rendering;
	}

	static bool TryMeasureDifference(byte[] first, byte[] second, int width, int height, out int measuredWidth, out int measuredHeight, out int changedPixels)
	{
		int minX = width;
		int minY = height;
		int maxX = -1;
		int maxY = -1;
		changedPixels = 0;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int offset = (y * width + x) * 4;
				bool changed =
					Math.Abs(first[offset] - second[offset]) > 16 ||
					Math.Abs(first[offset + 1] - second[offset + 1]) > 16 ||
					Math.Abs(first[offset + 2] - second[offset + 2]) > 16 ||
					Math.Abs(first[offset + 3] - second[offset + 3]) > 16;

				if (!changed)
					continue;

				changedPixels++;
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		measuredWidth = maxX >= minX ? maxX - minX + 1 : 0;
		measuredHeight = maxY >= minY ? maxY - minY + 1 : 0;
		return changedPixels > 0;
	}
#endif
}

public class Issue33314Editor : Editor
{
}


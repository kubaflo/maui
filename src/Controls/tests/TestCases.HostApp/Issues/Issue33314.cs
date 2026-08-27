#if WINDOWS
using WCompositionTarget = Microsoft.UI.Xaml.Media.CompositionTarget;
using WDataReader = Windows.Storage.Streams.DataReader;
using WKeyRoutedEventArgs = Microsoft.UI.Xaml.Input.KeyRoutedEventArgs;
using WRenderTargetBitmap = Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33314, "Editor caret renders as a dot after clearing text and hiding adjacent content", PlatformAffected.UWP)]
public class Issue33314 : ContentPage
{
	const int CaptureFrameCount = 72;
	const int SettlingFrameCount = 4;

	readonly Issue33314Editor _issueEditor;
	readonly ContentView _cancelView;
	readonly Label _baselineStatus;
	readonly Label _transitionStatus;
	readonly Label _postTriggerStatus;

	int _textChangedSequence;
	int _emptyTextChangedSequence = -1;
	int _shiftKeyDownCount;
	int _baselineCaretHeight;
	bool _baselineCaptureStarted;
	bool _postTriggerCaptureStarted;

	public Issue33314()
	{
		_issueEditor = new Issue33314Editor
		{
			AutomationId = "IssueEditor",
			Placeholder = "Enter text"
		};
		_issueEditor.Focused += OnEditorFocused;
		_issueEditor.TextChanged += OnEditorTextChanged;
		_issueEditor.ShiftKeyDown = OnShiftKeyDown;
		_issueEditor.ShiftClearCompleted = OnShiftClearCompleted;

		_cancelView = new ContentView
		{
			AutomationId = "CancelView",
			IsVisible = false,
			Content = new Label
			{
				Text = "Cancel",
				VerticalOptions = LayoutOptions.Center
			}
		};

		var editorGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			ColumnSpacing = 12
		};
		editorGrid.Add(_issueEditor, 0);
		editorGrid.Add(_cancelView, 1);

		_baselineStatus = new Label
		{
			AutomationId = "BaselineCaretStatus",
			Text = "Pending"
		};
		_transitionStatus = new Label
		{
			AutomationId = "TransitionStatus",
			Text = "Shift=-1; EmptySequence=-1"
		};
		_postTriggerStatus = new Label
		{
			AutomationId = "PostTriggerCaretStatus",
			Text = "Pending"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Type text in the Editor, then press Shift. The empty Editor must keep a full-height vertical caret.",
					FontSize = 18
				},
				editorGrid,
				_baselineStatus,
				_transitionStatus,
				_postTriggerStatus
			}
		};
	}

	async void OnEditorFocused(object sender, FocusEventArgs e)
	{
		if (_baselineCaptureStarted)
			return;

		_baselineCaptureStarted = true;
		var measurement = await MeasureCaretAsync();
		_baselineCaretHeight = measurement.CaretHeight;
		_baselineStatus.Text = FormatMeasurement(measurement, measurement.CaretHeight >= measurement.Threshold);
	}

	void OnEditorTextChanged(object sender, TextChangedEventArgs e)
	{
		_textChangedSequence++;
		_cancelView.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);

		if (_shiftKeyDownCount > 0 && string.IsNullOrEmpty(e.NewTextValue))
			_emptyTextChangedSequence = _textChangedSequence;
	}

	void OnShiftKeyDown()
	{
		_shiftKeyDownCount++;
	}

	async void OnShiftClearCompleted()
	{
		if (_postTriggerCaptureStarted)
			return;

		_postTriggerCaptureStarted = true;
		var platformEditor = GetPlatformEditor();
		var transitionState =
			$"Shift={_shiftKeyDownCount}; EmptySequence={_emptyTextChangedSequence}; TextEmpty={string.IsNullOrEmpty(platformEditor.Text)}; Selection={platformEditor.SelectionStart}; Focused={platformEditor.FocusState != Microsoft.UI.Xaml.FocusState.Unfocused}; CancelVisible={_cancelView.IsVisible}";

		var measurement = await MeasureCaretAsync();
		var minimumConsistentHeight = (int)Math.Ceiling(_baselineCaretHeight * 0.6);
		var passed = measurement.CaretHeight >= measurement.Threshold &&
			measurement.CaretHeight >= minimumConsistentHeight;
		_transitionStatus.Text = transitionState;
		_postTriggerStatus.Text = FormatMeasurement(measurement, passed);
	}

	async Task<Issue33314CaretMeasurement> MeasureCaretAsync()
	{
		var platformEditor = GetPlatformEditor();
		for (int frame = 0; frame < SettlingFrameCount; frame++)
			await WaitForNextFrameAsync();

		int width = 0;
		int height = 0;
		int[] minimumIntensity = [];
		int[] maximumIntensity = [];

		for (int frame = 0; frame < CaptureFrameCount; frame++)
		{
			await WaitForNextFrameAsync();
			var bitmap = new WRenderTargetBitmap();
			await bitmap.RenderAsync(platformEditor);
			var pixelBuffer = await bitmap.GetPixelsAsync();
			var pixels = new byte[pixelBuffer.Length];
			using (var reader = WDataReader.FromBuffer(pixelBuffer))
				reader.ReadBytes(pixels);

			if (frame == 0)
			{
				width = bitmap.PixelWidth;
				height = bitmap.PixelHeight;
				if (width <= 0 || height <= 0 || pixels.Length != width * height * 4)
					throw new InvalidOperationException($"Invalid native Editor frame {width}x{height} with {pixels.Length} bytes.");

				minimumIntensity = Enumerable.Repeat(int.MaxValue, width * height).ToArray();
				maximumIntensity = new int[width * height];
			}
			else if (bitmap.PixelWidth != width || bitmap.PixelHeight != height || pixels.Length != width * height * 4)
			{
				throw new InvalidOperationException("The native Editor frame changed size during caret capture.");
			}

			for (int pixel = 0; pixel < width * height; pixel++)
			{
				int byteOffset = pixel * 4;
				int intensity = pixels[byteOffset] + pixels[byteOffset + 1] + pixels[byteOffset + 2];
				minimumIntensity[pixel] = Math.Min(minimumIntensity[pixel], intensity);
				maximumIntensity[pixel] = Math.Max(maximumIntensity[pixel], intensity);
			}
		}

		int caretHeight = FindTallestBlinkingRun(minimumIntensity, maximumIntensity, width, height);
		double scale = platformEditor.XamlRoot.RasterizationScale;
		int threshold = Math.Max(8, (int)Math.Floor(platformEditor.FontSize * scale * 0.6));
		return new Issue33314CaretMeasurement(caretHeight, threshold, width, height, platformEditor.FontSize, scale);
	}

	static int FindTallestBlinkingRun(int[] minimumIntensity, int[] maximumIntensity, int width, int height)
	{
		int tallestRun = 0;
		for (int x = 0; x < width; x++)
		{
			int currentRun = 0;
			for (int y = 0; y < height; y++)
			{
				bool changed = false;
				for (int sampleX = Math.Max(0, x - 1); sampleX <= Math.Min(width - 1, x + 1); sampleX++)
				{
					int index = (y * width) + sampleX;
					if (maximumIntensity[index] - minimumIntensity[index] >= 90)
					{
						changed = true;
						break;
					}
				}

				currentRun = changed ? currentRun + 1 : 0;
				tallestRun = Math.Max(tallestRun, currentRun);
			}
		}

		return tallestRun;
	}

	static Task WaitForNextFrameAsync()
	{
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler<object> renderingHandler = null!;
		renderingHandler = delegate
		{
			WCompositionTarget.Rendering -= renderingHandler;
			completion.TrySetResult();
		};
		WCompositionTarget.Rendering += renderingHandler;
		return completion.Task;
	}

	WTextBox GetPlatformEditor()
	{
		if (_issueEditor.Handler?.PlatformView is WTextBox platformEditor)
			return platformEditor;

		throw new InvalidOperationException("The MAUI Editor is not attached to its standard WinUI TextBox.");
	}

	static string FormatMeasurement(Issue33314CaretMeasurement measurement, bool passed) =>
		$"MEASURED: Pass={passed}; CaretHeight={measurement.CaretHeight}; Threshold={measurement.Threshold}; Frame={measurement.Width}x{measurement.Height}; FontSize={measurement.FontSize:F2}; Scale={measurement.Scale:F2}";
}

readonly record struct Issue33314CaretMeasurement(
	int CaretHeight,
	int Threshold,
	int Width,
	int Height,
	double FontSize,
	double Scale);

public class Issue33314Editor : Editor
{
	WTextBox _attachedEditor = null!;

	public Action ShiftKeyDown { get; set; } = delegate { };

	public Action ShiftClearCompleted { get; set; } = delegate { };

	protected override void OnHandlerChanged()
	{
		if (_attachedEditor is not null)
			_attachedEditor.KeyDown -= OnPlatformKeyDown;

		base.OnHandlerChanged();

		if (Handler?.PlatformView is WTextBox platformEditor)
		{
			_attachedEditor = platformEditor;
			_attachedEditor.KeyDown += OnPlatformKeyDown;
		}
	}

	void OnPlatformKeyDown(object sender, WKeyRoutedEventArgs e)
	{
		if (e.Key != Windows.System.VirtualKey.Shift)
			return;

		ShiftKeyDown();
		Text = string.Empty;
		ShiftClearCompleted();
	}
}
#endif


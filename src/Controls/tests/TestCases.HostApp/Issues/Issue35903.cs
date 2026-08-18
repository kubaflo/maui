#if WINDOWS
using System.Globalization;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35903, "Editor control does not show all the text after increasing its height on Windows", PlatformAffected.UWP)]
public class Issue35903 : ContentPage
{
	const double SmallWindowWidth = 430;
	const double SmallWindowHeight = 300;
	const double LargeWindowWidth = 900;
	const double LargeWindowHeight = 700;
	const string EditorText =
		"Lorem ipsum dolor sit amet, consectetur adipiscing elit.\n" +
		"Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\n" +
		"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n" +
		"Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.\n" +
		"Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

	readonly Editor _issueEditor;
	readonly Label _resultLabel;

	WTextBox _initialTextBox;
	WScrollViewer _initialViewport;
	bool _trackingSizeChanges;
	bool _shrinkRequested;
	bool _expandRequested;
	bool _largeWindowRequested;
	bool _completed;
	int _editorSizeCallbackCount = -1;
	int _smallEditorSizeCallbackCount = -1;
	double _smallEditorHeight = -1;
	double _smallViewportHeight = -1;
	double _rasterizationScale = -1;

	public Issue35903()
	{
		_issueEditor = new Editor
		{
			AutomationId = "IssueEditor",
			Text = EditorText
		};
		_issueEditor.SizeChanged += OnEditorSizeChanged;

		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "NO BUG:"
		};

		var shrinkButton = new Button
		{
			AutomationId = "ShrinkButton",
			Text = "Shrink window"
		};
		shrinkButton.Clicked += OnShrinkClicked;

		var expandButton = new Button
		{
			AutomationId = "ExpandButton",
			Text = "Expand window"
		};
		expandButton.Clicked += OnExpandClicked;

		var buttons = new HorizontalStackLayout
		{
			Spacing = 6,
			Children =
			{
				shrinkButton,
				expandButton
			}
		};

		var controls = new VerticalStackLayout
		{
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.End,
			Margin = 12,
			Spacing = 6,
			Children =
			{
				_resultLabel,
				buttons
			}
		};

		Content = new Grid
		{
			Children =
			{
				_issueEditor,
				controls
			}
		};
	}

	void OnShrinkClicked(object sender, EventArgs e)
	{
		if (_shrinkRequested || Window is null)
			return;

		if (_issueEditor.Text != EditorText ||
			_issueEditor.Handler is not EditorHandler editorHandler ||
			editorHandler.PlatformView is not WTextBox textBox)
		{
			CompleteWithSetupFailure("Editor text or EditorHandler platform view was unavailable before resizing.");
			return;
		}

		textBox.ApplyTemplate();
		var viewport = FindTextViewport(textBox);
		if (viewport is null || textBox.XamlRoot is null || textBox.ActualHeight <= 0 || viewport.ViewportHeight <= 0)
		{
			CompleteWithSetupFailure("The applied TextBox template did not expose a positive native text viewport.");
			return;
		}

		_initialTextBox = textBox;
		_initialViewport = viewport;
		_rasterizationScale = textBox.XamlRoot.RasterizationScale;
		if (_rasterizationScale <= 0)
		{
			CompleteWithSetupFailure("The initial rasterization scale was invalid.");
			return;
		}

		if (!HasExpectedText(textBox.Text))
		{
			CompleteWithSetupFailure("The initial native text did not match the Editor text.");
			return;
		}

		_editorSizeCallbackCount = -1;
		_trackingSizeChanges = true;
		_shrinkRequested = true;
		textBox.LayoutUpdated += OnNativeLayoutUpdated;
		Window.Width = SmallWindowWidth;
		Window.Height = SmallWindowHeight;
	}

	void OnExpandClicked(object sender, EventArgs e)
	{
		if (!_shrinkRequested || _completed)
			return;

		_expandRequested = true;
		TryAdvanceResize();
	}

	void OnEditorSizeChanged(object sender, EventArgs e)
	{
		if (_trackingSizeChanges)
			_editorSizeCallbackCount++;
	}

	void OnNativeLayoutUpdated(object sender, object e)
	{
		TryAdvanceResize();
	}

	void TryAdvanceResize()
	{
		if (_completed || Window is null || _initialTextBox is null || _initialViewport is null)
			return;

		double tolerance = 2 / _rasterizationScale;
		if (!_largeWindowRequested)
		{
			bool smallLayoutCompleted =
				Math.Abs(Window.Width - SmallWindowWidth) <= 2 &&
				Math.Abs(Window.Height - SmallWindowHeight) <= 2 &&
				_issueEditor.Height > 0 &&
				_initialViewport.ViewportHeight > 0 &&
				_editorSizeCallbackCount >= 0;

			if (!smallLayoutCompleted)
				return;

			if (_smallViewportHeight < 0)
			{
				_smallEditorHeight = _issueEditor.Height;
				_smallViewportHeight = _initialViewport.ViewportHeight;
				_smallEditorSizeCallbackCount = _editorSizeCallbackCount;
				_resultLabel.Text = "SHRUNK:";
			}

			if (!_expandRequested)
				return;

			_largeWindowRequested = true;
			Window.Width = LargeWindowWidth;
			Window.Height = LargeWindowHeight;
			return;
		}

		if (Math.Abs(Window.Width - LargeWindowWidth) > 2 ||
			Math.Abs(Window.Height - LargeWindowHeight) > 2 ||
			_issueEditor.Height <= _smallEditorHeight + 100 ||
			_editorSizeCallbackCount <= _smallEditorSizeCallbackCount)
		{
			return;
		}

		Complete(tolerance);
	}

	void Complete(double tolerance)
	{
		_completed = true;
		_initialTextBox.LayoutUpdated -= OnNativeLayoutUpdated;

		var currentTextBox = (_issueEditor.Handler as EditorHandler)?.PlatformView;
		var currentViewport = currentTextBox is null ? null : FindTextViewport(currentTextBox);
		double clientHeight = currentTextBox is null
			? -1
			: currentTextBox.ActualHeight -
				currentTextBox.BorderThickness.Top -
				currentTextBox.BorderThickness.Bottom -
				currentTextBox.Padding.Top -
				currentTextBox.Padding.Bottom;
		double largeViewportHeight = currentViewport?.ViewportHeight ?? -1;

		bool identitiesRetained =
			ReferenceEquals(currentTextBox, _initialTextBox) &&
			ReferenceEquals(currentViewport, _initialViewport);
		bool textRetained =
			currentTextBox is not null &&
			HasExpectedText(currentTextBox.Text) &&
			HasExpectedText(_issueEditor.Text);
		bool viewportExpanded =
			largeViewportHeight > _smallViewportHeight + tolerance &&
			Math.Abs(clientHeight - largeViewportHeight) <= tolerance;
		bool passed = identitiesRetained && textRetained && viewportExpanded;

		_resultLabel.Text = string.Format(
			CultureInfo.InvariantCulture,
			"{0}: smallEditor={1:F2}; largeEditor={2:F2}; textBox={3:F2}; smallViewport={4:F2}; largeViewport={5:F2}; client={6:F2}; window={7:F2}x{8:F2}; scale={9:F2}; callbacks={10}->{11}; tolerance={12:F2}; sameViews={13}; textRetained={14}",
			passed ? "PASS" : "FAIL",
			_smallEditorHeight,
			_issueEditor.Height,
			currentTextBox?.ActualHeight ?? -1,
			_smallViewportHeight,
			largeViewportHeight,
			clientHeight,
			Window.Width,
			Window.Height,
			_rasterizationScale,
			_smallEditorSizeCallbackCount,
			_editorSizeCallbackCount,
			tolerance,
			identitiesRetained,
			textRetained);
	}

	void CompleteWithSetupFailure(string message)
	{
		_completed = true;
		_resultLabel.Text = $"SETUP: {message}";
	}

	static bool HasExpectedText(string text) =>
		text is not null && text.ReplaceLineEndings("\n") == EditorText;

	static WScrollViewer FindTextViewport(DependencyObject parent)
	{
		int childCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childCount; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is WScrollViewer viewport && viewport.Name == "ContentElement")
				return viewport;

			var descendant = FindTextViewport(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}
}
#endif

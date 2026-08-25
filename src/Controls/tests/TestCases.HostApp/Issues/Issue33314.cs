#if WINDOWS
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33314, "Editor caret collapses after clearing text while hiding adjacent content", PlatformAffected.UWP)]
public class Issue33314 : ContentPage
{
	readonly Issue33314ShiftClearingEditor _issueEditor;
	readonly ContentView _cancelContent;
	readonly Label _measurementLabel;
	readonly Label _keyDownTokenLabel;
#if WINDOWS
	int _measurementSequence;
	bool _shiftTriggered;
#endif

	public Issue33314()
	{
		_issueEditor = new Issue33314ShiftClearingEditor
		{
			AutomationId = "Issue33314Editor"
		};
		_issueEditor.TextChanged += OnEditorTextChanged;
#if WINDOWS
		_issueEditor.ShiftPressed += OnShiftPressed;
#endif

		_cancelContent = new ContentView
		{
			AutomationId = "Issue33314CancelContent",
			IsVisible = false,
			Content = new Label
			{
				Text = "Cancel",
				VerticalOptions = LayoutOptions.Center
			}
		};

		var editorRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		editorRow.Add(_issueEditor, 0);
		editorRow.Add(_cancelContent, 1);

		_measurementLabel = new Label
		{
			AutomationId = "Issue33314Measurement",
			Text = "Phase=Pending;Sequence=-1"
		};

		_keyDownTokenLabel = new Label
		{
			AutomationId = "Issue33314KeyDownToken",
			Text = "KeyDown=-1"
		};

		var instructions = new Label
		{
			Text = "Type in the Editor, then press Shift. The caret should remain a full-height vertical line."
		};

		var layout = new Grid
		{
			Padding = 24,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		layout.Add(editorRow, 0, 0);
		layout.Add(instructions, 0, 1);
		layout.Add(_measurementLabel, 0, 2);
		layout.Add(_keyDownTokenLabel, 0, 3);
		Content = layout;
	}

	void OnEditorTextChanged(object sender, TextChangedEventArgs e)
	{
		_cancelContent.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
#if WINDOWS
		Dispatcher.Dispatch(CaptureNativeMeasurement);
#endif
	}

#if WINDOWS
	void OnShiftPressed(object sender, EventArgs e)
	{
		_shiftTriggered = true;
		_keyDownTokenLabel.Text = "KeyDown=1";
	}

	void CaptureNativeMeasurement()
	{
		if (_issueEditor.Handler?.PlatformView is not WTextBox textBox)
		{
			return;
		}

		var contentScrollViewer = FindContentScrollViewer(textBox);
		if (contentScrollViewer?.Content is not WFrameworkElement contentElement)
		{
			return;
		}

		_measurementSequence++;
		string phase = _shiftTriggered ? "Post" : "Clean";
		int textBoxIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(textBox);
		int contentIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(contentElement);
		int textLength = _issueEditor.Text?.Length ?? 0;
		_measurementLabel.Text = FormattableString.Invariant(
			$"Phase={phase};Sequence={_measurementSequence};TextBoxId={textBoxIdentity};ContentId={contentIdentity};TextLength={textLength};FontSize={textBox.FontSize};Height={contentElement.ActualHeight};CancelVisible={_cancelContent.IsVisible}");
	}

	static WScrollViewer FindContentScrollViewer(WDependencyObject element)
	{
		int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount; i++)
		{
			WDependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
			if (child is WScrollViewer scrollViewer && scrollViewer.Name == "ContentElement")
			{
				return scrollViewer;
			}

			var descendant = FindContentScrollViewer(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}
#endif
}

public class Issue33314ShiftClearingEditor : Editor
{
#if WINDOWS
	public event EventHandler ShiftPressed;

	WTextBox _platformTextBox;

	protected override void OnHandlerChanged()
	{
		if (_platformTextBox is not null)
		{
			_platformTextBox.KeyDown -= OnPlatformKeyDown;
		}

		base.OnHandlerChanged();
		_platformTextBox = Handler?.PlatformView as WTextBox;
		if (_platformTextBox is not null)
		{
			_platformTextBox.KeyDown += OnPlatformKeyDown;
		}
	}

	void OnPlatformKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.Shift)
		{
			ShiftPressed?.Invoke(this, EventArgs.Empty);
			Text = string.Empty;
		}
	}
#endif
}


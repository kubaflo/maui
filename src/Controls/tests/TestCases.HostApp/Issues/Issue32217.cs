#if IOS
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32217, "RTL Editor spaces render inconsistently", PlatformAffected.iOS)]
public class Issue32217 : ContentPage
{
	readonly Editor _rtlEditor;
	readonly Label _caretMeasurementLabel;
#if IOS
	int _measurementSequence;
#endif

	public Issue32217()
	{
		_caretMeasurementLabel = new Label
		{
			AutomationId = "CaretMeasurement",
			FontAttributes = FontAttributes.Bold,
			MaxLines = 1,
			Text = "-1|sentinel"
		};

		_rtlEditor = new Editor
		{
			AutomationId = "RtlEditor",
			FlowDirection = FlowDirection.RightToLeft
		};
		_rtlEditor.TextChanged += OnEditorTextChanged;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label { FontSize = 20, Text = "RTL Editor space rendering" },
				new Label { Text = "Expected visible phrase: This is a test" },
				_caretMeasurementLabel,
				new Label { Text = "Type this phrase in the right-to-left Editor: This is a test" },
				_rtlEditor
			}
		};
	}

	void OnEditorTextChanged(object sender, TextChangedEventArgs e)
	{
#if IOS
		var sequence = ++_measurementSequence;
		var expectedText = e.NewTextValue ?? string.Empty;
		Foundation.NSRunLoop.Main.BeginInvokeOnMainThread(() => CaptureCaretMeasurement(sequence, expectedText));
#endif
	}

#if IOS
	void CaptureCaretMeasurement(int sequence, string expectedText)
	{
		var encodedExpectedText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(expectedText));

		if (_rtlEditor.Handler?.PlatformView is not UIKit.UITextView textView)
		{
			_caretMeasurementLabel.Text = $"{sequence}|{encodedExpectedText}|error=platform-view";
			return;
		}

		var selectedTextRange = textView.SelectedTextRange;
		var nativeFont = textView.Font;
		var nativeText = textView.Text;
		if (selectedTextRange is null || nativeFont is null || nativeText is null)
		{
			_caretMeasurementLabel.Text = $"{sequence}|{encodedExpectedText}|error=native-state";
			return;
		}

		var caret = textView.GetCaretRectForPosition(selectedTextRange.End);
		var caretInWindow = textView.ConvertRectToView(caret, null);
		var editorInWindow = textView.ConvertRectToView(textView.Bounds, null);
		var selectionOffset = (int)textView.GetOffsetFromPosition(textView.BeginningOfDocument, selectedTextRange.End);
		using var space = new Foundation.NSString(" ");
		var spaceAdvance = space.GetSizeUsingAttributes(new UIKit.UIStringAttributes { Font = nativeFont }).Width;
		var encodedNativeText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(nativeText));

		_caretMeasurementLabel.Text = FormattableString.Invariant(
			$"{sequence}|{encodedExpectedText}|{encodedNativeText}|{(textView.Window is not null ? 1 : 0)}|{(textView.IsFirstResponder ? 1 : 0)}|{selectionOffset}|{caretInWindow.X:R}|{caretInWindow.Y:R}|{caretInWindow.Width:R}|{caretInWindow.Height:R}|{editorInWindow.X:R}|{editorInWindow.Y:R}|{editorInWindow.Width:R}|{editorInWindow.Height:R}|{spaceAdvance:R}");
	}
#endif
}


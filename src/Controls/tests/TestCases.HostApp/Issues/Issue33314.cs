#if WINDOWS
using System.Globalization;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33314, "Editor caret renders as a dot after Shift clears text and hides adjacent content", PlatformAffected.UWP)]
public class Issue33314 : ContentPage
{
	readonly ContentView _cancelContent;
	readonly Label _nativeKeyDownSequence;
	readonly Label _postClearLayoutStatus;
	readonly Label _textChangedSequence;
	bool _awaitingPostClearLayout;
	bool _hasText;
	int _textChangeCount = -1;

	public Issue33314()
	{
		_nativeKeyDownSequence = new Label
		{
			AutomationId = "NativeKeyDownSequence",
			Text = "-1"
		};

		_textChangedSequence = new Label
		{
			AutomationId = "TextChangedSequence",
			Text = "-1"
		};

		var focusStatus = new Label
		{
			AutomationId = "FocusStatus",
			Text = "NotFocused"
		};

		_postClearLayoutStatus = new Label
		{
			AutomationId = "PostClearLayoutStatus",
			Text = "Waiting"
		};

		var nativeFontSize = new Label
		{
			AutomationId = "NativeFontSize",
			Text = "-1"
		};

		_cancelContent = new ContentView
		{
			AutomationId = "CancelContent",
			IsVisible = false,
			Content = new Label
			{
				Text = "Cancel",
				Padding = 12
			}
		};
		Grid.SetColumn(_cancelContent, 1);

		var editor = new Issue33314Editor(_nativeKeyDownSequence, nativeFontSize)
		{
			AutomationId = "IssueEditor"
		};
		editor.TextChanged += OnEditorTextChanged;
		editor.Focused += (_, _) => focusStatus.Text = "Focused";
		editor.SizeChanged += OnEditorSizeChanged;

		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			Children =
			{
				editor,
				_cancelContent
			}
		};

		Content = new VerticalStackLayout
		{
			Padding = 32,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Type in the Editor, then press Shift",
					FontSize = 20
				},
				grid,
				_nativeKeyDownSequence,
				_textChangedSequence,
				focusStatus,
				_postClearLayoutStatus,
				nativeFontSize
			}
		};
	}

	void OnEditorTextChanged(object sender, TextChangedEventArgs e)
	{
		_textChangeCount++;
		_textChangedSequence.Text = _textChangeCount.ToString(CultureInfo.InvariantCulture);
		var hasText = !string.IsNullOrEmpty(e.NewTextValue);
		_awaitingPostClearLayout = _hasText && !hasText;
		_hasText = hasText;
		_cancelContent.IsVisible = hasText;
	}

	void OnEditorSizeChanged(object sender, EventArgs e)
	{
		if (!_awaitingPostClearLayout)
			return;

		_awaitingPostClearLayout = false;
		_postClearLayoutStatus.Text = "Complete";
	}
}

class Issue33314Editor : Editor
{
	readonly Label _nativeKeyDownSequence;
	readonly Label _nativeFontSize;
	WTextBox _platformTextBox = null!;
	int _shiftKeyDownCount = -1;

	public Issue33314Editor(Label nativeKeyDownSequence, Label nativeFontSize)
	{
		_nativeKeyDownSequence = nativeKeyDownSequence;
		_nativeFontSize = nativeFontSize;
	}

	protected override void OnHandlerChanged()
	{
		if (_platformTextBox is not null)
			_platformTextBox.KeyDown -= OnPlatformKeyDown;

		base.OnHandlerChanged();

		if (Handler?.PlatformView is WTextBox platformTextBox)
		{
			_platformTextBox = platformTextBox;
			_platformTextBox.KeyDown += OnPlatformKeyDown;
			_nativeFontSize.Text = platformTextBox.FontSize.ToString(CultureInfo.InvariantCulture);
		}
	}

	void OnPlatformKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (e.Key != Windows.System.VirtualKey.Shift)
			return;

		_shiftKeyDownCount++;
		Text = string.Empty;
		_nativeKeyDownSequence.Text = _shiftKeyDownCount.ToString(CultureInfo.InvariantCulture);
	}
}
#endif


#if WINDOWS
using WKeyRoutedEventArgs = Microsoft.UI.Xaml.Input.KeyRoutedEventArgs;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33314, "Editor caret renders as a dot after clearing text and hiding a sibling", PlatformAffected.WinPhone)]
public class Issue33314 : ContentPage
{
	public Issue33314()
	{
		var cancelIndicator = new Label
		{
			AutomationId = "CancelIndicator",
			Text = "Cancel",
			VerticalTextAlignment = TextAlignment.Center
		};

		var cancelContent = new ContentView
		{
			IsVisible = false,
			Content = cancelIndicator
		};

		var triggerStatus = new Label
		{
			AutomationId = "TriggerStatus",
			Text = "Waiting"
		};

		var editor = new Issue33314Editor
		{
			AutomationId = "IssueEditor",
			Placeholder = "Type any text"
		};

		editor.TextChanged += (_, e) =>
			cancelContent.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
		editor.ShiftPressed += (_, _) => triggerStatus.Text = "Shift key received";

		var editorGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			ColumnSpacing = 12
		};
		editorGrid.Add(editor);
		editorGrid.Add(cancelContent, 1);

		var outerGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		outerGrid.Add(editorGrid);
		outerGrid.Add(triggerStatus, 0, 1);
		Content = outerGrid;
	}
}

public class Issue33314Editor : Editor
{
	public event EventHandler ShiftPressed;

#if WINDOWS
	WTextBox _platformEditor;

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();

		if (_platformEditor is not null)
			_platformEditor.KeyDown -= OnPlatformEditorKeyDown;

		_platformEditor = Handler?.PlatformView as WTextBox;

		if (_platformEditor is not null)
			_platformEditor.KeyDown += OnPlatformEditorKeyDown;
	}

	void OnPlatformEditorKeyDown(object sender, WKeyRoutedEventArgs e)
	{
		if (e.Key != Windows.System.VirtualKey.Shift)
			return;

		Text = string.Empty;
		ShiftPressed?.Invoke(this, EventArgs.Empty);
	}
#endif
}


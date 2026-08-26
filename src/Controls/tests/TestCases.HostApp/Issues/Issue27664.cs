namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27664, "Editor does not resize when the iOS keyboard appears", PlatformAffected.iOS)]
public class Issue27664 : ContentPage
{
	public Issue27664()
	{
		double nativeHeightBeforeKeyboard = -1;
		double nativeHeightAfterKeyboard = -1;
		var nativeHeightTracker = new Label
		{
			AutomationId = "NativeHeightTracker",
			Text = "Editor keyboard resize"
		};

		var editor = new Editor
		{
			AutomationId = "IssueEditor",
			HorizontalOptions = LayoutOptions.Fill,
			Placeholder = "Enter enough text to wrap across several lines",
			VerticalOptions = LayoutOptions.Fill
		};
		editor.Focused += (_, _) => UpdateNativeHeight(captureBeforeKeyboard: true);

		var checkResizeButton = new Button
		{
			AutomationId = "CheckResizeButton",
			Text = "Check editor resize"
		};
		checkResizeButton.Clicked += (_, _) => UpdateNativeHeight(captureBeforeKeyboard: false);

		var header = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new Label
				{
					Text = "Tap the Editor, enter text, then check whether it resized for the keyboard."
				},
				nativeHeightTracker,
				checkResizeButton
			}
		};

		var grid = new Grid
		{
			AutomationId = "IssueGrid",
			Padding = 16,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			},
			RowSpacing = 12
		};

		grid.Children.Add(header);
		Grid.SetRow(editor, 1);
		grid.Children.Add(editor);
		Content = grid;

		void UpdateNativeHeight(bool captureBeforeKeyboard)
		{
#if IOS
			if (editor.Handler?.PlatformView is UIKit.UITextView textView)
			{
				var nativeHeight = textView.Frame.Height;
				if (nativeHeight <= 0)
					return;

				if (captureBeforeKeyboard)
				{
					if (nativeHeightBeforeKeyboard > 0)
						return;

					nativeHeightBeforeKeyboard = nativeHeight;
				}
				else
					nativeHeightAfterKeyboard = nativeHeight;

				nativeHeightTracker.Text = FormattableString.Invariant(
					$"{nativeHeightBeforeKeyboard:F3}|{nativeHeightAfterKeyboard:F3}");
			}
#endif
		}
	}
}


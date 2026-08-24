namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	public Issue35002()
	{
		Title = "Keyboard Tap Gesture";

		var focusSequenceLabel = new Label
		{
			AutomationId = "FocusSequenceLabel",
			Text = "None"
		};

		var startEntry = new Entry
		{
			AutomationId = "StartEntry",
			Placeholder = "Keyboard navigation starts here"
		};

		var tappableLabel = new Label
		{
			AutomationId = "TappableLabel",
			Text = "Tappable label",
			Padding = 12
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) => tappableLabel.Text = "Tappable label tapped";
		tappableLabel.GestureRecognizers.Add(tapGestureRecognizer);

		var followingButton = new Button
		{
			AutomationId = "FollowingButton",
			Text = "Following button"
		};

		startEntry.Focused += (_, _) => focusSequenceLabel.Text = "StartEntry";
		tappableLabel.Focused += (_, _) => focusSequenceLabel.Text = "TappableLabel";
		followingButton.Focused += (_, _) => focusSequenceLabel.Text = "FollowingButton";

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "TapGestureRecognizer keyboard accessibility",
					FontSize = 20
				},
				new Label
				{
					Text = "Focus the entry, then press Tab. The tappable label should receive keyboard focus before the button."
				},
				startEntry,
				tappableLabel,
				followingButton,
				focusSequenceLabel
			}
		};
	}
}


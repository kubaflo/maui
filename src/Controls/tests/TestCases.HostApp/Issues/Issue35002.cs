#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	int _targetActivations;

	public Issue35002()
	{
		var focusStartButton = new Button
		{
			AutomationId = "FocusStart",
			Text = "Keyboard focus start"
		};

		var targetLabel = new Label
		{
			AutomationId = "TapGestureTarget",
			Text = "Tap gesture target"
		};

		var keyboardCheckButton = new Button
		{
			AutomationId = "KeyboardCheck",
			Text = "Keyboard check"
		};

		var activationLabel = new Label
		{
			AutomationId = "ActivationEvidence",
			Text = "Target activations: 0"
		};

		var outcomeLabel = new Label
		{
			AutomationId = "KeyboardOutcome",
			Text = "Keyboard outcome: pending"
		};

		focusStartButton.Focused += (_, _) => focusStartButton.Text = "Keyboard focus start: focused";

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) =>
		{
			_targetActivations++;
			targetLabel.Text = "Tap gesture target activated";
			activationLabel.Text = $"Target activations: {_targetActivations}";
			outcomeLabel.Text = "Keyboard result: target activated";
		};
		targetLabel.GestureRecognizers.Add(tapGestureRecognizer);

		void RecordFollowingButtonActivation() =>
			outcomeLabel.Text = "Keyboard result: following button activated";

		keyboardCheckButton.Focused += (_, _) => RecordFollowingButtonActivation();
		keyboardCheckButton.Clicked += (_, _) => RecordFollowingButtonActivation();

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label { Text = "Use Tab and Enter to activate the label with a tap gesture." },
				focusStartButton,
				targetLabel,
				keyboardCheckButton,
				activationLabel,
				outcomeLabel
			}
		};
	}
}
#endif


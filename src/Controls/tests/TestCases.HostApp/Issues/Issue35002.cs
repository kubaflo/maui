namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	public Issue35002()
	{
		int keyboardBaselineActivations = 0;
		int keyboardStartDepartures = 0;
		int tapTargetActivations = 0;
		int tapTargetFocuses = 0;

		var keyboardBaselineLabel = new Label
		{
			AutomationId = "KeyboardBaselineLabel",
			Text = "Keyboard baseline activations: 0"
		};

		var keyboardDepartureLabel = new Label
		{
			AutomationId = "KeyboardDepartureLabel",
			Text = "Keyboard start departures: 0"
		};

		var tapActivationLabel = new Label
		{
			AutomationId = "TapActivationLabel",
			Text = "Tap target activations: 0"
		};

		var tapFocusLabel = new Label
		{
			AutomationId = "TapFocusLabel",
			Text = "Tap target keyboard focuses: 0"
		};

		var keyboardStartButton = new Button
		{
			AutomationId = "KeyboardStartButton",
			Text = "Keyboard start"
		};
		keyboardStartButton.Clicked += (sender, args) =>
		{
			keyboardBaselineActivations++;
			keyboardBaselineLabel.Text = $"Keyboard baseline activations: {keyboardBaselineActivations}";
		};
		keyboardStartButton.Unfocused += (sender, args) =>
		{
			keyboardStartDepartures++;
			keyboardDepartureLabel.Text = $"Keyboard start departures: {keyboardStartDepartures}";
		};

		var tapTargetLabel = new Label
		{
			AutomationId = "TapTargetLabel",
			Text = "Tap gesture target"
		};
		tapTargetLabel.Focused += (sender, args) =>
		{
			tapTargetFocuses++;
			tapFocusLabel.Text = $"Tap target keyboard focuses: {tapTargetFocuses}";
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
		{
			tapTargetActivations++;
			tapActivationLabel.Text = $"Tap target activations: {tapTargetActivations}";
		};
		tapTargetLabel.GestureRecognizers.Add(tapGestureRecognizer);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "TapGestureRecognizer keyboard accessibility" },
					new Label { Text = "Activate Keyboard start with Enter, then press Tab and Enter. The tap target should receive the activation." },
					keyboardStartButton,
					tapTargetLabel,
					new Button
					{
						AutomationId = "KeyboardFallbackButton",
						Text = "Keyboard fallback sentinel"
					},
					keyboardBaselineLabel,
					keyboardDepartureLabel,
					tapActivationLabel,
					tapFocusLabel
				}
			}
		};
	}
}


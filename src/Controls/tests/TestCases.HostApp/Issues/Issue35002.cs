namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	int _gestureFocusCount = -1;
	int _gestureTapCount = -1;
	int _afterTargetFocusCount = -1;
	int _afterTargetClickCount = -1;
	readonly Label _resultLabel;

	public Issue35002()
	{
		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = GetTelemetry("Not initialized")
		};

		var focusStartButton = new Button
		{
			AutomationId = "FocusStartButton",
			Text = "Keyboard focus start"
		};
		focusStartButton.Clicked += (sender, args) =>
		{
			_gestureFocusCount = 0;
			_gestureTapCount = 0;
			_afterTargetFocusCount = 0;
			_afterTargetClickCount = 0;
			_resultLabel.Text = GetTelemetry("Initialized");
		};

		var gestureLabel = new Label
		{
			AutomationId = "GestureLabel",
			Text = "Gesture target"
		};
		gestureLabel.Focused += (sender, args) =>
		{
			_gestureFocusCount++;
			_resultLabel.Text = GetTelemetry("Focus observed");
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
		{
			_gestureTapCount++;
			_resultLabel.Text = GetTelemetry("Activation observed");
		};
		gestureLabel.GestureRecognizers.Add(tapGestureRecognizer);

		var afterTargetButton = new Button
		{
			AutomationId = "AfterTargetButton",
			Text = "After gesture target"
		};
		afterTargetButton.Focused += (sender, args) =>
		{
			_afterTargetFocusCount++;
			_resultLabel.Text = GetTelemetry("Focus observed");
		};
		afterTargetButton.Clicked += (sender, args) =>
		{
			_afterTargetClickCount++;
			_resultLabel.Text = GetTelemetry("Activation observed");
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					FontSize = 22,
					Text = "TapGestureRecognizer keyboard accessibility"
				},
				new Label
				{
					Text = "Focus the start button, then press Tab and Enter. The gesture label should receive keyboard focus and activate."
				},
				focusStartButton,
				gestureLabel,
				afterTargetButton,
				_resultLabel
			}
		};
	}

	string GetTelemetry(string state) =>
		$"{state}; GestureFocus={_gestureFocusCount}; GestureTaps={_gestureTapCount}; AfterFocus={_afterTargetFocusCount}; AfterClicks={_afterTargetClickCount}";
}


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	int _lastFocusedIdentity = -1;
	int _observationSequence = -1;

	public Issue36412()
	{
		var lastFocusedLabel = new Label
		{
			AutomationId = "LastFocusedIdentity",
			Text = _lastFocusedIdentity.ToString()
		};

		var observationSequenceLabel = new Label
		{
			AutomationId = "ObservationSequence",
			Text = _observationSequence.ToString()
		};

		var instructionLabel = new Label
		{
			Text = "Focus Field 1, then tap the visible Field 7 above the numeric keyboard."
		};

		var observeButton = new Button
		{
			AutomationId = "ObserveFocusButton",
			Text = "Observe focus"
		};
		observeButton.Clicked += (_, _) =>
		{
			_observationSequence++;
			observationSequenceLabel.Text = _observationSequence.ToString();
		};

		var stack = new VerticalStackLayout
		{
			Spacing = 10,
			Children =
			{
				lastFocusedLabel,
				observationSequenceLabel,
				instructionLabel,
				observeButton
			}
		};

		for (int identity = 1; identity <= 15; identity++)
		{
			int focusedIdentity = identity;
			var entry = new Entry
			{
				AutomationId = $"Field{identity}",
				Keyboard = Keyboard.Numeric,
				Placeholder = $"Field {identity}"
			};
			entry.Focused += (_, _) =>
			{
				_lastFocusedIdentity = focusedIdentity;
				lastFocusedLabel.Text = focusedIdentity.ToString();
			};
			stack.Children.Add(entry);
		}

		Content = new ScrollView
		{
			Content = stack
		};
	}
}

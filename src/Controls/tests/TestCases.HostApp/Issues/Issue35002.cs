namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	public Issue35002()
	{
		var focusedElement = "Unset";
		var focusCallbacks = 0;
		var targetTaps = 0;
		var sentinelClicks = 0;

		var startEntry = new Entry
		{
			AutomationId = "StartEntry",
			Placeholder = "Keyboard traversal starts here"
		};

		var gestureTarget = new Label
		{
			AutomationId = "GestureTarget",
			Text = "Keyboard-selectable gesture target"
		};

		var traversalSentinel = new Button
		{
			AutomationId = "TraversalSentinel",
			Text = "Traversal sentinel"
		};

		var focusDetails = new Label
		{
			AutomationId = "FocusDetails",
			Text = "Waiting for keyboard input; Focused=Unset; FocusCallbacks=0"
		};

		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold
		};

		void UpdateFocus(string identity)
		{
			focusedElement = identity;
			focusCallbacks++;
			focusDetails.Text = $"Focused={focusedElement}; FocusCallbacks={focusCallbacks}";
		}

		void UpdateActivation()
		{
			resultLabel.Text = $"Recognizers={gestureTarget.GestureRecognizers.Count}; TargetTaps={targetTaps}; SentinelClicks={sentinelClicks}; TotalActivations={targetTaps + sentinelClicks}";
		}

		startEntry.Focused += (sender, args) => UpdateFocus("StartEntry");
		gestureTarget.Focused += (sender, args) => UpdateFocus("GestureTarget");
		traversalSentinel.Focused += (sender, args) => UpdateFocus("TraversalSentinel");

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
		{
			targetTaps++;
			UpdateActivation();
		};
		gestureTarget.GestureRecognizers.Add(tapGestureRecognizer);

		traversalSentinel.Clicked += (sender, args) =>
		{
			sentinelClicks++;
			UpdateActivation();
		};

		UpdateActivation();

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 20,
					Text = "TapGestureRecognizer keyboard test"
				},
				new Label
				{
					Text = "Focus the start field, then press Tab and Enter. The gesture target should receive keyboard focus and activate."
				},
				startEntry,
				gestureTarget,
				traversalSentinel,
				focusDetails,
				resultLabel
			}
		};
	}
}


namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.WinRT)]
public class Issue35002 : ContentPage
{
	public Issue35002()
	{
		var tapCount = 0;
		var buttonClickCount = 0;

		var inputStatusLabel = new Label
		{
			AutomationId = "Issue35002InputStatus",
			Text = "Callback=None"
		};

		var resultLabel = new Label
		{
			AutomationId = "Issue35002ResultStatus",
			Text = "Callback=None; TapCount=0; ButtonClickCount=0"
		};

		void UpdateResult(string callback)
		{
			inputStatusLabel.Text = $"Callback={callback}";
			resultLabel.Text = $"Callback={callback}; TapCount={tapCount}; ButtonClickCount={buttonClickCount}";
		}

		var gestureTarget = new Label
		{
			AutomationId = "Issue35002GestureTarget",
			Text = "Keyboard gesture target"
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
		{
			tapCount++;
			UpdateResult("Tap");
		};
		gestureTarget.GestureRecognizers.Add(tapGestureRecognizer);

		var afterTargetButton = new Button
		{
			AutomationId = "Issue35002AfterTargetButton",
			Text = "After target"
		};
		afterTargetButton.Clicked += (sender, args) =>
		{
			buttonClickCount++;
			UpdateResult("Button");
		};

		Content = new VerticalStackLayout
		{
			Margin = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "Issue35002Instructions",
					Text = "Use the keyboard to move from the entry to the gesture target and select it."
				},
				new Entry
				{
					AutomationId = "Issue35002KeyboardAnchor",
					Placeholder = "Keyboard focus starts here"
				},
				gestureTarget,
				afterTargetButton,
				inputStatusLabel,
				resultLabel
			}
		};
	}
}


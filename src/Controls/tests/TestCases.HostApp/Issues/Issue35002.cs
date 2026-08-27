#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	public Issue35002()
	{
		var transitionTrace = new Label
		{
			AutomationId = "TransitionTrace",
			Text = "ACTIVATION: Not triggered"
		};

		var setupStatus = new Label
		{
			AutomationId = "SetupStatus",
			Text = "SETUP: Waiting"
		};

		var keyboardStart = new Button
		{
			AutomationId = "KeyboardStart",
			Text = "Place keyboard focus before gesture target"
		};
		keyboardStart.Clicked += (sender, args) =>
		{
			keyboardStart.Focus();
			setupStatus.Text = keyboardStart.IsFocused
				? "SETUP: Preceding button focused"
				: "SETUP: Preceding button not focused";
		};

		var gestureTarget = new Label
		{
			AutomationId = "GestureTarget",
			Text = "Gesture target"
		};
		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (sender, args) =>
			transitionTrace.Text = "ACTIVATED: GestureTarget";
		gestureTarget.GestureRecognizers.Add(tapGestureRecognizer);

		var keyboardSentinel = new Button
		{
			AutomationId = "KeyboardSentinel",
			Text = "Sentinel after gesture target"
		};
		keyboardSentinel.Clicked += (sender, args) =>
			transitionTrace.Text = "ACTIVATED: KeyboardSentinel";

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontSize = 24,
						Text = "TapGestureRecognizer keyboard accessibility"
					},
					new Label
					{
						Text = "Use the setup button to place keyboard focus. Tab once and press Enter. The gesture target should be selected and activated instead of the sentinel button."
					},
					keyboardStart,
					gestureTarget,
					keyboardSentinel,
					transitionTrace,
					setupStatus
				}
			}
		};
	}
}
#endif


#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.UWP)]
public class Issue35002 : ContentPage
{
	int _focusTransitionCount = -1;
	int _activationCallbackCount = -1;
	int _gestureActivationCount;
	int _boundaryActivationCount;
	string _focusedIdentity = "None";
	readonly Label _resultLabel;

	public Issue35002()
	{
		_resultLabel = new Label
		{
			AutomationId = "ResultLabel"
		};

		var focusEntry = new Entry
		{
			AutomationId = "FocusEntry",
			Placeholder = "Keyboard focus starts here"
		};
		focusEntry.Focused += OnFocusEntryFocused;

		var gestureTarget = new Label
		{
			AutomationId = "GestureTarget",
			Text = "Activate gesture target"
		};
		gestureTarget.Focused += OnGestureTargetFocused;

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += OnGestureTapped;
		gestureTarget.GestureRecognizers.Add(tapGestureRecognizer);

		var boundaryButton = new Button
		{
			AutomationId = "BoundaryButton",
			Text = "Following focus target"
		};
		boundaryButton.Focused += OnBoundaryButtonFocused;
		boundaryButton.Clicked += OnBoundaryButtonClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Keyboard gesture accessibility",
					FontSize = 24
				},
				new Label
				{
					Text = "Focus the entry, then press Tab and Enter."
				},
				focusEntry,
				gestureTarget,
				boundaryButton,
				_resultLabel
			}
		};

		UpdateResult();
	}

	void OnFocusEntryFocused(object sender, FocusEventArgs e)
	{
		_focusTransitionCount = 0;
		_activationCallbackCount = 0;
		_gestureActivationCount = 0;
		_boundaryActivationCount = 0;
		_focusedIdentity = "FocusEntry";
		UpdateResult();
	}

	void OnGestureTargetFocused(object sender, FocusEventArgs e)
	{
		_focusTransitionCount++;
		_focusedIdentity = "GestureTarget";
		UpdateResult();
	}

	void OnBoundaryButtonFocused(object sender, FocusEventArgs e)
	{
		_focusTransitionCount++;
		_focusedIdentity = "BoundaryButton";
		UpdateResult();
	}

	void OnGestureTapped(object sender, TappedEventArgs e)
	{
		_gestureActivationCount++;
		_activationCallbackCount++;
		UpdateResult();
	}

	void OnBoundaryButtonClicked(object sender, EventArgs e)
	{
		_boundaryActivationCount++;
		_activationCallbackCount++;
		UpdateResult();
	}

	void UpdateResult()
	{
		_resultLabel.Text = $"Focused={_focusedIdentity}; FocusTransitions={_focusTransitionCount}; ActivationCallbacks={_activationCallbackCount}; GestureActivations={_gestureActivationCount}; BoundaryActivations={_boundaryActivationCount}";
	}
}
#endif


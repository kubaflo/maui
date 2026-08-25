namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35002, "TapGestureRecognizer controls are not selectable with a physical keyboard", PlatformAffected.WinRT)]
public partial class Issue35002 : ContentPage
{
	public Issue35002()
	{
		InitializeComponent();
	}

	void OnKeyboardStartEntryFocused(object sender, FocusEventArgs e)
	{
		SetStatus("None", "KeyboardStartEntry");
	}

	void OnGestureTargetFocused(object sender, FocusEventArgs e)
	{
		SetStatus("None", "GestureTarget");
	}

	void OnGestureTargetTapped(object sender, TappedEventArgs e)
	{
		SetStatus("GestureTarget", "GestureTarget");
	}

	void OnFallbackButtonClicked(object sender, EventArgs e)
	{
		FallbackButton.Text = "Keyboard reached fallback";
		SetStatus("FallbackButton", "FallbackButton");
	}

	void SetStatus(string activatedControl, string focusedControl)
	{
		ActivationStatusLabel.Text = $"ActivatedControl:{activatedControl}\nFocusedControl:{focusedControl}";
	}
}

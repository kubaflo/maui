namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26158, "Issue 26158: SelectionLength set in Focused callback is reset on iOS", PlatformAffected.iOS)]
public partial class Issue26158 : ContentPage
{
	bool _focusCallbackRan;

	public Issue26158()
	{
		InitializeComponent();
	}

	void OnEntryFocused(object sender, FocusEventArgs e)
	{
		_focusCallbackRan = true;
		ReproEntry.SelectionLength = 3;
		UpdateResult();
	}

	void OnEntryPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (!_focusCallbackRan ||
			e.PropertyName != nameof(Entry.SelectionLength))
		{
			return;
		}

		UpdateResult();
	}

	void UpdateResult()
	{
		ResultLabel.Text = $"FocusCallbackRan=True;IsFocused={ReproEntry.IsFocused};Text={ReproEntry.Text};ManagedSelectionLength={ReproEntry.SelectionLength}";
	}
}

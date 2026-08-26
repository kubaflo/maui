namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27126, "Line control prevents tapping other controls on iOS", PlatformAffected.iOS)]
public partial class Issue27126 : ContentPage
{
	int _tapCount;

	public Issue27126()
	{
		InitializeComponent();
	}

	void OnIssueLayoutSizeChanged(object sender, EventArgs e)
	{
		IssueLine.X2 = IssueLayout.Width;
	}

	void OnTapTargetTapped(object sender, TappedEventArgs e)
	{
		_tapCount++;
		TapCount.Text = $"Target tap count: {_tapCount}";
	}
}

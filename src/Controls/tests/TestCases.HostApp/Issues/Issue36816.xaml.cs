namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36816, "Clicks pass through ContentView to controls underneath on Android", PlatformAffected.Android)]
public partial class Issue36816 : ContentPage
{
	int _buttonClickCount;

	public Issue36816()
	{
		InitializeComponent();
		Loaded += OnPageLoaded;
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		ButtonClickCountLabel.Text = $"Underlying button clicks: {_buttonClickCount}";
	}

	void OnCoveredButtonClicked(object sender, EventArgs e)
	{
		_buttonClickCount++;
		ButtonClickCountLabel.Text = $"Underlying button clicks: {_buttonClickCount}";
		CoveredButton.BackgroundColor = Colors.Red;
	}
}

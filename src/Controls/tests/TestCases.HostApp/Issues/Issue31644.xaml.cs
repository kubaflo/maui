namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31644, "Input blocked to sibling ContentView on iOS", PlatformAffected.iOS)]
public partial class Issue31644 : ContentPage
{
	int _bottomClickCount;

	public Issue31644()
	{
		InitializeComponent();
	}

	void OnTopButtonClicked(object sender, EventArgs e)
	{
		TopGrid.IsVisible = false;
		InteractionArea.BackgroundColor = Colors.LightCoral;
		TopTransitionLabel.Text = "1";
	}

	void OnBottomButtonClicked(object sender, EventArgs e)
	{
		_bottomClickCount++;
		BottomClickCountLabel.Text = _bottomClickCount.ToString();
		TopGrid.IsVisible = true;
		InteractionArea.BackgroundColor = Colors.LightGreen;
	}
}

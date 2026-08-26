namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26173, "Fancy Sample Code Uses Copyrighted Fonts", PlatformAffected.iOS)]
public partial class Issue26173 : ContentPage
{
	public Issue26173()
	{
		InitializeComponent();
	}

	void OnCreateSampleProjectClicked(object sender, EventArgs e)
	{
		GeneratedProject.IsVisible = true;
		CallbackTokenLabel.Text = "CallbackToken:created";
	}
}

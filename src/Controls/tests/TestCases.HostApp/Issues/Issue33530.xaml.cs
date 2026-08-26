namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "Border with Rotation and Start alignment is positioned incorrectly on initial load", PlatformAffected.Android)]
public partial class Issue33530 : ContentPage
{
	public Issue33530()
	{
		InitializeComponent();
	}

	async void OnOpenAffectedModalClicked(object sender, EventArgs e)
	{
		var template = (DataTemplate)Resources["AffectedPageTemplate"];
		var modalPage = (ContentPage)template.CreateContent();
		await Navigation.PushModalAsync(modalPage, false);
	}
}

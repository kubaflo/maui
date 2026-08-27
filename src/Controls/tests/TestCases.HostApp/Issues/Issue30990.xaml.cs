namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30990, "Shell toolbar ignores Shell.ForegroundColor", PlatformAffected.Android)]
public partial class Issue30990 : Shell
{
	public Issue30990()
	{
		InitializeComponent();
	}

	void OnShellLoaded(object sender, EventArgs e)
	{
		var foregroundColor = GetForegroundColor(this);
		var application = Application.Current;

		if (application is null)
			throw new InvalidOperationException("The application must be available when the Shell is loaded.");

		var red = (int)Math.Round(foregroundColor.Red * 255);
		var green = (int)Math.Round(foregroundColor.Green * 255);
		var blue = (int)Math.Round(foregroundColor.Blue * 255);

		SemanticProperties.SetDescription(
			MetadataLabel,
			$"Foreground={red},{green},{blue};Text={TextToolbarItem.Text};Theme={application.RequestedTheme}");
	}
}

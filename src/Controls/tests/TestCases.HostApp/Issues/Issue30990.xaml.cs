using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30990, "Shell toolbar ignores Shell foreground color", PlatformAffected.Android)]
public partial class Issue30990 : Shell
{
	public Issue30990()
	{
		InitializeComponent();
	}

	void OnPageLoaded(object sender, EventArgs e)
	{
		var page = (ContentPage)sender;
		LoadedStatusLabel.Text = page.ToolbarItems.Count.ToString(CultureInfo.InvariantCulture);
	}
}

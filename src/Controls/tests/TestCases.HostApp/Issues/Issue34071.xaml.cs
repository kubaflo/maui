namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "Shell foreground color is not applied to ToolbarItems", PlatformAffected.UWP)]
public partial class Issue34071 : Shell
{
	public Issue34071()
	{
		InitializeComponent();
	}

	void OnContentPageLoaded(object sender, EventArgs e)
	{
		if (sender is not ContentPage page)
			throw new InvalidOperationException("The loaded Shell content must be a ContentPage.");

		var loadStatus = page.FindByName<Label>("LoadStatus");
		if (loadStatus is null)
			throw new InvalidOperationException("The load status label was not created.");

		var toolbarItem = page.ToolbarItems.SingleOrDefault();
		var setupIsValid =
			Shell.GetForegroundColor(this) == Colors.Magenta &&
			Shell.GetTitleColor(this) == Colors.Black &&
			CurrentPage == page &&
			toolbarItem is not null &&
			toolbarItem.AutomationId == "AffectedToolbarItem" &&
			toolbarItem.IconImageSource?.ToString().EndsWith("shopping_cart.png", StringComparison.Ordinal) == true;

		loadStatus.Text = setupIsValid ? "Loaded:Ready" : "Loaded:Invalid";
	}
}

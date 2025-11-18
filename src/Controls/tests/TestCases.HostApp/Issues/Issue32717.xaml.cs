using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 32717, "LinearGradientBrush disappears after showing a Popup on iOS", PlatformAffected.iOS)]
	public partial class Issue32717 : ContentPage
	{
		public Issue32717()
		{
			InitializeComponent();
		}

		private void OnShowOverlayClicked(object sender, System.EventArgs e)
		{
			// Simulate popup showing by making overlay visible
			PopupOverlay.IsVisible = true;
			StatusLabel.Text = "Overlay Shown";
		}

		private void OnCloseOverlayClicked(object sender, System.EventArgs e)
		{
			// Simulate popup closing
			PopupOverlay.IsVisible = false;
			StatusLabel.Text = "Overlay Closed - Gradient should still be visible";
		}
	}
}

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 32673, "[Android] FlowDirection not working on EmptyView in CollectionView", 
		PlatformAffected.Android | PlatformAffected.iOS)]
	public partial class Issue32673 : ContentPage
	{
		public Issue32673()
		{
			InitializeComponent();
		}

		private void OnToggleFlowDirection(object sender, System.EventArgs e)
		{
			var newDirection = CollectionViewString.FlowDirection == FlowDirection.LeftToRight
				? FlowDirection.RightToLeft
				: FlowDirection.LeftToRight;

			CollectionViewString.FlowDirection = newDirection;
			CollectionViewView.FlowDirection = newDirection;
			CollectionViewTemplated.FlowDirection = newDirection;

			StatusLabel.Text = $"FlowDirection: {newDirection}";
		}
	}
}

using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 32407, "BoxView as item separator not displaying in CollectionView2 after scrolling", PlatformAffected.iOS)]
	public partial class Issue32407 : ContentPage
	{
		public ObservableCollection<string> Items { get; set; }

		public Issue32407()
		{
			InitializeComponent();

			// Create enough items to ensure scrolling and cell reuse
			Items = new ObservableCollection<string>();
			for (int i = 1; i <= 50; i++)
			{
				Items.Add($"Question {i}");
			}

			BindingContext = this;
		}
	}
}

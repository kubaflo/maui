using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Controls.Sample.Issues
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	[Issue(IssueTracker.Github, 15524, "Text entry border disappear when changing to/from dark mode", PlatformAffected.Android)]
	public partial class Issue15524 : ContentPage
	{
		public Issue15524()
		{
			InitializeComponent();
		}
	}
}
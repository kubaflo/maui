using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Controls.Sample.Issues
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	[Issue(IssueTracker.Github, 32440, "CheckBox alignment issue in .NET 10 when placed inside Grid with VerticalOptions=Center", PlatformAffected.Android)]
	public partial class Issue32440 : ContentPage
	{
		public Issue32440()
		{
			InitializeComponent();
		}
	}
}

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;
using TabbedPage = Microsoft.Maui.Controls.TabbedPage;

namespace Maui.Controls.Sample
{
	public partial class DemoTabbedPage : TabbedPage
	{
		public DemoTabbedPage()
		{
			InitializeComponent();
		}

		void Button_Clicked(System.Object sender, System.EventArgs e)
		{
			On<iOS>().SetPrefersHomeIndicatorAutoHidden(!On<iOS>().PrefersHomeIndicatorAutoHidden());
		}

		void Back_Clicked(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new MainPage();
		}
	}
}
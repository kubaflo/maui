using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace Maui.Controls.Sample
{
	public partial class DemoNavigationPage : ContentPage
	{
		public DemoNavigationPage()
		{
			InitializeComponent();
		}

		void Button_Clicked(System.Object sender, System.EventArgs e)
		{
			On<iOS>().SetPrefersHomeIndicatorAutoHidden(!On<iOS>().PrefersHomeIndicatorAutoHidden());
		}

		void Back_Clicked(System.Object sender, System.EventArgs e)
		{
			if(Application.Current!=null)
				Application.Current.MainPage = new MainPage();
		}
	}
}
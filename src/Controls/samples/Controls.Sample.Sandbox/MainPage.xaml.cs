using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

		void Button_Clicked_1(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new NavigationPage(new DemoNavigationPage());
		}

		void Button_Clicked_2(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new DemoTabbedPage();
		}

		void Button_Clicked_3(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new DemoFlyoutPage();
		}

		void Button_Clicked_4(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new DemoShellPage();
		}
	}
}
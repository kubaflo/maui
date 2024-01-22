using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;
using TabbedPage = Microsoft.Maui.Controls.TabbedPage;

namespace Maui.Controls.Sample
{
	public partial class DemoFlyoutPage : Microsoft.Maui.Controls.FlyoutPage
    {
		Command BackCommand;

		public DemoFlyoutPage()
		{
			InitializeComponent();
			BackCommand = new Command(() => {
				if (Application.Current != null)
					Application.Current.MainPage = new MainPage();
			});
		}

		private void btnPage1_Clicked(object sender, EventArgs e)
		{
			ContentPage contentPage = new ContentPage() { Title = "Detail 2" };
			contentPage.Content = new StackLayout
			{
				Margin = new Thickness(20, 35, 20, 20),
				Children =
					{
						new Label
						{
							Text = "Detail 1",
							FontSize = 25
						},
						new Label
						{
							Text = "Tap the button below to toggle the home indicator on iPhone X, XR, and XS models."
						},
						new Button
						{
							Text = "Toggle Home Indicator",
							Command = new Command(() => On<iOS>().SetPrefersHomeIndicatorAutoHidden(!On<iOS>().PrefersHomeIndicatorAutoHidden()))
						},
						new Button
						{
							VerticalOptions = LayoutOptions.End,
							Text = "Back",
							Command = BackCommand
						}
					}
			};
			Detail = contentPage;
		}

		private void btnPage2_Clicked(object sender, EventArgs e)
		{
			ContentPage contentPage = new ContentPage() { Title = "Detail 2" };
			contentPage.Content= new StackLayout
			{
				Margin = new Thickness(20, 35, 20, 20),
				Children =
					{
						new Label
						{
							Text = "Detail 2",
							FontSize = 25
						},
						new Label
						{
							Text = "Tap the button below to toggle the home indicator on iPhone X, XR, and XS models."
						},
						new Button
						{
							Text = "Toggle Home Indicator",
							Command = new Command(() => On<iOS>().SetPrefersHomeIndicatorAutoHidden(!On<iOS>().PrefersHomeIndicatorAutoHidden()))
						},
						new Button
						{
							VerticalOptions = LayoutOptions.End,
							Text = "Back",
							Command = BackCommand
						}
					}
			};

			Detail = contentPage;
		}

		void Button_Clicked(System.Object sender, System.EventArgs e)
		{
			On<iOS>().SetPrefersHomeIndicatorAutoHidden(!On<iOS>().PrefersHomeIndicatorAutoHidden());
		}

		void Back_Clicked(System.Object sender, System.EventArgs e)
		{
			BackCommand.Execute(null);
		}
	}
}
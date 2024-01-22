using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace Maui.Controls.Sample
{
	public partial class DemoShellPage : Shell
	{
		public DemoShellPage()
		{
			InitializeComponent();
		}

		void Button_Page1_Clicked(System.Object sender, System.EventArgs e)
		{
			Page1.On<iOS>().SetPrefersHomeIndicatorAutoHidden(!Page1.On<iOS>().PrefersHomeIndicatorAutoHidden());
		}

		void Button_Page2_Clicked(System.Object sender, System.EventArgs e)
		{
			Page2.On<iOS>().SetPrefersHomeIndicatorAutoHidden(!Page2.On<iOS>().PrefersHomeIndicatorAutoHidden());
		}

		void Back_Clicked(System.Object sender, System.EventArgs e)
		{
			if (Application.Current != null)
				Application.Current.MainPage = new MainPage();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample
{
	public partial class DetailPage : ContentPage
	{
		public DetailPage()
		{
			InitializeComponent();
		}


		private async void NavigateBack(object sender, EventArgs e)
		{
			await Shell.Current.GoToAsync("..", animate: true);
		}

		private async void NavigateBackPop(object sender, EventArgs e)
		{
			await Shell.Current.Navigation.PopAsync();
		}

	}
}
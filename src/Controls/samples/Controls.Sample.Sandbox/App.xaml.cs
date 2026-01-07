using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace Maui.Controls.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// To test shell scenarios, change this to true
		bool useShell = true;

		if (!useShell)
		{
			var navPage = new Microsoft.Maui.Controls.NavigationPage(new MainPage());
			navPage.BarBackgroundColor = Colors.Transparent;
			navPage.BackgroundColor = Colors.Brown;
			navPage.On<iOS>().SetPrefersLargeTitles(true);
			return new Window(navPage);
		}
		else
		{
			return new Window(new SandboxShell());
		}
	}
}

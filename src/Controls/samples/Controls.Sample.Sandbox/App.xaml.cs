namespace Maui.Controls.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Testing PR #32700 - Shell NavBar space reservation fix
		return new Window(new TestShell());
	}
}

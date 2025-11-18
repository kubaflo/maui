using System;

namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	public SandboxShell()
	{
		InitializeComponent();
	}

	private void OnAboutClicked(object sender, EventArgs e)
	{
		Console.WriteLine("About clicked");
	}

	private void OnSettingsClicked(object sender, EventArgs e)
	{
		Console.WriteLine("Settings clicked");
	}
}

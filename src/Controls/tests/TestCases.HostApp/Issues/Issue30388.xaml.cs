namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30388, "COMException during OnLaunched in Debug", PlatformAffected.UWP)]
public partial class Issue30388 : TestContentPage
{
	public Issue30388()
	{
		InitializeComponent();
	}

	protected override void Init()
	{
	}

	void OnGetActivatedEventArgsClicked(object sender, EventArgs e)
	{
		InvocationStatusLabel.Text = "API invocation reached";

#if WINDOWS
		try
		{
			_ = Windows.ApplicationModel.AppInstance.GetActivatedEventArgs();
			ExceptionStatusLabel.Text = "None";
		}
		catch (System.Runtime.InteropServices.COMException exception)
		{
			ExceptionStatusLabel.Text = $"{exception.GetType().FullName} (0x{exception.HResult:X8})";
		}
#endif
	}
}

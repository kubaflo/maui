#if WINDOWS
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37540, "SetDynamicResource does not update the Background property of the Label", PlatformAffected.UWP)]
public class Issue37540NavigationPage : NavigationPage
{
	public Issue37540NavigationPage() : base(new Issue37540())
	{
	}
}

public partial class Issue37540 : ContentPage
{
#if WINDOWS
	static int s_nextInstanceId;
	static int s_nextLoadSequence;

	readonly int _instanceId;
	int _loadedCount;
	int _loadSequence;
#endif

	readonly bool _runScenario;

	public Issue37540() : this(false)
	{
	}

	Issue37540(bool runScenario)
	{
		_runScenario = runScenario;
#if WINDOWS
		_instanceId = System.Threading.Interlocked.Increment(ref s_nextInstanceId);
#endif
		InitializeComponent();
	}

	void OnTargetLabelLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		_loadedCount++;
		_loadSequence = System.Threading.Interlocked.Increment(ref s_nextLoadSequence);
#endif

		if (_runScenario)
			TargetLabel.SetDynamicResource(Label.BackgroundProperty, "backgroundColor");

		PublishResult();
	}

	void PublishResult()
	{
#if WINDOWS
		var resourceColor = Resources["backgroundColor"] as Color;
		var viewHandler = TargetLabel.Handler as Microsoft.Maui.IViewHandler;
		var nativeView = viewHandler?.ContainerView ?? viewHandler?.PlatformView;
		var nativeBrush = (nativeView as WPanel)?.Background;

		var resourceArgb = resourceColor?.ToArgbHex(includeAlpha: true) ?? "MISSING_RESOURCE";
		var nativeArgb = nativeBrush is WSolidColorBrush solidBrush
			? $"#{solidBrush.Color.A:X2}{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}"
			: "MISSING_BRUSH";
		var nativeAlpha = nativeBrush is WSolidColorBrush alphaBrush
			? alphaBrush.Color.A.ToString()
			: "MISSING";

		ResultStatus.Text =
			$"Phase={(_runScenario ? "Scenario" : "Setup")};" +
			$"Instance={_instanceId};Loaded={_loadedCount};LoadSequence={_loadSequence};" +
			"Sampled=True;" +
			$"Resource={resourceArgb};NativeView={nativeView is not null};" +
			$"Native={nativeArgb};NativeAlpha={nativeAlpha}";
#else
		ResultStatus.Text = "Windows-only native background sample";
#endif
	}

	async void OnRunScenarioClicked(object sender, EventArgs e)
	{
		RunScenarioButton.IsEnabled = false;
		await Navigation.PushAsync(new Issue37540(true));
	}
}

using System;
using System.Threading.Tasks;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Add instrumentation to capture layout info after appearing
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			CaptureLayoutInfo("OnAppearing");
		});
	}

	private void CaptureLayoutInfo(string context)
	{
		Console.WriteLine($"========== {context} ==========");
		Console.WriteLine($"Page FlowDirection: {FlowDirection}");
		Console.WriteLine($"ContentStack FlowDirection: {ContentStack.FlowDirection}");
		Console.WriteLine($"Page Bounds: {Bounds}");
		Console.WriteLine($"ContentStack Bounds: {ContentStack.Bounds}");
		
		#if IOS || MACCATALYST
		if (Handler?.PlatformView is UIKit.UIView platformView)
		{
			Console.WriteLine($"Platform View SemanticContentAttribute: {platformView.SemanticContentAttribute}");
			Console.WriteLine($"Platform View Frame: {platformView.Frame}");
			
			// Check parent shell
			var parent = platformView.Superview;
			while (parent != null)
			{
				Console.WriteLine($"Parent View Type: {parent.GetType().Name}, SemanticContentAttribute: {parent.SemanticContentAttribute}");
				parent = parent.Superview;
			}
		}
		#endif
		
		Console.WriteLine("=================================");
		
		DebugLabel.Text = $"FlowDirection: {FlowDirection}, Bounds: {Bounds.Width}x{Bounds.Height}";
	}
}

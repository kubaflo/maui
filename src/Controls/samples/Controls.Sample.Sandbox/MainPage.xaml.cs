namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		Console.WriteLine("=== SANDBOX APP: MainPage loaded ===");
		
		// Subscribe to size changes to track TitleView resizing
		TitleViewGrid.SizeChanged += OnTitleViewSizeChanged;
		
		// Log initial state after a delay to ensure layout is complete
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1000), () =>
		{
			LogTitleViewMeasurements("Initial Load");
		});
	}

	private void OnTitleViewSizeChanged(object sender, EventArgs e)
	{
		// Log whenever the TitleView size changes
		LogTitleViewMeasurements("SizeChanged Event");
	}

	private void LogTitleViewMeasurements(string context)
	{
		Console.WriteLine($"\n========== TITLEVIEW MEASUREMENTS: {context} ==========");
		
		// Log TitleView dimensions
		Console.WriteLine($"TitleViewGrid.Bounds: {TitleViewGrid.Bounds}");
		Console.WriteLine($"TitleViewGrid.Width: {TitleViewGrid.Width}");
		Console.WriteLine($"TitleViewGrid.Height: {TitleViewGrid.Height}");
		Console.WriteLine($"TitleViewGrid.X: {TitleViewGrid.X}");
		Console.WriteLine($"TitleViewGrid.Y: {TitleViewGrid.Y}");
		
		// Get screen dimensions
		var screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
		var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
		Console.WriteLine($"Screen Size: {screenWidth}x{screenHeight}");
		
		// Platform-specific position tracking
#if IOS || MACCATALYST
		if (TitleViewGrid.Handler?.PlatformView is UIKit.UIView platformView)
		{
			// Find the root superview (window)
			var rootView = platformView;
			while (rootView?.Superview != null)
			{
				rootView = rootView.Superview;
			}
			
			// Convert TitleView bounds to screen coordinates
			var screenRect = platformView.ConvertRectToView(platformView.Bounds, rootView);
			
			Console.WriteLine($"[iOS] Platform Frame: {platformView.Frame}");
			Console.WriteLine($"[iOS] Screen Position: X={screenRect.X}, Y={screenRect.Y}");
			Console.WriteLine($"[iOS] Screen Size: W={screenRect.Width}, H={screenRect.Height}");
			Console.WriteLine($"[iOS] AutoresizingMask: {platformView.AutoresizingMask}");
		}
#endif
		
		Console.WriteLine("======================================================\n");
	}
}
using System;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		
		// Hook into the Loaded event to measure after layout is complete
		Loaded += OnLoaded;
		
		// Also monitor size changes to track rotation
		SizeChanged += OnSizeChanged;
	}
	
	private void OnLoaded(object sender, EventArgs e)
	{
		// Wait for layout to complete
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			LogMeasurements("OnLoaded (Initial)");
		});
	}
	
	private void OnSizeChanged(object sender, EventArgs e)
	{
		// Wait for layout to complete after size change
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			LogMeasurements($"OnSizeChanged (Page: {Width:F0}x{Height:F0})");
		});
	}
	
	private void LogMeasurements(string context)
	{
		Console.WriteLine($"\n========== {context} ==========");
		
		// Get TitleView measurements
		var titleViewWidth = TitleViewGrid.Width;
		var titleViewHeight = TitleViewGrid.Height;
		var titleViewBounds = TitleViewGrid.Bounds;
		
		Console.WriteLine($"TitleView Width: {titleViewWidth:F2}");
		Console.WriteLine($"TitleView Height: {titleViewHeight:F2}");
		Console.WriteLine($"TitleView Bounds: {titleViewBounds}");
		
		// Get screen dimensions for reference
		var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
		var screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
		Console.WriteLine($"Screen Size: {screenWidth:F2}x{screenHeight:F2}");
		
		// Get page dimensions
		Console.WriteLine($"Page Size: {Width:F2}x{Height:F2}");
		
#if IOS || MACCATALYST
		// Get platform-specific navigation bar width for comparison
		if (TitleViewGrid.Handler?.PlatformView is UIKit.UIView platformView)
		{
			var navController = platformView;
			while (navController != null && navController is not UIKit.UINavigationController)
			{
				navController = navController.Superview;
			}
			
			if (navController is UIKit.UINavigationController uiNavController)
			{
				var navBarWidth = uiNavController.NavigationBar.Frame.Width;
				var navBarHeight = uiNavController.NavigationBar.Frame.Height;
				Console.WriteLine($"Navigation Bar Size: {navBarWidth:F2}x{navBarHeight:F2}");
				
				var widthDiff = navBarWidth - titleViewWidth;
				Console.WriteLine($"Width Difference (NavBar - TitleView): {widthDiff:F2}");
				
				if (Math.Abs(widthDiff) > 10)
				{
					Console.WriteLine($"⚠️  WARNING: TitleView width ({titleViewWidth:F2}) does NOT match NavBar width ({navBarWidth:F2})");
					Console.WriteLine($"    This indicates the bug is present!");
				}
				else
				{
					Console.WriteLine($"✅ TitleView width matches NavBar width");
				}
			}
		}
#endif
		
		Console.WriteLine("=================================\n");
		
		// Update UI label
		Dispatcher.Dispatch(() =>
		{
			MeasurementsLabel.Text = $"Last measurement: {context}\nTitleView: {titleViewWidth:F0}x{titleViewHeight:F0}";
		});
	}
}
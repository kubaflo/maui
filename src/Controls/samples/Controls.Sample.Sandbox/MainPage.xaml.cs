using System;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		
		// Capture initial measurements when page appears
		Loaded += OnPageLoaded;
	}

	private async void OnPageLoaded(object? sender, EventArgs e)
	{
		// Wait for layout to complete
		await Task.Delay(500);
		CaptureMeasurements("OnPageLoaded");
	}

	private async void OnMeasureClicked(object? sender, EventArgs e)
	{
		await Task.Delay(100);
		CaptureMeasurements("OnButtonClicked");
	}

	private void CaptureMeasurements(string context)
	{
		Console.WriteLine($"\n========== MEASUREMENTS: {context} ==========");
		Console.WriteLine($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
		
		// Measure the Grid container
		Console.WriteLine($"\n--- TestGrid (Container) ---");
		Console.WriteLine($"Bounds: {TestGrid.Bounds}");
		Console.WriteLine($"Width: {TestGrid.Width}, Height: {TestGrid.Height}");
		
		// Measure the CheckBox inside Grid
		Console.WriteLine($"\n--- CenteredCheckBox (in Grid) ---");
		Console.WriteLine($"Bounds: {CenteredCheckBox.Bounds}");
		Console.WriteLine($"Width: {CenteredCheckBox.Width}, Height: {CenteredCheckBox.Height}");
		Console.WriteLine($"Margin: {CenteredCheckBox.Margin}");
		Console.WriteLine($"HorizontalOptions: {CenteredCheckBox.HorizontalOptions}");
		Console.WriteLine($"VerticalOptions: {CenteredCheckBox.VerticalOptions}");
		
		// Calculate centering
		double gridCenterY = TestGrid.Height / 2;
		double checkBoxCenterY = CenteredCheckBox.Bounds.Y + (CenteredCheckBox.Height / 2);
		double verticalOffset = checkBoxCenterY - gridCenterY;
		
		Console.WriteLine($"\n--- Centering Analysis ---");
		Console.WriteLine($"Grid center Y: {gridCenterY}");
		Console.WriteLine($"CheckBox center Y (Bounds.Y + Height/2): {checkBoxCenterY}");
		Console.WriteLine($"Vertical offset from center: {verticalOffset}");
		Console.WriteLine($"Expected offset: 0 (perfectly centered)");
		
		if (Math.Abs(verticalOffset) > 1)
		{
			Console.WriteLine($"❌ WARNING: CheckBox is NOT centered! Offset: {verticalOffset:F2}px");
		}
		else
		{
			Console.WriteLine($"✅ CheckBox appears centered (offset within 1px)");
		}
		
		// Platform-specific measurements
		#if ANDROID
		Console.WriteLine($"\n--- Android Platform View ---");
		if (CenteredCheckBox.Handler?.PlatformView is Android.Views.View androidView)
		{
			int[] location = new int[2];
			androidView.GetLocationInWindow(location);
			
			Console.WriteLine($"Platform Location in Window: X={location[0]}, Y={location[1]}");
			Console.WriteLine($"Platform Size: {androidView.Width}x{androidView.Height}");
			Console.WriteLine($"Platform MeasuredWidth: {androidView.MeasuredWidth}");
			Console.WriteLine($"Platform MeasuredHeight: {androidView.MeasuredHeight}");
			
			var layoutParams = androidView.LayoutParameters;
			if (layoutParams != null)
			{
				Console.WriteLine($"LayoutParams: {layoutParams.Width}x{layoutParams.Height}");
			}
			
			// Check padding
			Console.WriteLine($"Platform Padding: L={androidView.PaddingLeft}, T={androidView.PaddingTop}, R={androidView.PaddingRight}, B={androidView.PaddingBottom}");
			
			// Check if it's MaterialCheckBox
			if (androidView is Google.Android.Material.CheckBox.MaterialCheckBox materialCheckBox)
			{
				Console.WriteLine($"Type: MaterialCheckBox");
			}
			else if (androidView is AndroidX.AppCompat.Widget.AppCompatCheckBox appCompatCheckBox)
			{
				Console.WriteLine($"Type: AppCompatCheckBox");
			}
		}
		#endif
		
		Console.WriteLine($"===========================================\n");
		
		// Update status label
		StatusLabel.Text = $"Vertical offset: {verticalOffset:F2}px (Expected: 0px). Check console for details.";
	}
}
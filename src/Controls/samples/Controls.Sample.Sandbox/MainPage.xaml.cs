using Microsoft.Maui.Media;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnPickPhotosClicked(object sender, EventArgs e)
	{
		try
		{
			Console.WriteLine("=== TEST: PickPhotosAsync ===");
			Console.WriteLine("User action: Please CANCEL the picker when it appears");
			
			var options = new MediaPickerOptions
			{
				Title = "Test: Please CANCEL to test null return fix"
			};
			
			// This is the key test - before the fix, this could return null
			// After the fix, it should always return a list (empty if cancelled)
			var result = await MediaPicker.Default.PickPhotosAsync(options);
			
			// The fix ensures result is never null
			if (result == null)
			{
				Console.WriteLine("❌ FAIL: Result is NULL (bug not fixed)");
				ResultLabel.Text = "❌ FAIL: Result is NULL\nThe bug is NOT fixed!";
				ResultLabel.TextColor = Colors.Red;
			}
			else
			{
				Console.WriteLine($"✅ PASS: Result is not null");
				Console.WriteLine($"Result Count: {result.Count}");
				
				if (result.Count == 0)
				{
					Console.WriteLine("✅ PASS: Empty list returned as expected on cancellation");
					ResultLabel.Text = $"✅ PASS: Empty list returned\nResult is not null\nCount: {result.Count}";
					ResultLabel.TextColor = Colors.Green;
				}
				else
				{
					Console.WriteLine($"✓ User selected {result.Count} photo(s)");
					ResultLabel.Text = $"✓ User selected {result.Count} photo(s)\nResult is not null (correct)";
					ResultLabel.TextColor = Colors.Blue;
				}
			}
			
			Console.WriteLine("=== END TEST ===");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ EXCEPTION: {ex.GetType().Name}: {ex.Message}");
			Console.WriteLine($"Stack: {ex.StackTrace}");
			ResultLabel.Text = $"❌ EXCEPTION: {ex.GetType().Name}\n{ex.Message}";
			ResultLabel.TextColor = Colors.Red;
		}
	}

	private async void OnPickVideosClicked(object sender, EventArgs e)
	{
		try
		{
			Console.WriteLine("=== TEST: PickVideosAsync ===");
			Console.WriteLine("User action: Please CANCEL the picker when it appears");
			
			var options = new MediaPickerOptions
			{
				Title = "Test: Please CANCEL to test null return fix"
			};
			
			// This is the key test - before the fix, this could return null
			// After the fix, it should always return a list (empty if cancelled)
			var result = await MediaPicker.Default.PickVideosAsync(options);
			
			// The fix ensures result is never null
			if (result == null)
			{
				Console.WriteLine("❌ FAIL: Result is NULL (bug not fixed)");
				ResultLabel.Text = "❌ FAIL: Result is NULL\nThe bug is NOT fixed!";
				ResultLabel.TextColor = Colors.Red;
			}
			else
			{
				Console.WriteLine($"✅ PASS: Result is not null");
				Console.WriteLine($"Result Count: {result.Count}");
				
				if (result.Count == 0)
				{
					Console.WriteLine("✅ PASS: Empty list returned as expected on cancellation");
					ResultLabel.Text = $"✅ PASS: Empty list returned\nResult is not null\nCount: {result.Count}";
					ResultLabel.TextColor = Colors.Green;
				}
				else
				{
					Console.WriteLine($"✓ User selected {result.Count} video(s)");
					ResultLabel.Text = $"✓ User selected {result.Count} video(s)\nResult is not null (correct)";
					ResultLabel.TextColor = Colors.Blue;
				}
			}
			
			Console.WriteLine("=== END TEST ===");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ EXCEPTION: {ex.GetType().Name}: {ex.Message}");
			Console.WriteLine($"Stack: {ex.StackTrace}");
			ResultLabel.Text = $"❌ EXCEPTION: {ex.GetType().Name}\n{ex.Message}";
			ResultLabel.TextColor = Colors.Red;
		}
	}
}
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
			
			var options = new MediaPickerOptions
			{
				Title = "Select Photos (or Cancel)",
				SelectionLimit = 3
			};
			
			Console.WriteLine("Calling PickPhotosAsync...");
			var result = await MediaPicker.Default.PickPhotosAsync(options);
			
			Console.WriteLine($"Result returned: {result}");
			Console.WriteLine($"Result is null: {result == null}");
			Console.WriteLine($"Result count: {result?.Count ?? -1}");
			
			if (result == null)
			{
				ResultLabel.Text = "❌ BUG: Result is NULL (should be empty list)";
				ResultLabel.TextColor = Colors.Red;
				Console.WriteLine("❌ BUG DETECTED: PickPhotosAsync returned null instead of empty list");
			}
			else if (result.Count == 0)
			{
				ResultLabel.Text = "✅ CORRECT: Empty list returned (user cancelled)";
				ResultLabel.TextColor = Colors.Green;
				Console.WriteLine("✅ CORRECT: Empty list returned");
			}
			else
			{
				ResultLabel.Text = $"✅ SUCCESS: {result.Count} photo(s) selected";
				ResultLabel.TextColor = Colors.Blue;
				Console.WriteLine($"✅ SUCCESS: {result.Count} photo(s) selected");
			}
			
			Console.WriteLine("=== END TEST ===");
		}
		catch (Exception ex)
		{
			ResultLabel.Text = $"❌ ERROR: {ex.Message}";
			ResultLabel.TextColor = Colors.Red;
			Console.WriteLine($"❌ ERROR: {ex.GetType().Name}: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}

	private async void OnPickVideosClicked(object sender, EventArgs e)
	{
		try
		{
			Console.WriteLine("=== TEST: PickVideosAsync ===");
			
			var options = new MediaPickerOptions
			{
				Title = "Select Videos (or Cancel)",
				SelectionLimit = 3
			};
			
			Console.WriteLine("Calling PickVideosAsync...");
			var result = await MediaPicker.Default.PickVideosAsync(options);
			
			Console.WriteLine($"Result returned: {result}");
			Console.WriteLine($"Result is null: {result == null}");
			Console.WriteLine($"Result count: {result?.Count ?? -1}");
			
			if (result == null)
			{
				ResultLabel.Text = "❌ BUG: Result is NULL (should be empty list)";
				ResultLabel.TextColor = Colors.Red;
				Console.WriteLine("❌ BUG DETECTED: PickVideosAsync returned null instead of empty list");
			}
			else if (result.Count == 0)
			{
				ResultLabel.Text = "✅ CORRECT: Empty list returned (user cancelled)";
				ResultLabel.TextColor = Colors.Green;
				Console.WriteLine("✅ CORRECT: Empty list returned");
			}
			else
			{
				ResultLabel.Text = $"✅ SUCCESS: {result.Count} video(s) selected";
				ResultLabel.TextColor = Colors.Blue;
				Console.WriteLine($"✅ SUCCESS: {result.Count} video(s) selected");
			}
			
			Console.WriteLine("=== END TEST ===");
		}
		catch (Exception ex)
		{
			ResultLabel.Text = $"❌ ERROR: {ex.Message}";
			ResultLabel.TextColor = Colors.Red;
			Console.WriteLine($"❌ ERROR: {ex.GetType().Name}: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
		}
	}
}
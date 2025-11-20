namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnTitleViewLoaded(object sender, EventArgs e)
	{
		// Wait for layout to complete
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			Console.WriteLine("========== TITLE VIEW MEASUREMENTS ==========");
			
			// Get the platform view frame information
			#if IOS || MACCATALYST
			if (TitleView.Handler?.PlatformView is UIKit.UIView platformView)
			{
				// Get root view (screen bounds)
				var rootView = platformView;
				while (rootView.Superview != null)
					rootView = rootView.Superview;
				
				// Get title view bounds in screen coordinates
				var screenRect = platformView.ConvertRectToView(platformView.Bounds, rootView);
				
				Console.WriteLine($"TitleView Platform Frame:");
				Console.WriteLine($"  X: {platformView.Frame.X}");
				Console.WriteLine($"  Y: {platformView.Frame.Y}");
				Console.WriteLine($"  Width: {platformView.Frame.Width}");
				Console.WriteLine($"  Height: {platformView.Frame.Height}");
				
				Console.WriteLine($"TitleView Screen Position:");
				Console.WriteLine($"  Screen X: {screenRect.X}");
				Console.WriteLine($"  Screen Y: {screenRect.Y}");
				Console.WriteLine($"  Screen Width: {screenRect.Width}");
				Console.WriteLine($"  Screen Height: {screenRect.Height}");
				
				Console.WriteLine($"Screen Info:");
				Console.WriteLine($"  Screen Width: {rootView.Bounds.Width}");
				Console.WriteLine($"  Screen Height: {rootView.Bounds.Height}");
			}
			#endif
			
			// MAUI view information
			Console.WriteLine($"TitleView MAUI Properties:");
			Console.WriteLine($"  Margin: {TitleView.Margin}");
			Console.WriteLine($"  Bounds: {TitleView.Bounds}");
			Console.WriteLine($"  Width: {TitleView.Width}");
			Console.WriteLine($"  Height: {TitleView.Height}");
			
			Console.WriteLine("=============================================");
		});
	}
}
using System;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		
		// Log initial state
		LogFlowDirectionState("Initial Load");
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		
		// Add a small delay to ensure layout is complete
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			LogFlowDirectionState("OnAppearing");
		});
	}

	private void OnToggleFlowDirectionClicked(object sender, EventArgs e)
	{
		var newDirection = CollectionViewSimple.FlowDirection == FlowDirection.LeftToRight 
			? FlowDirection.RightToLeft 
			: FlowDirection.LeftToRight;
		
		Console.WriteLine($"\n========== TOGGLING FLOWDIRECTION to {newDirection} ==========");
		
		CollectionViewSimple.FlowDirection = newDirection;
		CollectionViewView.FlowDirection = newDirection;
		CollectionViewTemplated.FlowDirection = newDirection;
		
		FlowDirectionLabel.Text = $"Current FlowDirection: {newDirection}";
		
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
		{
			LogFlowDirectionState($"After Toggle to {newDirection}");
		});
	}

	private void LogFlowDirectionState(string context)
	{
		Console.WriteLine($"\n========== {context} ==========");
		Console.WriteLine($"CollectionViewSimple.FlowDirection: {CollectionViewSimple.FlowDirection}");
		Console.WriteLine($"CollectionViewView.FlowDirection: {CollectionViewView.FlowDirection}");
		Console.WriteLine($"CollectionViewTemplated.FlowDirection: {CollectionViewTemplated.FlowDirection}");
		
		// Try to access EmptyView if it's been created
		if (CollectionViewSimple.Handler?.PlatformView != null)
		{
			Console.WriteLine($"CollectionViewSimple PlatformView exists");
			
#if ANDROID
			if (CollectionViewSimple.Handler.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
			{
				Console.WriteLine($"Android RecyclerView LayoutDirection: {recyclerView.LayoutDirection}");
			}
#elif IOS || MACCATALYST
			if (CollectionViewSimple.Handler.PlatformView is UIKit.UICollectionView uiCollectionView)
			{
				Console.WriteLine($"iOS UICollectionView SemanticContentAttribute: {uiCollectionView.SemanticContentAttribute}");
			}
#endif
		}
		
		Console.WriteLine("=================================\n");
	}
}
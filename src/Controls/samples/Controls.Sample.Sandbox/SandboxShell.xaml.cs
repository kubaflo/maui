namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	public SandboxShell()
	{
		InitializeComponent();
		
		// Register a test page to navigate to
		Routing.RegisterRoute("TestPage", typeof(TestBackButtonPage));
		
		// Automatically navigate to test page after a short delay
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), async () =>
		{
			await GoToAsync("TestPage");
		});
	}
}

// Test page with BackButtonBehavior.IconOverride
public class TestBackButtonPage : ContentPage
{
	public TestBackButtonPage()
	{
		Title = "Test Icon Override";
		
		// Set the IconOverride on the back button
		// This is the scenario that was broken - IconOverride not working when FlyoutBehavior != Flyout
		Shell.SetBackButtonBehavior(this, new BackButtonBehavior 
		{ 
			IconOverride = "dotnet_bot.png" 
		});
		
		Content = new StackLayout
		{
			Padding = 20,
			Children =
			{
				new Label 
				{ 
					Text = "TEST: IconOverride on Back Button",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold,
					Margin = new Thickness(0, 0, 0, 20)
				},
				new Label 
				{ 
					Text = "Expected: Back button should show dotnet_bot.png icon",
					Margin = new Thickness(0, 0, 0, 10)
				},
				new Label 
				{ 
					Text = "Bug (before fix): Back button shows default arrow",
					TextColor = Colors.Red,
					Margin = new Thickness(0, 0, 0, 10)
				},
				new Label 
				{ 
					Text = "Fixed (after PR): Back button shows custom icon",
					TextColor = Colors.Green,
					Margin = new Thickness(0, 0, 0, 10)
				},
				new Label 
				{ 
					Text = "\nNote: This only affects Android when FlyoutBehavior is not Flyout.",
					FontSize = 12,
					TextColor = Colors.Gray
				}
			}
		};
		
		Console.WriteLine("========== ICON OVERRIDE TEST ==========");
		Console.WriteLine("Page loaded with BackButtonBehavior.IconOverride = dotnet_bot.png");
		Console.WriteLine("FlyoutBehavior: " + Shell.GetFlyoutBehavior(this));
		Console.WriteLine("Expected: Custom icon should be visible on back button");
		Console.WriteLine("========================================");
	}
}
